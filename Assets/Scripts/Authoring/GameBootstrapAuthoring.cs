using UnityEngine;
using AntWar.Components;
using AntWar.Utils;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Authoring
{
    public class GameBootstrapAuthoring : MonoBehaviour
    {
        public int InitialWorkersPerTeam = 15;
        public int InitialSoldiersPerTeam = 5;

        public Vector2 RedNestPosition = new Vector2(-70, 0);
        public Vector2 BlueNestPosition = new Vector2(70, 0);

        private void Awake()
        {
            GameConfig.RedNestPosition = new float2(RedNestPosition.x, RedNestPosition.y);
            GameConfig.BlueNestPosition = new float2(BlueNestPosition.x, BlueNestPosition.y);

            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            BattleLogger.Clear();
            BattleLogger.Log("🐜 蚂蚁大战游戏开始！");
            BattleLogger.Log($"🔴 红方巢穴位置: ({RedNestPosition.x}, {RedNestPosition.y})");
            BattleLogger.Log($"🔵 蓝方巢穴位置: ({BlueNestPosition.x}, {BlueNestPosition.y})");

            CreateNest(entityManager, TeamType.Red, new float2(RedNestPosition.x, RedNestPosition.y));
            CreateNest(entityManager, TeamType.Blue, new float2(BlueNestPosition.x, BlueNestPosition.y));

            for (int i = 0; i < InitialWorkersPerTeam; i++)
            {
                SpawnAnt(entityManager, TeamType.Red, AntType.Worker, new float2(RedNestPosition.x, RedNestPosition.y));
                SpawnAnt(entityManager, TeamType.Blue, AntType.Worker, new float2(BlueNestPosition.x, BlueNestPosition.y));
            }

            for (int i = 0; i < InitialSoldiersPerTeam; i++)
            {
                SpawnAnt(entityManager, TeamType.Red, AntType.Soldier, new float2(RedNestPosition.x, RedNestPosition.y));
                SpawnAnt(entityManager, TeamType.Blue, AntType.Soldier, new float2(BlueNestPosition.x, BlueNestPosition.y));
            }

            BattleLogger.Log($"🐜 初始蚂蚁已生成: 每队 {InitialWorkersPerTeam} 工蚁 + {InitialSoldiersPerTeam} 兵蚁");

            SpawnFruitTrees(entityManager);
        }

        private void CreateNest(EntityManager entityManager, TeamType team, float2 position)
        {
            Entity nestEntity = entityManager.CreateEntity();

            entityManager.AddComponentData(nestEntity, new PositionComponent { Value = position });
            entityManager.AddComponentData(nestEntity, new TeamComponent { Team = team });
            entityManager.AddComponentData(nestEntity, new NestComponent
            {
                StoredFood = 100f,
                SpawnTimer = 5f,
                SpawnInterval = GameConfig.NestWorkerSpawnInterval,
                WorkerCount = 0,
                SoldierCount = 0,
                FoodPerSpawn = GameConfig.FoodPerWorkerSpawn
            });
            entityManager.AddComponentData(nestEntity, new StrategyComponent
            {
                Strategy = StrategyType.None,
                StrategyTarget = float2.zero,
                StrategyRadius = 0f,
                Priority = 0f
            });
            entityManager.AddComponent<NestTag>(nestEntity);
        }

        private void SpawnAnt(EntityManager entityManager, TeamType team, AntType antType, float2 nestPos)
        {
            Entity antEntity = entityManager.CreateEntity();

            Random random = Random.CreateFromIndex((uint)(antEntity.Index + 1));
            float2 offset = MathUtils.RandomDirection(ref random) * 5f;
            float2 spawnPos = nestPos + offset;

            entityManager.AddComponentData(antEntity, new PositionComponent { Value = spawnPos });
            entityManager.AddComponentData(antEntity, new VelocityComponent
            {
                Value = float2.zero,
                Speed = antType == AntType.Worker ? GameConfig.WorkerSpeed : GameConfig.SoldierSpeed
            });
            entityManager.AddComponentData(antEntity, new TeamComponent { Team = team });
            entityManager.AddComponentData(antEntity, new AntTypeComponent { Type = antType });
            entityManager.AddComponentData(antEntity, new AntStateComponent
            {
                CurrentState = AntState.Idle,
                HasTarget = false,
                TargetEntity = Entity.Null,
                TargetPosition = float2.zero
            });
            entityManager.AddComponentData(antEntity, new StrategyComponent
            {
                Strategy = StrategyType.None,
                StrategyTarget = float2.zero,
                StrategyRadius = 0f,
                Priority = 0f
            });
            entityManager.AddComponentData(antEntity, new AvoidanceComponent
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
                entityManager.AddComponent<WorkerAntTag>(antEntity);
                entityManager.AddComponentData(antEntity, new CarryComponent
                {
                    CarriedFood = 0f,
                    MaxCarryCapacity = GameConfig.WorkerCarryCapacity,
                    IsCarrying = false
                });
                entityManager.AddComponentData(antEntity, new HealthComponent
                {
                    CurrentHealth = GameConfig.WorkerHealth,
                    MaxHealth = GameConfig.WorkerHealth
                });
                entityManager.AddComponentData(antEntity, new CombatComponent
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
                entityManager.AddComponent<SoldierAntTag>(antEntity);
                entityManager.AddComponentData(antEntity, new CarryComponent
                {
                    CarriedFood = 0f,
                    MaxCarryCapacity = 0f,
                    IsCarrying = false
                });
                entityManager.AddComponentData(antEntity, new HealthComponent
                {
                    CurrentHealth = GameConfig.SoldierHealth,
                    MaxHealth = GameConfig.SoldierHealth
                });
                entityManager.AddComponentData(antEntity, new CombatComponent
                {
                    Damage = GameConfig.SoldierDamage,
                    AttackRange = GameConfig.SoldierAttackRange,
                    AttackCooldown = GameConfig.SoldierAttackCooldown,
                    AttackTimer = 0f,
                    Target = Entity.Null
                });
            }
        }

        private void SpawnFruitTrees(EntityManager entityManager)
        {
            Random random = Random.CreateFromIndex(42);
            int spawned = 0;
            int maxAttempts = GameConfig.FruitTreeCount * 5;
            int attempts = 0;

            while (spawned < GameConfig.FruitTreeCount && attempts < maxAttempts)
            {
                attempts++;
                float x = random.NextFloat(-GameConfig.MapWidth / 2f + 10f, GameConfig.MapWidth / 2f - 10f);
                float y = random.NextFloat(-GameConfig.MapHeight / 2f + 10f, GameConfig.MapHeight / 2f - 10f);
                float2 pos = new float2(x, y);

                if (math.distance(pos, GameConfig.RedNestPosition) < GameConfig.FruitTreeMinDistanceFromNest ||
                    math.distance(pos, GameConfig.BlueNestPosition) < GameConfig.FruitTreeMinDistanceFromNest)
                {
                    continue;
                }

                var treeQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<FruitTreeTag>(),
                    ComponentType.ReadOnly<PositionComponent>());
                using var treePositions = treeQuery.ToComponentDataArray<PositionComponent>(Unity.Collections.Allocator.Temp);
                bool tooClose = false;
                for (int t = 0; t < treePositions.Length; t++)
                {
                    if (math.distance(pos, treePositions[t].Value) < 15f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                    continue;

                float yield = random.NextFloat(GameConfig.MinFruitTreeYield, GameConfig.MaxFruitTreeYield);
                CreateFruitTree(entityManager, pos, yield);
                spawned++;
            }

            BattleLogger.Log($"� 地图上生成了 {spawned} 棵果树");
        }

        private void CreateFruitTree(EntityManager entityManager, float2 position, float yield)
        {
            Entity treeEntity = entityManager.CreateEntity();

            entityManager.AddComponentData(treeEntity, new PositionComponent { Value = position });
            entityManager.AddComponentData(treeEntity, new FoodComponent
            {
                Amount = yield,
                MaxAmount = yield
            });
            entityManager.AddComponentData(treeEntity, new FruitTreeComponent
            {
                RegrowTimer = 0f,
                RegrowInterval = GameConfig.FruitTreeRegrowInterval,
                IsRegenerating = false,
                MaxYield = yield
            });
            entityManager.AddComponent<FoodTag>(treeEntity);
            entityManager.AddComponent<FruitTreeTag>(treeEntity);

            BattleLogger.LogFruitTreeGrown(position, yield);
        }

        public void SetTeamStrategy(TeamType team, StrategyType strategy, Vector2 target, float radius)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<StrategyComponent>(),
                ComponentType.ReadOnly<TeamComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                var teamComp = entityManager.GetComponentData<TeamComponent>(entity);
                if (teamComp.Team == team)
                {
                    var strategyComp = entityManager.GetComponentData<StrategyComponent>(entity);
                    strategyComp.Strategy = strategy;
                    strategyComp.StrategyTarget = new float2(target.x, target.y);
                    strategyComp.StrategyRadius = radius;
                    entityManager.SetComponentData(entity, strategyComp);
                }
            }

            BattleLogger.LogStrategy(
                team == TeamType.Red ? "红方" : "蓝方",
                GetStrategyName(strategy));
        }

        private string GetStrategyName(StrategyType strategy)
        {
            switch (strategy)
            {
                case StrategyType.GatherArea: return "区域采集";
                case StrategyType.Defend: return "防守";
                case StrategyType.Attack: return "进攻";
                case StrategyType.Retreat: return "撤退";
                default: return "无";
            }
        }
    }
}
