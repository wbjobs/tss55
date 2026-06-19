using Unity.Entities;

namespace AntWar.Components
{
    public struct NestComponent : IComponentData
    {
        public float StoredFood;
        public float SpawnTimer;
        public float SpawnInterval;
        public int WorkerCount;
        public int SoldierCount;
        public float FoodPerSpawn;
    }
}
