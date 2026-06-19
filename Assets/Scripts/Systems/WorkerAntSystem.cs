using AntWar.Components;
using AntWar.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MovementSystem))]
    [UpdateBefore(typeof(NestSystem))]
    public partial struct WorkerAntSystem : ISystem
    {
        private EntityQuery _foodQuery;
        private EntityQuery _nestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorkerAntTag>();
            state.RequireForUpdate<AntStateComponent>();

            _foodQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<FoodComponent>(),
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<FoodTag>());

            _nestQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<NestComponent>(),
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<TeamComponent>(),
                ComponentType.ReadOnly<NestTag>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var foodEntities = _foodQuery.ToEntityArray(Allocator.Temp);
            var foodPositions = _foodQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            var foodComponents = _foodQuery.ToComponentDataArray<FoodComponent>(Allocator.Temp);

            var nestEntities = _nestQuery.ToEntityArray(Allocator.Temp);
            var nestPositions = _nestQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            var nestTeams = _nestQuery.ToComponentDataArray<TeamComponent>(Allocator.Temp);

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (position, velocity, antState, carry, team, strategy, workerEntity) in
                     SystemAPI.Query<
                         RefRW<PositionComponent>,
                         RefRW<VelocityComponent>,
                         RefRW<AntStateComponent>,
                         RefRW<CarryComponent>,
                         RefRO<TeamComponent>,
                         RefRO<StrategyComponent>>()
                     .WithAll<WorkerAntTag>().WithEntityAccess())
            {
                float2 pos = position.ValueRW.Value;
                TeamType myTeam = team.ValueRO.Team;
                AntState currentState = antState.ValueRW.CurrentState;

                float2 nestPos = float2.zero;
                Entity nestEntity = Entity.Null;
                for (int i = 0; i < nestTeams.Length; i++)
                {
                    if (nestTeams[i].Team == myTeam)
                    {
                        nestPos = nestPositions[i].Value;
                        nestEntity = nestEntities[i];
                        break;
                    }
                }

                switch (currentState)
                {
                    case AntState.Idle:
                        FindNearestFood(pos, foodEntities, foodPositions, foodComponents,
                            ref antState, strategy.ValueRO);
                        break;

                    case AntState.SeekingFood:
                        HandleSeekingFood(pos, ref antState, ref carry, ref velocity,
                            foodEntities, foodPositions, foodComponents, deltaTime,
                            ref state, ecb, myTeam);
                        break;

                    case AntState.ReturningFood:
                        HandleReturningFood(pos, nestPos, nestEntity, ref antState, ref carry,
                            ref velocity, ref state, deltaTime, myTeam);
                        break;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            foodEntities.Dispose();
            foodPositions.Dispose();
            foodComponents.Dispose();
            nestEntities.Dispose();
            nestPositions.Dispose();
            nestTeams.Dispose();
        }

        private void FindNearestFood(float2 currentPos,
            NativeArray<Entity> foodEntities,
            NativeArray<PositionComponent> foodPositions,
            NativeArray<FoodComponent> foodComponents,
            ref RefRW<AntStateComponent> antState,
            StrategyComponent strategy)
        {
            int nearestIndex = -1;
            float nearestDistSq = float.MaxValue;

            for (int i = 0; i < foodComponents.Length; i++)
            {
                if (foodComponents[i].Amount <= 0f)
                    continue;

                float distSq = math.distancesq(currentPos, foodPositions[i].Value);

                if (strategy.Strategy == StrategyType.GatherArea)
                {
                    float distToTarget = math.distance(foodPositions[i].Value, strategy.StrategyTarget);
                    if (distToTarget > strategy.StrategyRadius)
                    {
                        distSq *= 3f;
                    }
                }

                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearestIndex = i;
                }
            }

            if (nearestIndex >= 0)
            {
                antState.ValueRW.CurrentState = AntState.SeekingFood;
                antState.ValueRW.TargetEntity = foodEntities[nearestIndex];
                antState.ValueRW.TargetPosition = foodPositions[nearestIndex].Value;
                antState.ValueRW.HasTarget = true;
            }
        }

        private void HandleSeekingFood(float2 pos,
            ref RefRW<AntStateComponent> antState,
            ref RefRW<CarryComponent> carry,
            ref RefRW<VelocityComponent> velocity,
            NativeArray<Entity> foodEntities,
            NativeArray<PositionComponent> foodPositions,
            NativeArray<FoodComponent> foodComponents,
            float deltaTime,
            ref SystemState state,
            EntityCommandBuffer ecb,
            TeamType team)
        {
            if (!antState.ValueRO.HasTarget)
            {
                antState.ValueRW.CurrentState = AntState.Idle;
                velocity.ValueRW.Value = float2.zero;
                return;
            }

            Entity target = antState.ValueRO.TargetEntity;
            float2 targetPos = antState.ValueRO.TargetPosition;
            float distToTarget = math.distance(pos, targetPos);

            if (distToTarget <= GameConfig.FoodGatherRange)
            {
                float gatherAmount = GameConfig.FoodGatherRate * deltaTime;

                int foodIndex = -1;
                for (int i = 0; i < foodEntities.Length; i++)
                {
                    if (foodEntities[i] == target)
                    {
                        foodIndex = i;
                        break;
                    }
                }

                if (foodIndex >= 0 && foodComponents[foodIndex].Amount > 0f)
                {
                    float actualGather = math.min(gatherAmount, foodComponents[foodIndex].Amount);
                    actualGather = math.min(actualGather, carry.ValueRO.MaxCarryCapacity - carry.ValueRO.CarriedFood);

                    if (actualGather > 0f)
                    {
                        carry.ValueRW.CarriedFood += actualGather;
                        carry.ValueRW.IsCarrying = carry.ValueRO.CarriedFood > 0f;

                        RefRW<FoodComponent> foodComp = SystemAPI.GetComponentRW<FoodComponent>(target);
                        foodComp.ValueRW.Amount -= actualGather;
                    }

                    if (carry.ValueRO.CarriedFood >= carry.ValueRO.MaxCarryCapacity ||
                        foodComponents[foodIndex].Amount <= actualGather)
                    {
                        if (carry.ValueRO.CarriedFood > 0f)
                        {
                            BattleLogger.LogFoodGathered(
                                team == TeamType.Red ? "红方" : "蓝方",
                                carry.ValueRO.CarriedFood);
                        }

                        antState.ValueRW.CurrentState = AntState.ReturningFood;
                        antState.ValueRW.HasTarget = false;
                        antState.ValueRW.TargetEntity = Entity.Null;
                    }
                }
                else
                {
                    antState.ValueRW.CurrentState = AntState.Idle;
                    antState.ValueRW.HasTarget = false;
                    antState.ValueRW.TargetEntity = Entity.Null;
                }

                velocity.ValueRW.Value = float2.zero;
            }
            else
            {
                float2 dir = math.normalize(targetPos - pos);
                velocity.ValueRW.Value = dir;
            }
        }

        private void HandleReturningFood(float2 pos,
            float2 nestPos,
            Entity nestEntity,
            ref RefRW<AntStateComponent> antState,
            ref RefRW<CarryComponent> carry,
            ref RefRW<VelocityComponent> velocity,
            ref SystemState state,
            float deltaTime,
            TeamType team)
        {
            float distToNest = math.distance(pos, nestPos);

            if (distToNest <= GameConfig.NestRadius)
            {
                float deliveredFood = carry.ValueRO.CarriedFood;

                if (deliveredFood > 0f && nestEntity != Entity.Null)
                {
                    RefRW<NestComponent> nestComp = SystemAPI.GetComponentRW<NestComponent>(nestEntity);
                    nestComp.ValueRW.StoredFood += deliveredFood;

                    BattleLogger.LogFoodDelivered(
                        team == TeamType.Red ? "红方" : "蓝方",
                        deliveredFood,
                        nestComp.ValueRO.StoredFood);
                }

                carry.ValueRW.CarriedFood = 0f;
                carry.ValueRW.IsCarrying = false;

                antState.ValueRW.CurrentState = AntState.Idle;
                velocity.ValueRW.Value = float2.zero;
            }
            else
            {
                float2 dir = math.normalize(nestPos - pos);
                velocity.ValueRW.Value = dir;
            }
        }
    }
}
