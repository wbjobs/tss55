using AntWar.Components;
using AntWar.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FruitTreeSystem))]
    public partial struct CarrionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (carrion, food, position, entity) in
                     SystemAPI.Query<
                         RefRW<CarrionComponent>,
                         RefRO<FoodComponent>,
                         RefRO<PositionComponent>>()
                     .WithAll<CarrionTag>().WithEntityAccess())
            {
                carrion.ValueRW.DecayTimer -= deltaTime;

                if (carrion.ValueRO.DecayTimer <= 0f)
                {
                    BattleLogger.LogCarrionDecayed(position.ValueRO.Value);
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (food.ValueRO.Amount <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public static void SpawnCarrionOnDeath(EntityManager entityManager,
            float2 position, AntType deadAntType)
        {
            float yieldAmount = deadAntType == AntType.Soldier
                ? GameConfig.CarrionPerSoldier
                : GameConfig.CarrionPerWorker;

            if (yieldAmount <= 0f)
                return;

            Entity carrionEntity = entityManager.CreateEntity();

            entityManager.AddComponentData(carrionEntity, new PositionComponent { Value = position });
            entityManager.AddComponentData(carrionEntity, new FoodComponent
            {
                Amount = yieldAmount,
                MaxAmount = yieldAmount
            });
            entityManager.AddComponentData(carrionEntity, new CarrionComponent
            {
                DecayTimer = GameConfig.CarrionDecayTime,
                DecayTime = GameConfig.CarrionDecayTime
            });
            entityManager.AddComponent<FoodTag>(carrionEntity);
            entityManager.AddComponent<CarrionTag>(carrionEntity);

            string source = deadAntType == AntType.Soldier ? "兵蚁尸体" : "工蚁尸体";
            BattleLogger.LogCarrionSpawned(position, yieldAmount, source);
        }
    }
}
