using UnityEngine;
using AntWar.Components;
using AntWar.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace AntWar.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        private EntityManager _entityManager;
        private World _defaultWorld;

        public int InitialWorkersPerTeam = 15;
        public int InitialSoldiersPerTeam = 5;
        public int FoodPointCount = 12;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeGame();
        }

        private void InitializeGame()
        {
            _defaultWorld = World.DefaultGameObjectInjectionWorld;
            _entityManager = _defaultWorld.EntityManager;

            BattleLogger.Clear();
            BattleLogger.Log("🐜 蚂蚁大战游戏开始！");
            BattleLogger.Log("🔴 红方巢穴位置: " + GameConfig.RedNestPosition);
            BattleLogger.Log("🔵 蓝方巢穴位置: " + GameConfig.BlueNestPosition);

            SpawnNests();
            SpawnInitialAnts();
            SpawnFoodPoints();
        }

        private void SpawnNests()
        {
            CreateNest(TeamType.Red, GameConfig.RedNestPosition);
            CreateNest(TeamType.Blue, GameConfig.BlueNestPosition);

            BattleLogger.Log("🏠 双方巢穴已建立");
        }

        private void CreateNest(TeamType team, float2 position)
        {
            Entity nestEntity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(nestEntity, new PositionComponent { Value = position });
            _entityManager.AddComponentData(nestEntity, new TeamComponent { Team = team });
            _entityManager.AddComponentData(nestEntity, new NestComponent
            {
                StoredFood = 100f,
                SpawnTimer = 5f,
                SpawnInterval = GameConfig.NestWorkerSpawnInterval,
                WorkerCount = 0,
                SoldierCount = 0,
                FoodPerSpawn = GameConfig.FoodPerWorkerSpawn
            });
            _entityManager.AddComponentData(nestEntity, new StrategyComponent
            {
                Strategy = StrategyType.None,
                StrategyTarget = float2.zero,
                StrategyRadius = 0f,
                Priority = 0f
            });
            _entityManager.AddComponent<NestTag>(nestEntity);
        }

        private void SpawnInitialAnts()
        {
            for (int i = 0; i < InitialWorkersPerTeam; i++)
            {
                SpawnAnt(TeamType.Red, AntType.Worker, GameConfig.RedNestPosition);
                SpawnAnt(TeamType.Blue, AntType.Worker, GameConfig.BlueNestPosition);
            }

            for (int i = 0; i < InitialSoldiersPerTeam; i++)
            {
                SpawnAnt(TeamType.Red, AntType.Soldier, GameConfig.RedNestPosition);
                SpawnAnt(TeamType.Blue, AntType.Soldier, GameConfig.BlueNestPosition);
            }

            BattleLogger.Log($"🐜 初始蚂蚁已生成: 每队 {InitialWorkersPerTeam} 工蚁 + {InitialSoldiersPerTeam} 兵蚁");
        }

        private void SpawnAnt(TeamType team, AntType antType, float2 nestPos)
        {
            Entity antEntity = _entityManager.CreateEntity();

            Random random = Random.CreateFromIndex((uint)(antEntity.Index + 1));
            float2 offset = MathUtils.RandomDirection(ref random) * 5f;
            float2 spawnPos = nestPos + offset;

            _entityManager.AddComponentData(antEntity, new PositionComponent { Value = spawnPos });
            _entityManager.AddComponentData(antEntity, new VelocityComponent
            {
                Value = float2.zero,
                Speed = antType == AntType.Worker ? GameConfig.WorkerSpeed : GameConfig.SoldierSpeed
            });
            _entityManager.AddComponentData(antEntity, new TeamComponent { Team = team });
            _entityManager.AddComponentData(antEntity, new AntTypeComponent { Type = antType });
            _entityManager.AddComponentData(antEntity, new AntStateComponent
            {
                CurrentState = AntState.Idle,
                HasTarget = false,
                TargetEntity = Entity.Null,
                TargetPosition = float2.zero
            });
            _entityManager.AddComponentData(antEntity, new StrategyComponent
            {
                Strategy = StrategyType.None,
                StrategyTarget = float2.zero,
                StrategyRadius = 0f,
                Priority = 0f
            });
            _entityManager.AddComponentData(antEntity, new AvoidanceComponent
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
                _entityManager.AddComponent<WorkerAntTag>(antEntity);
                _entityManager.AddComponentData(antEntity, new CarryComponent
                {
                    CarriedFood = 0f,
                    MaxCarryCapacity = GameConfig.WorkerCarryCapacity,
                    IsCarrying = false
                });
                _entityManager.AddComponentData(antEntity, new HealthComponent
                {
                    CurrentHealth = GameConfig.WorkerHealth,
                    MaxHealth = GameConfig.WorkerHealth
                });
                _entityManager.AddComponentData(antEntity, new CombatComponent
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
                _entityManager.AddComponent<SoldierAntTag>(antEntity);
                _entityManager.AddComponentData(antEntity, new CarryComponent
                {
                    CarriedFood = 0f,
                    MaxCarryCapacity = 0f,
                    IsCarrying = false
                });
                _entityManager.AddComponentData(antEntity, new HealthComponent
                {
                    CurrentHealth = GameConfig.SoldierHealth,
                    MaxHealth = GameConfig.SoldierHealth
                });
                _entityManager.AddComponentData(antEntity, new CombatComponent
                {
                    Damage = GameConfig.SoldierDamage,
                    AttackRange = GameConfig.SoldierAttackRange,
                    AttackCooldown = GameConfig.SoldierAttackCooldown,
                    AttackTimer = 0f,
                    Target = Entity.Null
                });
            }
        }

        private void SpawnFoodPoints()
        {
            Random random = Random.CreateFromIndex(42);

            for (int i = 0; i < FoodPointCount; i++)
            {
                float x = random.NextFloat(-GameConfig.MapWidth / 2f + 10f, GameConfig.MapWidth / 2f - 10f);
                float y = random.NextFloat(-GameConfig.MapHeight / 2f + 10f, GameConfig.MapHeight / 2f - 10f);
                float2 pos = new float2(x, y);

                if (math.distance(pos, GameConfig.RedNestPosition) < 20f ||
                    math.distance(pos, GameConfig.BlueNestPosition) < 20f)
                {
                    i--;
                    continue;
                }

                float amount = random.NextFloat(GameConfig.MinFoodAmount, GameConfig.MaxFoodAmount);
                CreateFoodPoint(pos, amount);
            }

            BattleLogger.Log($"🍞 地图上生成了 {FoodPointCount} 个食物点");
        }

        private void CreateFoodPoint(float2 position, float amount)
        {
            Entity foodEntity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(foodEntity, new PositionComponent { Value = position });
            _entityManager.AddComponentData(foodEntity, new FoodComponent
            {
                Amount = amount,
                MaxAmount = amount
            });
            _entityManager.AddComponent<FoodTag>(foodEntity);
        }

        public void SetRedGatherStrategy(float2 target, float radius)
        {
            SetTeamStrategy(TeamType.Red, StrategyType.GatherArea, target, radius);
        }

        public void SetBlueGatherStrategy(float2 target, float radius)
        {
            SetTeamStrategy(TeamType.Blue, StrategyType.GatherArea, target, radius);
        }

        public void SetRedDefendStrategy()
        {
            SetTeamStrategy(TeamType.Red, StrategyType.Defend, GameConfig.RedNestPosition, 30f);
        }

        public void SetBlueDefendStrategy()
        {
            SetTeamStrategy(TeamType.Blue, StrategyType.Defend, GameConfig.BlueNestPosition, 30f);
        }

        public void SetRedAttackStrategy(float2 target, float radius)
        {
            SetTeamStrategy(TeamType.Red, StrategyType.Attack, target, radius);
        }

        public void SetBlueAttackStrategy(float2 target, float radius)
        {
            SetTeamStrategy(TeamType.Blue, StrategyType.Attack, target, radius);
        }

        private void SetTeamStrategy(TeamType team, StrategyType strategy, float2 target, float radius)
        {
            var query = _entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<StrategyComponent>(),
                ComponentType.ReadOnly<TeamComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                var teamComp = _entityManager.GetComponentData<TeamComponent>(entity);
                if (teamComp.Team == team)
                {
                    var strategyComp = _entityManager.GetComponentData<StrategyComponent>(entity);
                    strategyComp.Strategy = strategy;
                    strategyComp.StrategyTarget = target;
                    strategyComp.StrategyRadius = radius;
                    _entityManager.SetComponentData(entity, strategyComp);
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

        private void OnGUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("🐜 蚂蚁大战 - ECS策略游戏", GUILayout.Width(250));

            GUILayout.Space(10);
            GUILayout.Label("【红方策略】");

            if (GUILayout.Button("工蚁优先采集左上区域", GUILayout.Width(220)))
            {
                SetRedGatherStrategy(new float2(-60f, 60f), 40f);
            }
            if (GUILayout.Button("工蚁优先采集右上区域", GUILayout.Width(220)))
            {
                SetRedGatherStrategy(new float2(60f, 60f), 40f);
            }
            if (GUILayout.Button("工蚁优先采集左下区域", GUILayout.Width(220)))
            {
                SetRedGatherStrategy(new float2(-60f, -60f), 40f);
            }
            if (GUILayout.Button("工蚁优先采集右下区域", GUILayout.Width(220)))
            {
                SetRedGatherStrategy(new float2(60f, -60f), 40f);
            }
            if (GUILayout.Button("兵蚁集中防守", GUILayout.Width(220)))
            {
                SetRedDefendStrategy();
            }
            if (GUILayout.Button("兵蚁进攻蓝方巢穴", GUILayout.Width(220)))
            {
                SetRedAttackStrategy(GameConfig.BlueNestPosition, 25f);
            }

            GUILayout.Space(10);
            GUILayout.Label("【蓝方策略】");

            if (GUILayout.Button("工蚁优先采集左上区域", GUILayout.Width(220)))
            {
                SetBlueGatherStrategy(new float2(-60f, 60f), 40f);
            }
            if (GUILayout.Button("工蚁优先采集右上区域", GUILayout.Width(220)))
            {
                SetBlueGatherStrategy(new float2(60f, 60f), 40f);
            }
            if (GUILayout.Button("工蚁优先采集左下区域", GUILayout.Width(220)))
            {
                SetBlueGatherStrategy(new float2(-60f, -60f), 40f);
            }
            if (GUILayout.Button("工蚁优先采集右下区域", GUILayout.Width(220)))
            {
                SetBlueGatherStrategy(new float2(60f, -60f), 40f);
            }
            if (GUILayout.Button("兵蚁集中防守", GUILayout.Width(220)))
            {
                SetBlueDefendStrategy();
            }
            if (GUILayout.Button("兵蚁进攻红方巢穴", GUILayout.Width(220)))
            {
                SetBlueAttackStrategy(GameConfig.RedNestPosition, 25f);
            }

            GUILayout.EndVertical();
        }
    }
}
