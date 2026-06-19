using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Components
{
    public struct PositionComponent : IComponentData
    {
        public float2 Value;
    }
}
