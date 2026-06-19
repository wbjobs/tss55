using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Components
{
    public struct VelocityComponent : IComponentData
    {
        public float2 Value;
        public float Speed;
    }
}
