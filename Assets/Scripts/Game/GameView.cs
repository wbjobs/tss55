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
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FoodTag>(),
                ComponentType.ReadOnly<PositionComponent>(),
                ComponentType.ReadOnly<FoodComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var positions = query.ToComponentDataArray<PositionComponent>(Unity.Collections.Allocator.Temp);
            using var foods = query.ToComponentDataArray<FoodComponent>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                float2 pos = positions[i].Value;
                float amount = foods[i].Amount;
                float maxAmount = foods[i].MaxAmount;

                float size = Mathf.Lerp(0.5f, 2f, amount / maxAmount);

                Gizmos.color = new Color(0.8f, 0.6f, 0.2f, 0.8f);
                Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), size);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), size);
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
