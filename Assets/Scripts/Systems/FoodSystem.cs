using AntWar.Components;
using Unity.Entities;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CarrionSystem))]
    public partial struct FoodSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);

            foreach (var (food, entity) in
                     SystemAPI.Query<RefRO<FoodComponent>>()
                     .WithAll<FoodTag>()
                     .WithNone<FruitTreeTag, CarrionTag>()
                     .WithEntityAccess())
            {
                if (food.ValueRO.Amount <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
