using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Components
{
    public enum AntState : byte
    {
        Idle = 0,
        SeekingFood = 1,
        ReturningFood = 2,
        Attacking = 3,
        Fleeing = 4,
        Defending = 5
    }

    public struct AntStateComponent : IComponentData
    {
        public AntState CurrentState;
        public Entity TargetEntity;
        public float2 TargetPosition;
        public bool HasTarget;
    }
}
