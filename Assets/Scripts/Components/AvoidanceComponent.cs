using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Components
{
    public struct AvoidanceComponent : IComponentData
    {
        public float2 PreviousPosition;
        public float StagnationTimer;
        public float2 AvoidanceOffset;
        public int StuckCount;
        public float DetourCooldown;
        public float2 DetourDirection;
        public float RandomSeed;
    }
}
