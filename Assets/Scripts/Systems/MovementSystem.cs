using AntWar.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(WorkerAntSystem))]
    [UpdateBefore(typeof(SoldierAntSystem))]
    public partial struct MovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PositionComponent>();
            state.RequireForUpdate<VelocityComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (position, velocity) in
                     SystemAPI.Query<RefRW<PositionComponent>, RefRO<VelocityComponent>>())
            {
                float2 pos = position.ValueRW.Value;
                float2 vel = velocity.ValueRO.Value;
                float speed = velocity.ValueRO.Speed;

                if (math.lengthsq(vel) > 0.0001f)
                {
                    float2 normalized = math.normalize(vel);
                    pos += normalized * speed * deltaTime;
                }

                pos.x = math.clamp(pos.x, -GameConfig.MapWidth / 2f, GameConfig.MapWidth / 2f);
                pos.y = math.clamp(pos.y, -GameConfig.MapHeight / 2f, GameConfig.MapHeight / 2f);

                position.ValueRW.Value = pos;
            }
        }
    }
}
