using AntWar.Components;
using AntWar.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MovementSystem))]
    [UpdateBefore(typeof(FoodSystem))]
    public partial struct SoldierAntSystem : ISystem
    {
        private EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SoldierAntTag>();
            state.RequireForUpdate<AntStateComponent>();

            _enemyQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<TeamComponent>(),
                ComponentType.ReadOnly<HealthComponent>(),
                ComponentType.ReadOnly<AntTypeComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.Temp);
            var enemyPositions = _enemyQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            var enemyTeams = _enemyQuery.ToComponentDataArray<TeamComponent>(Allocator.Temp);
            var enemyHealths = _enemyQuery.ToComponentDataArray<HealthComponent>(Allocator.Temp);
            var enemyTypes = _enemyQuery.ToComponentDataArray<AntTypeComponent>(Allocator.Temp);

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (position, velocity, antState, combat, team, health, antType, strategy, avoidance, soldierEntity) in
                     SystemAPI.Query<
                         RefRW<PositionComponent>,
                         RefRW<VelocityComponent>,
                         RefRW<AntStateComponent>,
                         RefRW<CombatComponent>,
                         RefRO<TeamComponent>,
                         RefRO<HealthComponent>,
                         RefRO<AntTypeComponent>,
                         RefRO<StrategyComponent>,
                         RefRW<AvoidanceComponent>>()
                     .WithAll<SoldierAntTag>().WithEntityAccess())
            {
                float2 pos = position.ValueRW.Value;
                TeamType myTeam = team.ValueRO.Team;
                AntState currentState = antState.ValueRW.CurrentState;

                if (avoidance.ValueRO.DetourCooldown > 0f)
                {
                    velocity.ValueRW.Value = avoidance.ValueRO.DetourDirection;
                    continue;
                }

                switch (currentState)
                {
                    case AntState.Idle:
                    case AntState.Defending:
                        FindNearestEnemy(pos, myTeam, enemyEntities, enemyPositions, enemyTeams, enemyHealths,
                            ref antState, combat.ValueRO.AttackRange, strategy.ValueRO, ref avoidance);
                        break;

                    case AntState.Attacking:
                        HandleAttacking(pos, ref antState, ref combat, ref velocity,
                            enemyEntities, enemyPositions, enemyHealths, enemyTeams, enemyTypes,
                            deltaTime, myTeam, ref state, ecb, soldierEntity);
                        break;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            enemyEntities.Dispose();
            enemyPositions.Dispose();
            enemyTeams.Dispose();
            enemyHealths.Dispose();
            enemyTypes.Dispose();
        }

        private void FindNearestEnemy(float2 currentPos,
            TeamType myTeam,
            NativeArray<Entity> enemyEntities,
            NativeArray<PositionComponent> enemyPositions,
            NativeArray<TeamComponent> enemyTeams,
            NativeArray<HealthComponent> enemyHealths,
            ref RefRW<AntStateComponent> antState,
            float attackRange,
            StrategyComponent strategy,
            ref RefRW<AvoidanceComponent> avoidance)
        {
            int nearestIndex = -1;
            float nearestDistSq = float.MaxValue;

            float detectionRange = GameConfig.SoldierDetectionRange;

            if (strategy.Strategy == StrategyType.Defend)
            {
                detectionRange *= 0.6f;
            }

            for (int i = 0; i < enemyEntities.Length; i++)
            {
                if (enemyTeams[i].Team == myTeam)
                    continue;

                if (enemyHealths[i].CurrentHealth <= 0f)
                    continue;

                float distSq = math.distancesq(currentPos, enemyPositions[i].Value);

                if (strategy.Strategy == StrategyType.Attack)
                {
                    float distToStrategyTarget = math.distance(enemyPositions[i].Value, strategy.StrategyTarget);
                    if (distToStrategyTarget > strategy.StrategyRadius)
                    {
                        continue;
                    }
                }

                if (distSq < nearestDistSq && distSq <= detectionRange * detectionRange)
                {
                    nearestDistSq = distSq;
                    nearestIndex = i;
                }
            }

            if (nearestIndex >= 0)
            {
                antState.ValueRW.CurrentState = AntState.Attacking;
                antState.ValueRW.TargetEntity = enemyEntities[nearestIndex];
                antState.ValueRW.TargetPosition = enemyPositions[nearestIndex].Value;
                antState.ValueRW.HasTarget = true;
            }
            else
            {
                if (strategy.Strategy == StrategyType.Defend)
                {
                    antState.ValueRW.CurrentState = AntState.Defending;

                    uint seed = (uint)(currentPos.x * 1000f + currentPos.y * 7919f +
                                       avoidance.ValueRO.RandomSeed +
                                       (uint)SystemAPI.Time.ElapsedTime * 7u);
                    Random rng = Random.CreateFromIndex(seed);
                    float angle = rng.NextFloat(0f, 2f * math.PI);
                    float2 wanderDir = new float2(math.cos(angle), math.sin(angle));

                    float2 nestPos = strategy.Strategy == StrategyType.Defend
                        ? strategy.StrategyTarget
                        : currentPos;
                    float2 wanderTarget = nestPos + wanderDir * rng.NextFloat(3f, 8f);

                    antState.ValueRW.TargetPosition = wanderTarget;
                    antState.ValueRW.HasTarget = true;
                }
                else
                {
                    antState.ValueRW.CurrentState = AntState.Idle;
                    antState.ValueRW.HasTarget = false;
                    antState.ValueRW.TargetEntity = Entity.Null;
                }
            }
        }

        private void HandleAttacking(float2 pos,
            ref RefRW<AntStateComponent> antState,
            ref RefRW<CombatComponent> combat,
            ref RefRW<VelocityComponent> velocity,
            NativeArray<Entity> enemyEntities,
            NativeArray<PositionComponent> enemyPositions,
            NativeArray<HealthComponent> enemyHealths,
            NativeArray<TeamComponent> enemyTeams,
            NativeArray<AntTypeComponent> enemyTypes,
            float deltaTime,
            TeamType myTeam,
            ref SystemState state,
            EntityCommandBuffer ecb,
            Entity soldierEntity)
        {
            if (!antState.ValueRO.HasTarget || antState.ValueRO.TargetEntity == Entity.Null)
            {
                antState.ValueRW.CurrentState = AntState.Idle;
                velocity.ValueRW.Value = float2.zero;
                return;
            }

            Entity target = antState.ValueRO.TargetEntity;
            float2 targetPos = antState.ValueRO.TargetPosition;
            float distToTarget = math.distance(pos, targetPos);

            int targetIndex = -1;
            for (int i = 0; i < enemyEntities.Length; i++)
            {
                if (enemyEntities[i] == target)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0 || enemyHealths[targetIndex].CurrentHealth <= 0f)
            {
                antState.ValueRW.CurrentState = AntState.Idle;
                antState.ValueRW.HasTarget = false;
                antState.ValueRW.TargetEntity = Entity.Null;
                velocity.ValueRW.Value = float2.zero;
                return;
            }

            targetPos = enemyPositions[targetIndex].Value;
            distToTarget = math.distance(pos, targetPos);
            antState.ValueRW.TargetPosition = targetPos;

            if (distToTarget <= combat.ValueRO.AttackRange)
            {
                velocity.ValueRW.Value = float2.zero;

                combat.ValueRW.AttackTimer -= deltaTime;
                if (combat.ValueRO.AttackTimer <= 0f)
                {
                    combat.ValueRW.AttackTimer = combat.ValueRO.AttackCooldown;

                    float damage = combat.ValueRO.Damage;
                    TeamType enemyTeam = enemyTeams[targetIndex].Team;
                    AntType enemyType = enemyTypes[targetIndex].Type;

                    RefRW<HealthComponent> targetHealth = SystemAPI.GetComponentRW<HealthComponent>(target);
                    targetHealth.ValueRW.CurrentHealth -= damage;

                    BattleLogger.LogCombat(
                        myTeam == TeamType.Red ? "红方" : "蓝方",
                        "兵蚁",
                        enemyTeam == TeamType.Red ? "红方" : "蓝方",
                        enemyType == AntType.Worker ? "工蚁" : "兵蚁",
                        damage);

                    if (targetHealth.ValueRO.CurrentHealth <= 0f)
                    {
                        BattleLogger.LogKill(
                            myTeam == TeamType.Red ? "红方" : "蓝方",
                            "兵蚁",
                            enemyTeam == TeamType.Red ? "红方" : "蓝方",
                            enemyType == AntType.Worker ? "工蚁" : "兵蚁");

                        float2 deadPos = enemyPositions[targetIndex].Value;
                        float carrionYield = enemyType == AntType.Soldier
                            ? GameConfig.CarrionPerSoldier
                            : GameConfig.CarrionPerWorker;

                        if (carrionYield > 0f)
                        {
                            Entity carrionEntity = ecb.CreateEntity();
                            ecb.AddComponent(carrionEntity, new PositionComponent { Value = deadPos });
                            ecb.AddComponent(carrionEntity, new FoodComponent
                            {
                                Amount = carrionYield,
                                MaxAmount = carrionYield
                            });
                            ecb.AddComponent(carrionEntity, new CarrionComponent
                            {
                                DecayTimer = GameConfig.CarrionDecayTime,
                                DecayTime = GameConfig.CarrionDecayTime
                            });
                            ecb.AddComponent<FoodTag>(carrionEntity);
                            ecb.AddComponent<CarrionTag>(carrionEntity);

                            string carrionSource = enemyType == AntType.Soldier ? "兵蚁尸体" : "工蚁尸体";
                            BattleLogger.LogCarrionSpawned(deadPos, carrionYield, carrionSource);
                        }

                        ecb.DestroyEntity(target);

                        antState.ValueRW.CurrentState = AntState.Idle;
                        antState.ValueRW.HasTarget = false;
                        antState.ValueRW.TargetEntity = Entity.Null;
                    }
                }
            }
            else
            {
                float2 dir = math.normalize(targetPos - pos);
                velocity.ValueRW.Value = dir;
            }
        }
    }
}
