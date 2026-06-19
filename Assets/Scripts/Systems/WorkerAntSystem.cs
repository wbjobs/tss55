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
        private EntityQuery _workerQuery;
        private EntityQuery _treeQuery;

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

            _workerQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<TeamComponent>(),
                ComponentType.ReadOnly<AntStateComponent>(),
                ComponentType.ReadOnly<WorkerAntTag>());

            _treeQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<FruitTreeComponent>(),
                ComponentType.ReadOnly<FruitTreeTag>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var foodEntities = _foodQuery.ToEntityArray(Allocator.Temp);
            var foodPositions = _foodQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            var foodComponents = _foodQuery.ToComponentDataArray<FoodComponent>(Allocator.Temp);

            var nestEntities = _nestQuery.ToEntityArray(Allocator.Temp);
            var nestPositions = _nestQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            var nestTeams = _nestQuery.ToComponentDataArray<TeamComponent>(Allocator.Temp);

            var workerPositions = _workerQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            var workerTeams = _workerQuery.ToComponentDataArray<TeamComponent>(Allocator.Temp);
            var workerStates = _workerQuery.ToComponentDataArray<AntStateComponent>(Allocator.Temp);

            var treeEntities = _treeQuery.ToEntityArray(Allocator.Temp);
            var treeComponents = _treeQuery.ToComponentDataArray<FruitTreeComponent>(Allocator.Temp);

            var regeneratingTrees = new NativeHashSet<Entity>(treeEntities.Length, Allocator.Temp);
            for (int t = 0; t < treeEntities.Length; t++)
            {
                if (treeComponents[t].IsRegenerating)
                {
                    regeneratingTrees.Add(treeEntities[t]);
                }
            }

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (position, velocity, antState, carry, team, strategy, avoidance, workerEntity) in
                     SystemAPI.Query<
                         RefRW<PositionComponent>,
                         RefRW<VelocityComponent>,
                         RefRW<AntStateComponent>,
                         RefRW<CarryComponent>,
                         RefRO<TeamComponent>,
                         RefRO<StrategyComponent>,
                         RefRW<AvoidanceComponent>>()
                     .WithAll<WorkerAntTag>().WithEntityAccess())
            {
                float2 pos = position.ValueRW.Value;
                TeamType myTeam = team.ValueRO.Team;
                AntState currentState = antState.ValueRW.CurrentState;

                if (avoidance.ValueRO.DetourCooldown > 0f)
                {
                    velocity.ValueRW.Value = avoidance.ValueRO.DetourDirection;
                    continue;
                }

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
                        FindNearestFood(pos, myTeam, foodEntities, foodPositions, foodComponents,
                            ref antState, strategy.ValueRO, ref avoidance,
                            workerPositions, workerTeams, workerStates,
                            regeneratingTrees);
                        break;

                    case AntState.SeekingFood:
                        HandleSeekingFood(pos, ref antState, ref carry, ref velocity,
                            foodEntities, foodPositions, foodComponents, deltaTime,
                            ref state, ecb, myTeam, ref avoidance, regeneratingTrees);
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
            workerPositions.Dispose();
            workerTeams.Dispose();
            workerStates.Dispose();
            treeEntities.Dispose();
            treeComponents.Dispose();
            regeneratingTrees.Dispose();
        }

        private void FindNearestFood(float2 currentPos,
            TeamType myTeam,
            NativeArray<Entity> foodEntities,
            NativeArray<PositionComponent> foodPositions,
            NativeArray<FoodComponent> foodComponents,
            ref RefRW<AntStateComponent> antState,
            StrategyComponent strategy,
            ref RefRW<AvoidanceComponent> avoidance,
            NativeArray<PositionComponent> workerPositions,
            NativeArray<TeamComponent> workerTeams,
            NativeArray<AntStateComponent> workerStates,
            NativeHashSet<Entity> regeneratingTrees)
        {
            if (foodEntities.Length == 0)
                return;

            int bestIndex = -1;
            float bestScore = float.MaxValue;

            for (int i = 0; i < foodComponents.Length; i++)
            {
                if (foodComponents[i].Amount <= 0f)
                    continue;

                if (regeneratingTrees.Contains(foodEntities[i]))
                    continue;

                float distSq = math.distancesq(currentPos, foodPositions[i].Value);
                float dist = math.sqrt(distSq);

                float congestion = 0f;
                float congestionRadius = GameConfig.CongestionCheckRadius;
                for (int j = 0; j < workerPositions.Length; j++)
                {
                    if (workerTeams[j].Team != myTeam)
                        continue;

                    float distToFood = math.distance(workerPositions[j].Value, foodPositions[i].Value);
                    if (distToFood < congestionRadius)
                    {
                        AntState otherState = workerStates[j].CurrentState;
                        if (otherState == AntState.SeekingFood || otherState == AntState.Idle)
                        {
                            congestion += 1f - distToFood / congestionRadius;
                        }
                    }
                }

                float score = dist + congestion * GameConfig.CongestionPenaltyWeight;

                if (strategy.Strategy == StrategyType.GatherArea)
                {
                    float distToTarget = math.distance(foodPositions[i].Value, strategy.StrategyTarget);
                    if (distToTarget > strategy.StrategyRadius)
                    {
                        score += dist * 2f;
                    }
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                float2 foodPos = foodPositions[bestIndex].Value;
                uint seed = (uint)(currentPos.x * 1000f + currentPos.y * 7919f + bestIndex * 31f + avoidance.ValueRO.RandomSeed);
                Random rng = Random.CreateFromIndex(seed);
                float2 scatterOffset = new float2(
                    rng.NextFloat(-GameConfig.TargetScatterRadius, GameConfig.TargetScatterRadius),
                    rng.NextFloat(-GameConfig.TargetScatterRadius, GameConfig.TargetScatterRadius));
                float2 scatteredTarget = foodPos + scatterOffset;

                antState.ValueRW.CurrentState = AntState.SeekingFood;
                antState.ValueRW.TargetEntity = foodEntities[bestIndex];
                antState.ValueRW.TargetPosition = scatteredTarget;
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
            TeamType team,
            ref RefRW<AvoidanceComponent> avoidance,
            NativeHashSet<Entity> regeneratingTrees)
        {
            if (!antState.ValueRO.HasTarget || antState.ValueRO.TargetEntity == Entity.Null)
            {
                antState.ValueRW.CurrentState = AntState.Idle;
                velocity.ValueRW.Value = float2.zero;
                return;
            }

            Entity target = antState.ValueRO.TargetEntity;

            if (regeneratingTrees.Contains(target))
            {
                antState.ValueRW.CurrentState = AntState.Idle;
                antState.ValueRW.HasTarget = false;
                antState.ValueRW.TargetEntity = Entity.Null;
                velocity.ValueRW.Value = float2.zero;
                return;
            }

            float2 targetPos = antState.ValueRO.TargetPosition;

            int foodIndex = -1;
            for (int i = 0; i < foodEntities.Length; i++)
            {
                if (foodEntities[i] == target)
                {
                    foodIndex = i;
                    break;
                }
            }

            if (foodIndex < 0 || foodComponents[foodIndex].Amount <= 0f)
            {
                antState.ValueRW.CurrentState = AntState.Idle;
                antState.ValueRW.HasTarget = false;
                antState.ValueRW.TargetEntity = Entity.Null;
                velocity.ValueRW.Value = float2.zero;
                return;
            }

            float2 actualFoodPos = foodPositions[foodIndex].Value;
            float distToActualFood = math.distance(pos, actualFoodPos);

            if (distToActualFood <= GameConfig.FoodGatherRange)
            {
                float gatherAmount = GameConfig.FoodGatherRate * deltaTime;
                float actualGather = math.min(gatherAmount, foodComponents[foodIndex].Amount);
                actualGather = math.min(actualGather, carry.ValueRO.MaxCarryCapacity - carry.ValueRO.CarriedFood);

                if (actualGather > 0f)
                {
                    carry.ValueRW.CarriedFood += actualGather;
                    carry.ValueRW.IsCarrying = carry.ValueRO.CarriedFood > 0f;

                    RefRW<FoodComponent> foodComp = SystemAPI.GetComponentRW<FoodComponent>(target);
                    foodComp.ValueRW.Amount -= actualGather;
                }

                bool capacityFull = carry.ValueRO.CarriedFood >= carry.ValueRO.MaxCarryCapacity;
                bool foodDepleted = foodComponents[foodIndex].Amount <= actualGather + 0.01f;

                if (capacityFull || foodDepleted)
                {
                    if (carry.ValueRO.CarriedFood > 0f)
                    {
                        BattleLogger.LogFoodGathered(
                            team == TeamType.Red ? "红方" : "蓝方",
                            carry.ValueRO.CarriedFood);
                    }

                    antState.ValueRW.CurrentState = carry.ValueRO.CarriedFood > 0f
                        ? AntState.ReturningFood
                        : AntState.Idle;
                    antState.ValueRW.HasTarget = false;
                    antState.ValueRW.TargetEntity = Entity.Null;
                }

                velocity.ValueRW.Value = float2.zero;
            }
            else
            {
                float2 dir = math.normalize(actualFoodPos - pos);
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
