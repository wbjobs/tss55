using AntWar.Components;
using AntWar.Utils;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WorkerAntSystem))]
    [UpdateAfter(typeof(SoldierAntSystem))]
    public partial struct NestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NestTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);

            foreach (var (nest, position, team, nestEntity) in
                     SystemAPI.Query<RefRW<NestComponent>, RefRO<PositionComponent>, RefRO<TeamComponent>>()
                     .WithAll<NestTag>().WithEntityAccess())
            {
                nest.ValueRW.SpawnTimer -= deltaTime;

                if (nest.ValueRO.SpawnTimer <= 0f)
                {
                    if (CanSpawnWorker(nest.ValueRO))
                    {
                        SpawnAnt(ref state, ecb, position.ValueRO.Value, team.ValueRO.Team, AntType.Worker);
                        nest.ValueRW.WorkerCount++;
                        nest.ValueRW.StoredFood -= nest.ValueRO.FoodPerSpawn;
                        nest.ValueRW.SpawnTimer = nest.ValueRO.SpawnInterval;

                        BattleLogger.LogSpawn(team.ValueRO.Team == TeamType.Red ? "红方" : "蓝方", "工蚁");
                    }
                    else if (CanSpawnSoldier(nest.ValueRO))
                    {
                        SpawnAnt(ref state, ecb, position.ValueRO.Value, team.ValueRO.Team, AntType.Soldier);
                        nest.ValueRW.SoldierCount++;
                        nest.ValueRW.StoredFood -= nest.ValueRO.FoodPerSpawn * 2.5f;
                        nest.ValueRW.SpawnTimer = nest.ValueRO.SpawnInterval * 1.5f;

                        BattleLogger.LogSpawn(team.ValueRO.Team == TeamType.Red ? "红方" : "蓝方", "兵蚁");
                    }
                    else
                    {
                        nest.ValueRW.SpawnTimer = 2f;
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private bool CanSpawnWorker(NestComponent nest)
        {
            return nest.StoredFood >= nest.FoodPerSpawn;
        }

        private bool CanSpawnSoldier(NestComponent nest)
        {
            return nest.StoredFood >= nest.FoodPerSpawn * 2.5f &&
                   nest.SoldierCount < nest.WorkerCount * 0.5f;
        }

        private void SpawnAnt(ref SystemState state, EntityCommandBuffer ecb, float2 nestPos, TeamType team, AntType antType)
        {
            Entity antEntity = ecb.CreateEntity();

            Random random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000));
            float2 offset = MathUtils.RandomDirection(ref random) * 3f;
            float2 spawnPos = nestPos + offset;

            ecb.AddComponent(antEntity, new PositionComponent { Value = spawnPos });
            ecb.AddComponent(antEntity, new VelocityComponent
            {
                Value = float2.zero,
                Speed = antType == AntType.Worker ? GameConfig.WorkerSpeed : GameConfig.SoldierSpeed
            });
            ecb.AddComponent(antEntity, new TeamComponent { Team = team });
            ecb.AddComponent(antEntity, new AntTypeComponent { Type = antType });
            ecb.AddComponent(antEntity, new AntStateComponent
            {
                CurrentState = AntState.Idle,
                HasTarget = false,
                TargetEntity = Entity.Null,
                TargetPosition = float2.zero
            });
            ecb.AddComponent(antEntity, new StrategyComponent
            {
                Strategy = StrategyType.None,
                StrategyTarget = float2.zero,
                StrategyRadius = 0f,
                Priority = 0f
            });
            ecb.AddComponent(antEntity, new AvoidanceComponent
            {
                PreviousPosition = spawnPos,
                StagnationTimer = 0f,
                AvoidanceOffset = float2.zero,
                StuckCount = 0,
                DetourCooldown = 0f,
                DetourDirection = float2.zero,
                RandomSeed = random.NextFloat(0f, 10000f)
            });

            if (antType == AntType.Worker)
            {
                ecb.AddComponent<WorkerAntTag>(antEntity);
                ecb.AddComponent(antEntity, new CarryComponent
                {
                    CarriedFood = 0f,
                    MaxCarryCapacity = GameConfig.WorkerCarryCapacity,
                    IsCarrying = false
                });
                ecb.AddComponent(antEntity, new HealthComponent
                {
                    CurrentHealth = GameConfig.WorkerHealth,
                    MaxHealth = GameConfig.WorkerHealth
                });
                ecb.AddComponent(antEntity, new CombatComponent
                {
                    Damage = 1f,
                    AttackRange = 1f,
                    AttackCooldown = 2f,
                    AttackTimer = 0f,
                    Target = Entity.Null
                });
            }
            else
            {
                ecb.AddComponent<SoldierAntTag>(antEntity);
                ecb.AddComponent(antEntity, new CarryComponent
                {
                    CarriedFood = 0f,
                    MaxCarryCapacity = 0f,
                    IsCarrying = false
                });
                ecb.AddComponent(antEntity, new HealthComponent
                {
                    CurrentHealth = GameConfig.SoldierHealth,
                    MaxHealth = GameConfig.SoldierHealth
                });
                ecb.AddComponent(antEntity, new CombatComponent
                {
                    Damage = GameConfig.SoldierDamage,
                    AttackRange = GameConfig.SoldierAttackRange,
                    AttackCooldown = GameConfig.SoldierAttackCooldown,
                    AttackTimer = 0f,
                    Target = Entity.Null
                });
            }
        }
    }
}
