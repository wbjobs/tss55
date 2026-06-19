using Unity.Entities;

namespace AntWar.Components
{
    public enum TeamType : byte
    {
        Red = 0,
        Blue = 1
    }

    public struct TeamComponent : IComponentData
    {
        public TeamType Team;
    }
}
