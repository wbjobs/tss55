using Unity.Mathematics;

namespace AntWar.Utils
{
    public static class GameConfig
    {
        public const float MapWidth = 200f;
        public const float MapHeight = 200f;

        public const float WorkerSpeed = 8f;
        public const float SoldierSpeed = 10f;

        public const float WorkerHealth = 30f;
        public const float SoldierHealth = 80f;

        public const float SoldierDamage = 10f;
        public const float SoldierAttackRange = 3f;
        public const float SoldierAttackCooldown = 0.8f;

        public const float WorkerCarryCapacity = 10f;
        public const float FoodGatherRate = 2f;
        public const float FoodGatherRange = 1.5f;

        public const int InitialWorkerCount = 15;
        public const int InitialSoldierCount = 5;

        public const float NestWorkerSpawnInterval = 8f;
        public const float NestSoldierSpawnInterval = 15f;
        public const float FoodPerWorkerSpawn = 20f;
        public const float FoodPerSoldierSpawn = 50f;

        public const int FoodPointCount = 12;
        public const float MinFoodAmount = 30f;
        public const float MaxFoodAmount = 100f;

        public const float NestRadius = 8f;
        public const float NestPositionOffset = 70f;

        public static float2 RedNestPosition = new float2(-NestPositionOffset, 0);
        public static float2 BlueNestPosition = new float2(NestPositionOffset, 0);

        public const float SoldierDetectionRange = 15f;
        public const float WorkerDetectionRange = 10f;
    }
}
