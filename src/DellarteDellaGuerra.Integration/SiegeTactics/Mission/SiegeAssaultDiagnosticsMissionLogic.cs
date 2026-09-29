using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeTactics.Mission;

/// <summary>
/// Temporary diagnostic for the attacker AI ignoring an open gate and committing to ladders.
///
/// TacticBreachWalls wants one melee formation per siege lane, but only creates them through
/// SplitFormationClassIntoGivenNumber, whose splitting branch is gated on
/// `num5 > count3 && count2 > 0` where count3 = AI-owned infantry formations and
/// count2 = those that are also IsConvenientForTransfer. For a siege attacker
/// IsConvenientForTransfer is `QuerySystem.InsideCastleUnitCountIncludingUnpositioned == 0`,
/// so the moment any attacker stands inside the walls count2 drops to 0 and no further split
/// can happen at any formation size. BehaviorAssaultWalls then only routes through the gate
/// when its formation's AI.Side equals OuterGate.DefenseSide, so a formation parked on a
/// ladder lane can never reach the MoveToGate/AttackEntity states.
///
/// This logs the inputs to that decision on change (plus a heartbeat) so a full assault can be
/// replayed to tell "never split" apart from "split, then merged back by
/// MergeFormationsIfLanesBecameUnavailable". Remove once the cause is settled.
/// </summary>
public class SiegeAssaultDiagnosticsMissionLogic : MissionLogic
{
    private const float SampleInterval = 0.5f;
    private const float HeartbeatInterval = 10f;

    private readonly ILogger _logger;

    private float _nextSampleTime;
    private float _lastHeartbeatTime;
    private string? _lastSnapshot;

    public SiegeAssaultDiagnosticsMissionLogic(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SiegeAssaultDiagnosticsMissionLogic>();
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        var now = Mission.CurrentTime;
        if (now < _nextSampleTime) return;
        _nextSampleTime = now + SampleInterval;

        if (Mission.MissionTeamAIType != TaleWorlds.MountAndBlade.Mission.MissionTeamAITypeEnum.Siege) return;
        if (Mission.AttackerTeam?.TeamAI is not TeamAISiegeComponent siegeAi) return;

        var snapshot = BuildSnapshot(Mission.AttackerTeam, siegeAi);
        var isHeartbeat = now - _lastHeartbeatTime >= HeartbeatInterval;
        if (snapshot == _lastSnapshot && !isHeartbeat) return;

        _lastSnapshot = snapshot;
        _lastHeartbeatTime = now;
        _logger.Debug($"[siege-ai t={now:F1}] {snapshot}");
    }

    private static string BuildSnapshot(Team attackers, TeamAISiegeComponent siegeAi)
    {
        var lanes = TeamAISiegeComponent.SiegeLanes ?? new List<SiegeLane>();
        var breachLanes = lanes.Where(l => l.IsBreach).ToList();
        var usableLanes = lanes.Where(l => !l.CalculateIsLaneUnusable()).ToList();

        // Approximates TacticBreachWalls.DetermineCurrentLanes (private): >=2 breaches wins,
        // otherwise the usable lanes, otherwise the gate lanes.
        var targetLanes = breachLanes.Count >= 2
            ? breachLanes
            : usableLanes.Count > 0
                ? usableLanes
                : lanes.Where(l => l.HasGate).ToList();

        var formations = attackers.FormationsIncludingSpecialAndEmpty
            .Where(f => f.CountOfUnits > 0)
            .ToList();

        var infantry = formations.Where(f => f.QuerySystem.IsInfantryFormation).ToList();
        var aiOwnedInfantry = infantry.Where(f => f.IsAIOwned).ToList();
        var convenientInfantry = aiOwnedInfantry.Where(f => f.IsConvenientForTransfer).ToList();

        // The live gate inside SplitFormationClassIntoGivenNumber.
        var canSplitInfantry = targetLanes.Count > aiOwnedInfantry.Count && convenientInfantry.Count > 0;

        var laneText = string.Join(" ; ", lanes.Select(l =>
            $"{l.LaneSide}[state={l.LaneState} open={l.IsOpen} breach={l.IsBreach} gate={l.HasGate} " +
            $"cap={l.CalculateLaneCapacity():F0} unusable={l.CalculateIsLaneUnusable()} " +
            $"weapons={string.Join(",", l.PrimarySiegeWeapons.Select(w => w.GetType().Name + (w.HasCompletedAction() ? "!" : "")))}]"));

        var formationText = string.Join(" ; ", formations.Select(f =>
            $"{f.FormationIndex}[n={f.CountOfUnits} side={f.AI.Side} " +
            $"behavior={f.AI.ActiveBehavior?.GetType().Name ?? "null"} " +
            $"aiOwned={f.IsAIOwned} aiCtrl={f.IsAIControlled} splittable={f.IsSplittableByAI} " +
            $"convenient={f.IsConvenientForTransfer} inside={f.QuerySystem.InsideCastleUnitCountIncludingUnpositioned} " +
            $"gateLane={siegeAi.OuterGate != null && siegeAi.OuterGate.DefenseSide == f.AI.Side}]"));

        return $"lanes={lanes.Count} usable={usableLanes.Count} breach={breachLanes.Count} target={targetLanes.Count} " +
               $"| split: aiOwnedInf={aiOwnedInfantry.Count} convenientInf={convenientInfantry.Count} canSplit={canSplitInfantry} " +
               $"| outerGate={GateText(siegeAi.OuterGate)} innerGate={GateText(siegeAi.InnerGate)} " +
               $"insideAttackers={TeamAISiegeComponent.QuerySystem?.InsideAttackerCount} " +
               $"| LANES {laneText} | FORMATIONS {formationText}";
    }

    private static string GateText(CastleGate? gate)
    {
        return gate == null
            ? "null"
            : $"[side={gate.DefenseSide} state={gate.State} open={gate.IsGateOpen} destroyed={gate.IsDestroyed}]";
    }
}
