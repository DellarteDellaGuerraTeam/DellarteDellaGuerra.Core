using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeTactics.Mission;

/// <summary>
/// TacticBreachWalls pairs a lane with the melee formation whose median position is nearest to it,
/// then pins that pairing: AssignMeleeFormationsToLanes re-applies every lane's
/// GetLastAssignedFormation before distance-matching whatever is left over. Lane capacity is never
/// an input. BalanceAssaultLanes, which would even the formations out afterwards, transfers through
/// the gated Formation.TransferUnits, so it early-returns for good once a single attacker stands
/// inside the walls (IsConvenientForTransfer == InsideCastleUnitCountIncludingUnpositioned == 0).
///
/// Reinforcements arrive class-routed, so in a foot-heavy DADG army they nearly all land in the one
/// formation the initial pairing happened to weld to a ladder lane. Observed at Lancaster:
/// Infantry grew 58 -> 246 men on a two-ladder lane (capacity 16) while the open gate lane
/// (capacity 60) kept a Cavalry formation that withered 59 -> 2.
///
/// This corrects only that runaway case, and deliberately does nothing else. Reassignment is
/// expensive: AssignToLane calls ResetBehaviorWeights, which drops the formation's agents off any
/// siege machine they are crewing. Two things therefore have to hold or the correction destroys
/// more than it fixes:
///
///   - Size is measured with CountOfUnits, never CountOfUnitsWithoutDetachedOnes. The latter
///     excludes units detached onto a machine, so ranking by it couples the ranking to machine
///     usage that reassignment itself breaks: men grab the ram, the count drops, the formation is
///     demoted, the reassignment detaches them, the count recovers, it is promoted again. That
///     oscillated once a second and left the ram being pushed in one-second bursts.
///   - A swap needs a landslide, not an ordering. Formations sit within ~10 men of each other for
///     most of a siege, which is noise; only a formation several times larger than the one holding
///     the better lane is evidence of the runaway. Hence the ratio, the absolute margin, and the
///     cooldown.
///
/// Writing SetLastAssignedFormation means the vanilla preservation loop carries the corrected
/// pairing forward rather than reverting it next tick. Only AI.Side moves; no units are
/// transferred, so the IsConvenientForTransfer lock does not apply.
/// </summary>
public class CapacityWeightedBreachWallsTactic : TacticBreachWalls
{
    private const float RepairCooldown = 30f;
    private const float RunawaySizeRatio = 2f;
    private const int RunawaySizeMargin = 20;

    private readonly ILogger _logger;

    private float _nextRepairTime;

    public CapacityWeightedBreachWallsTactic(Team team, ILogger logger) : base(team)
    {
        _logger = logger;
    }

    public override void TickOccasionally()
    {
        base.TickOccasionally();
        RepairRunawayLane();
    }

    private void RepairRunawayLane()
    {
        if (TaleWorlds.MountAndBlade.Mission.Current.CurrentTime < _nextRepairTime) return;

        var lanes = TeamAISiegeComponent.SiegeLanes;
        if (lanes == null) return;

        // Only lanes vanilla has already committed a live formation to. Re-pairing within that set
        // means DetermineCurrentLanes (private) never has to be reimplemented, and no lane or
        // formation it rejected can be introduced here.
        var assigned = lanes
            .Where(lane => !lane.CalculateIsLaneUnusable())
            .Select(lane => (Lane: lane, Formation: lane.GetLastAssignedFormation(Team.TeamIndex)))
            .Where(pair => pair.Formation != null && pair.Formation.CountOfUnits > 0)
            .ToList();
        if (assigned.Count < 2) return;

        var crowded = assigned.OrderByDescending(pair => pair.Formation.CountOfUnits).First();
        var roomiest = assigned.OrderByDescending(pair => pair.Lane.CalculateLaneCapacity()).First();
        if (crowded.Lane == roomiest.Lane) return;
        if (roomiest.Lane.CalculateLaneCapacity() <= crowded.Lane.CalculateLaneCapacity()) return;

        var crowdedCount = crowded.Formation.CountOfUnits;
        var roomiestCount = roomiest.Formation.CountOfUnits;
        if (crowdedCount < roomiestCount * RunawaySizeRatio) return;
        if (crowdedCount < roomiestCount + RunawaySizeMargin) return;

        AssignToLane(crowded.Formation, roomiest.Lane);
        AssignToLane(roomiest.Formation, crowded.Lane);
        _nextRepairTime = TaleWorlds.MountAndBlade.Mission.Current.CurrentTime + RepairCooldown;

        _logger.Info($"[siege-ai] swapped runaway lane: " +
                     $"{crowded.Formation.FormationIndex}[n={crowdedCount}] " +
                     $"{crowded.Lane.LaneSide}[cap={crowded.Lane.CalculateLaneCapacity():F0}] -> " +
                     $"{roomiest.Lane.LaneSide}[cap={roomiest.Lane.CalculateLaneCapacity():F0}], " +
                     $"{roomiest.Formation.FormationIndex}[n={roomiestCount}] takes its place");
    }

    // Mirrors AssignMeleeFormationsToLanes, so a re-paired formation ends up in exactly the state
    // vanilla would have left it in had it made this pairing itself.
    private void AssignToLane(Formation formation, SiegeLane lane)
    {
        formation.AI.Side = lane.LaneSide;
        formation.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(formation);
        formation.AI.SetBehaviorWeight<BehaviorAssaultWalls>(1f);
        formation.AI.SetBehaviorWeight<BehaviorUseSiegeMachines>(1f);
        formation.AI.SetBehaviorWeight<BehaviorWaitForLadders>(1f);
        lane.SetLastAssignedFormation(Team.TeamIndex, formation);
    }
}
