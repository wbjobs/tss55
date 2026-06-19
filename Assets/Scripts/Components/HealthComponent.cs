using Unity.Entities;

namespace AntWar.Components
{
    public struct HealthComponent : IComponentData
    {
        public float CurrentHealth;
        public float MaxHealth;
    }
}
