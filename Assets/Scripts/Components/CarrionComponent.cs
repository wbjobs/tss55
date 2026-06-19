using Unity.Entities;

namespace AntWar.Components
{
    public struct CarrionComponent : IComponentData
    {
        public float DecayTimer;
        public float DecayTime;
    }

    public struct CarrionTag : IComponentData { }
}
