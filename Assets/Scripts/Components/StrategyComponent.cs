using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Components
{
    public enum StrategyType : byte
    {
        None = 0,
        GatherArea = 1,
        Defend = 2,
        Attack = 3,
        Retreat = 4
    }

    public struct StrategyComponent : IComponentData
    {
        public StrategyType Strategy;
        public float2 StrategyTarget;
        public float StrategyRadius;
        public float Priority;
    }
}
