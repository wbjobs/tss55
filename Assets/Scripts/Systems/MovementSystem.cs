using AntWar.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AntAvoidanceSystem))]
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
            float halfMapW = GameConfig.MapWidth / 2f;
            float halfMapH = GameConfig.MapHeight / 2f;

            foreach (var (position, velocity) in
                     SystemAPI.Query<RefRW<PositionComponent>, RefRW<VelocityComponent>>())
            {
                float2 pos = position.ValueRW.Value;
                float2 vel = velocity.ValueRW.Value;
                float speed = velocity.ValueRO.Speed;

                if (math.lengthsq(vel) > 0.0001f)
                {
                    float2 normalized = math.normalize(vel);
                    pos += normalized * speed * deltaTime;
                }

                bool hitBoundary = false;

                if (pos.x < -halfMapW)
                {
                    pos.x = -halfMapW + 0.1f;
                    hitBoundary = true;
                }
                else if (pos.x > halfMapW)
                {
                    pos.x = halfMapW - 0.1f;
                    hitBoundary = true;
                }

                if (pos.y < -halfMapH)
                {
                    pos.y = -halfMapH + 0.1f;
                    hitBoundary = true;
                }
                else if (pos.y > halfMapH)
                {
                    pos.y = halfMapH - 0.1f;
                    hitBoundary = true;
                }

                if (hitBoundary)
                {
                    velocity.ValueRW.Value = float2.zero;
                }

                position.ValueRW.Value = pos;
            }
        }
    }
}
