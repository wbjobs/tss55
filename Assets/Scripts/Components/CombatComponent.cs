using Unity.Entities;

namespace AntWar.Components
{
    public struct CombatComponent : IComponentData
    {
        public float Damage;
        public float AttackRange;
        public float AttackCooldown;
        public float AttackTimer;
        public Entity Target;
    }
}
