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

        public const float NestRadius = 8f;
        public const float NestPositionOffset = 70f;

        public static float2 RedNestPosition = new float2(-NestPositionOffset, 0);
        public static float2 BlueNestPosition = new float2(NestPositionOffset, 0);

        public const float SoldierDetectionRange = 15f;
        public const float WorkerDetectionRange = 10f;

        public const float SeparationRadius = 3f;
        public const float SeparationForce = 2f;

        public const float BoundaryAvoidanceMargin = 15f;
        public const float BoundaryAvoidanceForce = 3f;

        public const float StagnationThreshold = 0.5f;
        public const float StagnationTimeLimit = 1.5f;

        public const float DetourDuration = 2f;

        public const float CongestionCheckRadius = 8f;
        public const float CongestionPenaltyWeight = 5f;

        public const float TargetScatterRadius = 2f;

        public const int FruitTreeCount = 10;
        public const float MinFruitTreeYield = 40f;
        public const float MaxFruitTreeYield = 120f;
        public const float FruitTreeRegrowInterval = 20f;
        public const float FruitTreeMinDistanceFromNest = 20f;
        public const float FruitTreeRadius = 2.5f;
        public const float FruitTreeRandomSpawnInterval = 60f;
        public const int MaxFruitTrees = 15;

        public const float CarrionPerWorker = 15f;
        public const float CarrionPerSoldier = 35f;
        public const float CarrionDecayTime = 45f;
        public const float CarrionRadius = 1.2f;
    }
}
