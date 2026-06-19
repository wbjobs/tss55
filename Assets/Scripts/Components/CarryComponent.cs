using Unity.Entities;

namespace AntWar.Components
{
    public struct CarryComponent : IComponentData
    {
        public float CarriedFood;
        public float MaxCarryCapacity;
        public bool IsCarrying;
    }
}
