using Unity.Mathematics;

namespace AntWar.Utils
{
    public static class MathUtils
    {
        public static float Distance(float2 a, float2 b)
        {
            float2 diff = a - b;
            return math.sqrt(diff.x * diff.x + diff.y * diff.y);
        }

        public static float DistanceSq(float2 a, float2 b)
        {
            float2 diff = a - b;
            return diff.x * diff.x + diff.y * diff.y;
        }

        public static float2 Normalize(float2 v)
        {
            float len = math.sqrt(v.x * v.x + v.y * v.y);
            if (len < 0.0001f)
                return float2.zero;
            return v / len;
        }

        public static float2 Direction(float2 from, float2 to)
        {
            return Normalize(to - from);
        }

        public static float2 RandomDirection(ref Random random)
        {
            float angle = random.NextFloat(0f, 2f * math.PI);
            return new float2(math.cos(angle), math.sin(angle));
        }

        public static float2 RandomPosition(float2 center, float radius, ref Random random)
        {
            float2 dir = RandomDirection(ref random);
            float dist = random.NextFloat(0f, radius);
            return center + dir * dist;
        }

        public static bool IsInRange(float2 pos, float2 center, float radius)
        {
            return DistanceSq(pos, center) <= radius * radius;
        }
    }
}
