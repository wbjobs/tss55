using Unity.Entities;

namespace AntWar.Components
{
    public enum AntType : byte
    {
        Worker = 0,
        Soldier = 1
    }

    public struct AntTypeComponent : IComponentData
    {
        public AntType Type;
    }
}
