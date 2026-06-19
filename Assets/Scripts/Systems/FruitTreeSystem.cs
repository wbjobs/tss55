using AntWar.Components;
using AntWar.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NestSystem))]
    public partial struct FruitTreeSystem : ISystem
    {
        private EntityQuery _fruitTreeQuery;
        private float _randomSpawnTimer;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FruitTreeTag>();

            _fruitTreeQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<FruitTreeComponent>(),
                ComponentType.ReadWrite<FoodComponent>(),
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<FruitTreeTag>());

            _randomSpawnTimer = GameConfig.FruitTreeRandomSpawnInterval * 0.5f;
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (tree, food, position, treeEntity) in
                     SystemAPI.Query<
                         RefRW<FruitTreeComponent>,
                         RefRW<FoodComponent>,
                         RefRO<PositionComponent>>()
                     .WithAll<FruitTreeTag>().WithEntityAccess())
            {
                if (tree.ValueRO.IsRegenerating)
                {
                    tree.ValueRW.RegrowTimer -= deltaTime;
                    if (tree.ValueRO.RegrowTimer <= 0f)
                    {
                        tree.ValueRW.IsRegenerating = false;
                        tree.ValueRW.RegrowTimer = 0f;
                        food.ValueRW.Amount = tree.ValueRO.MaxYield;
                        food.ValueRW.MaxAmount = tree.ValueRO.MaxYield;

                        BattleLogger.LogFruitTreeRegrow(position.ValueRO.Value);
                    }
                }
                else
                {
                    if (food.ValueRO.Amount <= 0f)
                    {
                        tree.ValueRW.IsRegenerating = true;
                        tree.ValueRW.RegrowTimer = tree.ValueRO.RegrowInterval;
                    }
                }
            }

            _randomSpawnTimer -= deltaTime;
            if (_randomSpawnTimer <= 0f)
            {
                _randomSpawnTimer = GameConfig.FruitTreeRandomSpawnInterval;
                TrySpawnRandomTree(ref state);
            }
        }

        private void TrySpawnRandomTree(ref SystemState state)
        {
            if (_fruitTreeQuery.CalculateEntityCount() >= GameConfig.MaxFruitTrees)
                return;

            Random rng = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000f));

            int attempts = 20;
            for (int i = 0; i < attempts; i++)
            {
                float x = rng.NextFloat(-GameConfig.MapWidth / 2f + 10f, GameConfig.MapWidth / 2f - 10f);
                float y = rng.NextFloat(-GameConfig.MapHeight / 2f + 10f, GameConfig.MapHeight / 2f - 10f);
                float2 pos = new float2(x, y);

                if (math.distance(pos, GameConfig.RedNestPosition) < GameConfig.FruitTreeMinDistanceFromNest ||
                    math.distance(pos, GameConfig.BlueNestPosition) < GameConfig.FruitTreeMinDistanceFromNest)
                    continue;

                bool tooClose = false;
                using var positions = _fruitTreeQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
                for (int j = 0; j < positions.Length; j++)
                {
                    if (math.distance(pos, positions[j].Value) < 15f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                    continue;

                SpawnFruitTree(ref state, pos, ref rng);
                return;
            }
        }

        public static Entity SpawnFruitTree(ref SystemState state, float2 position, ref Random rng)
        {
            return SpawnFruitTree(state.EntityManager, position, ref rng);
        }

        public static Entity SpawnFruitTree(EntityManager entityManager, float2 position, ref Random rng)
        {
            Entity treeEntity = entityManager.CreateEntity();

            float yield = rng.NextFloat(GameConfig.MinFruitTreeYield, GameConfig.MaxFruitTreeYield);

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
            return treeEntity;
        }
    }
}
