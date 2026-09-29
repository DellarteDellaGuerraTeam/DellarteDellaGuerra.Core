using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeTactics.Mission;

public class CapacityWeightedAssaultMissionLogic : MissionLogic
{
    private readonly ILoggerFactory _loggerFactory;

    public CapacityWeightedAssaultMissionLogic(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    // MissionCombatantsLogic.EarlyStart gives the attacker team a TacticBreachWalls option. Swapping
    // it for the subclass here, before any team AI tick has run, means the first MakeDecision picks
    // ours - no ResetTactic and no Harmony patch needed. RemoveTacticOption compares GetType()
    // exactly, so it drops the vanilla instance and leaves the subclass alone.
    public override void AfterStart()
    {
        base.AfterStart();

        var attackers = Mission.AttackerTeam;
        if (attackers?.TeamAI is not TeamAISiegeComponent) return;

        attackers.RemoveTacticOption(typeof(TacticBreachWalls));
        attackers.AddTacticOption(new CapacityWeightedBreachWallsTactic(
            attackers, _loggerFactory.CreateLogger<CapacityWeightedBreachWallsTactic>()));
    }
}
