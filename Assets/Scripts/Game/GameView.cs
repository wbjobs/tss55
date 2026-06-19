using UnityEngine;
using AntWar.Components;
using AntWar.Utils;
using Unity.Entities;
using Unity.Mathematics;

namespace AntWar.Game
{
    public class GameView : MonoBehaviour
    {
        public static GameView Instance { get; private set; }

        public GameObject AntPrefab;
        public GameObject FoodPrefab;
        public GameObject NestPrefab;

        private EntityManager _entityManager;
        private World _defaultWorld;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _defaultWorld = World.DefaultGameObjectInjectionWorld;
            _entityManager = _defaultWorld.EntityManager;
        }

        private void OnDrawGizmos()
        {
            if (_defaultWorld == null || !_defaultWorld.IsCreated)
                return;

            var entityManager = _defaultWorld.EntityManager;

            DrawNests(entityManager);
            DrawFoods(entityManager);
            DrawAnts(entityManager);
            DrawMapBorder();
        }

        private void DrawMapBorder()
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(GameConfig.MapWidth, GameConfig.MapHeight, 0.1f));
        }

        private void DrawNests(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NestTag>(),
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<TeamComponent>(),
                ComponentType.ReadOnly<NestComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var positions = query.ToComponentDataArray<PositionComponent>(Unity.Collections.Allocator.Temp);
            using var teams = query.ToComponentDataArray<TeamComponent>(Unity.Collections.Allocator.Temp);
            using var nests = query.ToComponentDataArray<NestComponent>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                float2 pos = positions[i].Value;
                TeamType team = teams[i].Team;
                NestComponent nest = nests[i];

                Gizmos.color = team == TeamType.Red ? Color.red : Color.blue;
                Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), GameConfig.NestRadius);

                Color fillColor = team == TeamType.Red
                    ? new Color(1f, 0.5f, 0.5f, 0.3f)
                    : new Color(0.5f, 0.5f, 1f, 0.3f);
                Gizmos.color = fillColor;
                Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), GameConfig.NestRadius * 0.8f);

                Vector3 textPos = new Vector3(pos.x, pos.y + GameConfig.NestRadius + 2f, 0);
            }
        }

        private void DrawFoods(EntityManager entityManager)
        {
            DrawFruitTrees(entityManager);
            DrawCarrions(entityManager);
        }

        private void DrawFruitTrees(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FruitTreeTag>(),
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<FoodComponent>(),
                ComponentType.ReadOnly<FruitTreeComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var positions = query.ToComponentDataArray<PositionComponent>(Unity.Collections.Allocator.Temp);
            using var foods = query.ToComponentDataArray<FoodComponent>(Unity.Collections.Allocator.Temp);
            using var trees = query.ToComponentDataArray<FruitTreeComponent>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                float2 pos = positions[i].Value;
                float amount = foods[i].Amount;
                float maxAmount = foods[i].MaxAmount;
                bool isRegen = trees[i].IsRegenerating;

                float trunkSize = GameConfig.FruitTreeRadius;
                float fruitScale = Mathf.Lerp(0.3f, 1f, amount / Mathf.Max(1f, maxAmount));

                Gizmos.color = new Color(0.35f, 0.2f, 0.1f, 0.9f);
                Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), trunkSize * 0.6f);

                if (!isRegen && amount > 0f)
                {
                    Gizmos.color = new Color(0.2f, 0.55f, 0.2f, 0.85f);
                    Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), trunkSize * 1.4f);

                    Color fruitColor = new Color(0.85f, 0.2f, 0.2f, 0.95f);
                    Gizmos.color = fruitColor;
                    Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), trunkSize * 1.4f * fruitScale);

                    int fruitCount = Mathf.CeilToInt(amount / 10f);
                    for (int f = 0; f < fruitCount; f++)
                    {
                        float angle = (f * Mathf.PI * 2f / Mathf.Max(1, fruitCount)) + pos.x * 0.1f;
                        float radius = trunkSize * 1.1f * fruitScale;
                        Vector3 fruitPos = new Vector3(
                            pos.x + Mathf.Cos(angle) * radius,
                            pos.y + Mathf.Sin(angle) * radius,
                            0f);
                        Gizmos.color = fruitColor;
                        Gizmos.DrawSphere(fruitPos, 0.6f);
                    }
                }
                else if (isRegen)
                {
                    Gizmos.color = new Color(0.4f, 0.35f, 0.2f, 0.6f);
                    Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), trunkSize * 1.1f);

                    float progress = 1f - (trees[i].RegrowTimer / Mathf.Max(0.01f, trees[i].RegrowInterval));
                    Gizmos.color = Color.Lerp(new Color(0.6f, 0.55f, 0.3f), new Color(0.2f, 0.6f, 0.2f), progress);
                    Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), trunkSize * (1.1f + progress * 0.3f));
                }
            }
        }

        private void DrawCarrions(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CarrionTag>(),
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<FoodComponent>(),
                ComponentType.ReadOnly<CarrionComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var positions = query.ToComponentDataArray<PositionComponent>(Unity.Collections.Allocator.Temp);
            using var foods = query.ToComponentDataArray<FoodComponent>(Unity.Collections.Allocator.Temp);
            using var carrions = query.ToComponentDataArray<CarrionComponent>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                float2 pos = positions[i].Value;
                float amount = foods[i].Amount;
                float maxAmount = foods[i].MaxAmount;
                float decayProgress = 1f - (carrions[i].DecayTimer / Mathf.Max(0.01f, carrions[i].DecayTime));

                float sizeScale = Mathf.Lerp(0.5f, 1f, amount / Mathf.Max(1f, maxAmount));
                float alpha = Mathf.Lerp(1f, 0.3f, decayProgress);
                float radius = GameConfig.CarrionRadius * sizeScale;

                Gizmos.color = new Color(0.35f, 0.2f, 0.25f, alpha * 0.85f);
                Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), radius);

                Gizmos.color = new Color(0.5f, 0.25f, 0.2f, alpha * 0.6f);
                Gizmos.DrawSphere(new Vector3(pos.x + radius * 0.3f, pos.y + radius * 0.2f, 0), radius * 0.35f);
                Gizmos.DrawSphere(new Vector3(pos.x - radius * 0.35f, pos.y - radius * 0.15f, 0), radius * 0.3f);

                Gizmos.color = new Color(0.15f, 0.1f, 0.1f, alpha * 0.9f);
                Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), radius);

                if (decayProgress > 0.7f)
                {
                    Gizmos.color = new Color(0.6f, 0.55f, 0.4f, (decayProgress - 0.7f) / 0.3f * 0.7f);
                    for (int s = 0; s < 3; s++)
                    {
                        float angle = (s * 2.094f) + pos.x;
                        Vector3 p = new Vector3(
                            pos.x + Mathf.Cos(angle) * radius * 0.8f,
                            pos.y + Mathf.Sin(angle) * radius * 0.8f,
                            0);
                        Gizmos.DrawSphere(p, 0.25f);
                    }
                }
            }
        }

        private void DrawAnts(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<TeamComponent>(),
                ComponentType.ReadOnly<AntTypeComponent>(),
                ComponentType.ReadOnly<AntStateComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var positions = query.ToComponentDataArray<PositionComponent>(Unity.Collections.Allocator.Temp);
            using var teams = query.ToComponentDataArray<TeamComponent>(Unity.Collections.Allocator.Temp);
            using var types = query.ToComponentDataArray<AntTypeComponent>(Unity.Collections.Allocator.Temp);
            using var states = query.ToComponentDataArray<AntStateComponent>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                float2 pos = positions[i].Value;
                TeamType team = teams[i].Team;
                AntType type = types[i].Type;
                AntState state = states[i].CurrentState;

                float size = type == AntType.Worker ? 0.8f : 1.2f;

                Gizmos.color = team == TeamType.Red ? Color.red : Color.blue;

                if (type == AntType.Soldier)
                {
                    Gizmos.DrawCube(new Vector3(pos.x, pos.y, 0), new Vector3(size, size, 0.1f));
                }
                else
                {
                    Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), size * 0.5f);
                }

                if (state == AntState.Attacking)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), size * 0.8f);
                }
            }
        }
    }
}
