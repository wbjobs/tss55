using AntWar.Components;
using AntWar.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(MovementSystem))]
    public partial struct AntAvoidanceSystem : ISystem
    {
        private EntityQuery _antQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AvoidanceComponent>();

            _antQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<AvoidanceComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var antPositions = _antQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            var antAvoidances = _antQuery.ToComponentDataArray<AvoidanceComponent>(Allocator.Temp);
            int antCount = antPositions.Length;

            float separationRadius = GameConfig.SeparationRadius;
            float separationRadiusSq = separationRadius * separationRadius;
            float separationForce = GameConfig.SeparationForce;
            float halfMapW = GameConfig.MapWidth / 2f;
            float halfMapH = GameConfig.MapHeight / 2f;
            float boundaryMargin = GameConfig.BoundaryAvoidanceMargin;
            float boundaryForce = GameConfig.BoundaryAvoidanceForce;
            float stagnationThreshold = GameConfig.StagnationThreshold;
            float stagnationTimeLimit = GameConfig.StagnationTimeLimit;

            foreach (var (position, velocity, avoidance, antState) in
                     SystemAPI.Query<
                         RefRW<PositionComponent>,
                         RefRW<VelocityComponent>,
                         RefRW<AvoidanceComponent>,
                         RefRW<AntStateComponent>>())
            {
                float2 pos = position.ValueRO.Value;
                float2 currentVel = velocity.ValueRO.Value;
                float2 separationOffset = float2.zero;

                for (int i = 0; i < antCount; i++)
                {
                    float2 otherPos = antPositions[i].Value;
                    float2 diff = pos - otherPos;
                    float distSq = math.lengthsq(diff);

                    if (distSq < separationRadiusSq && distSq > 0.001f)
                    {
                        float dist = math.sqrt(distSq);
                        float2 awayDir = diff / dist;
                        float strength = (1f - dist / separationRadius) * separationForce;
                        separationOffset += awayDir * strength;
                    }
                }

                float2 boundaryOffset = float2.zero;
                float margin = boundaryMargin;

                if (pos.x < -halfMapW + margin)
                    boundaryOffset.x += boundaryForce * (1f - (pos.x + halfMapW) / margin);
                if (pos.x > halfMapW - margin)
                    boundaryOffset.x -= boundaryForce * (1f - (halfMapW - pos.x) / margin);
                if (pos.y < -halfMapH + margin)
                    boundaryOffset.y += boundaryForce * (1f - (pos.y + halfMapH) / margin);
                if (pos.y > halfMapH - margin)
                    boundaryOffset.y -= boundaryForce * (1f - (halfMapH - pos.y) / margin);

                float2 prevPos = avoidance.ValueRO.PreviousPosition;
                float movedDist = math.distance(pos, prevPos);
                float stagnationTimer = avoidance.ValueRO.StagnationTimer;
                int stuckCount = avoidance.ValueRO.StuckCount;
                float detourCooldown = avoidance.ValueRO.DetourCooldown;

                if (movedDist < stagnationThreshold && math.lengthsq(currentVel) > 0.01f)
                {
                    stagnationTimer += deltaTime;
                }
                else
                {
                    stagnationTimer = math.max(0f, stagnationTimer - deltaTime * 2f);
                }

                float2 detourDir = avoidance.ValueRO.DetourDirection;
                detourCooldown -= deltaTime;

                if (stagnationTimer > stagnationTimeLimit)
                {
                    stuckCount++;
                    stagnationTimer = 0f;
                    detourCooldown = GameConfig.DetourDuration + (float)stuckCount * 0.5f;

                    uint seed = (uint)(pos.x * 1000f + pos.y * 7919f + state.WorldUnmanaged.Time.ElapsedTime * 13f + stuckCount * 31f);
                    Random rng = Random.CreateFromIndex(seed);
                    float angle = rng.NextFloat(0f, 2f * math.PI);
                    detourDir = new float2(math.cos(angle), math.sin(angle));

                    antState.ValueRW.CurrentState = AntState.Idle;
                    antState.ValueRW.HasTarget = false;
                    antState.ValueRW.TargetEntity = Entity.Null;
                }

                if (detourCooldown <= 0f)
                {
                    detourDir = float2.zero;
                    if (stagnationTimer <= 0f)
                    {
                        stuckCount = math.max(0, stuckCount - 1);
                    }
                }

                float2 totalOffset = separationOffset + boundaryOffset;
                if (math.lengthsq(detourDir) > 0.001f)
                {
                    float detourStrength = math.min(detourCooldown * 0.5f, 3f);
                    totalOffset += detourDir * detourStrength;
                }

                avoidance.ValueRW.PreviousPosition = pos;
                avoidance.ValueRW.StagnationTimer = stagnationTimer;
                avoidance.ValueRW.StuckCount = stuckCount;
                avoidance.ValueRW.DetourCooldown = detourCooldown;
                avoidance.ValueRW.DetourDirection = detourDir;
                avoidance.ValueRW.AvoidanceOffset = totalOffset;

                float2 desiredVel = currentVel;
                if (math.lengthsq(totalOffset) > 0.001f)
                {
                    desiredVel = currentVel + totalOffset;
                }

                if (math.lengthsq(desiredVel) > 0.001f)
                {
                    velocity.ValueRW.Value = math.normalize(desiredVel);
                }
            }

            antPositions.Dispose();
            antAvoidances.Dispose();
        }
    }
}
