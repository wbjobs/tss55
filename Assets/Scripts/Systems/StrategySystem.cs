using AntWar.Components;
using AntWar.Utils;
using Unity.Entities;

namespace AntWar.Systems
{
    public partial struct StrategySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StrategyComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public static void SetGatherStrategy(EntityManager entityManager, TeamType team, StrategyType strategy,
            Unity.Mathematics.float2 target, float radius)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<StrategyComponent>(),
                ComponentType.ReadOnly<TeamComponent>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var teams = query.ToComponentDataArray<TeamComponent>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i].Team == team)
                {
                    var strategyComp = entityManager.GetComponentData<StrategyComponent>(entities[i]);
                    strategyComp.Strategy = strategy;
                    strategyComp.StrategyTarget = target;
                    strategyComp.StrategyRadius = radius;
                    entityManager.SetComponentData(entities[i], strategyComp);
                }
            }

            BattleLogger.LogStrategy(team == TeamType.Red ? "红方" : "蓝方", GetStrategyName(strategy));
        }

        public static string GetStrategyName(StrategyType strategy)
        {
            switch (strategy)
            {
                case StrategyType.GatherArea: return "区域采集";
                case StrategyType.Defend: return "防守";
                case StrategyType.Attack: return "进攻";
                case StrategyType.Retreat: return "撤退";
                default: return "无";
            }
        }
    }
}
