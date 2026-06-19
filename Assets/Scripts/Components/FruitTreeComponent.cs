using Unity.Entities;

namespace AntWar.Components
{
    public struct FruitTreeComponent : IComponentData
    {
        public float RegrowTimer;
        public float RegrowInterval;
        public bool IsRegenerating;
        public float MaxYield;
    }

    public struct FruitTreeTag : IComponentData { }
}
