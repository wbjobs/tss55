using Unity.Entities;

namespace AntWar.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AntStrategySystemGroup))]
    public partial class AntLogicSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(AntLogicSystemGroup))]
    public partial class AntStrategySystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(AntLogicSystemGroup))]
    [UpdateBefore(typeof(WorkerAntSystem))]
    [UpdateBefore(typeof(SoldierAntSystem))]
    public partial class AntMovementSystemGroup : ComponentSystemGroup { }
}
