using Unity.Entities;

namespace AntWar.Components
{
    public struct FoodComponent : IComponentData
    {
        public float Amount;
        public float MaxAmount;
    }
}
