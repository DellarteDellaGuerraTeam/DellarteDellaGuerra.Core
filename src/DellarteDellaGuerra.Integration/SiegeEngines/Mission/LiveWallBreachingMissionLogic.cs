using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission;

public class LiveWallBreachingMissionLogic : MissionLogic
{
    private const float FallbackWallSectionHitPoints = 4500f;
    private const string CannonballItemIdPart = "cannonball";

    private readonly ILogger _logger;
    private readonly Dictionary<WallSegment, LiveWallSection> _liveWallSections = new();

    public LiveWallBreachingMissionLogic(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LiveWallBreachingMissionLogic>();
    }

    public override void AfterStart()
    {
        base.AfterStart();

        if (!Mission.IsSiegeBattle || GameNetwork.IsClientOrReplay) return;

        TrackBreachableWalls();
    }

    public override void OnRegisterBlow(
        Agent attacker,
        Agent victim,
        GameEntity realHitEntity,
        Blow b,
        ref AttackCollisionData collisionData,
        in MissionWeapon attackerWeapon)
    {
        base.OnRegisterBlow(attacker, victim, realHitEntity, b, ref collisionData, in attackerWeapon);

        if (_liveWallSections.Count == 0 || realHitEntity == null || !IsCannonball(attackerWeapon)) return;

        var wallSegment = ResolveBreachableWall(realHitEntity);
        if (wallSegment == null || !_liveWallSections.TryGetValue(wallSegment, out var wallSection)) return;

        var damage = Math.Max(b.InflictedDamage, collisionData.InflictedDamage);
        if (damage <= 0) return;

        wallSection.ApplyDamage(damage);
        _logger.Debug(
            $"Cannonball hit breachable wall. WallSide={wallSegment.DefenseSide}, " +
            $"CampaignWallIndex={wallSection.CampaignWallIndex?.ToString() ?? "n/a"}, " +
            $"Weapon={attackerWeapon.Item?.StringId ?? "unknown"}, Damage={damage}, " +
            $"RemainingHitPoints={wallSection.HitPoints:0.##}/{wallSection.MaxHitPoints:0.##}.");

        if (wallSection.HitPoints > 0f) return;

        BreachWall(wallSegment, wallSection);
    }

    private void TrackBreachableWalls()
    {
        _liveWallSections.Clear();

        var breachableWalls = Mission.ActiveMissionObjects
            .FindAllWithType<WallSegment>()
            .Where(IsBreachableWallRoot)
            .ToList();

        if (breachableWalls.Count == 0) return;

        var settlement = PlayerSiege.BesiegedSettlement;
        var wallRatios = settlement?.SettlementWallSectionHitPointsRatioList;
        var sectionMaxHitPoints = settlement?.MaxHitPointsOfOneWallSection ?? FallbackWallSectionHitPoints;
        if (sectionMaxHitPoints <= 0f) sectionMaxHitPoints = FallbackWallSectionHitPoints;

        var campaignWallCount = wallRatios?.Count ?? Math.Min(2, breachableWalls.Count);
        for (var wallIndex = 0; wallIndex < campaignWallCount && breachableWalls.Count > 0; wallIndex++)
        {
            var wall = FindRightMostWall(breachableWalls);
            if (wall == null) break;

            var ratio = wallRatios != null && wallIndex < wallRatios.Count ? wallRatios[wallIndex] : 1f;
            var hitPoints = MathF.Max(0f, ratio * sectionMaxHitPoints);
            _liveWallSections[wall] = new LiveWallSection(wallIndex, hitPoints, sectionMaxHitPoints);
            breachableWalls.Remove(wall);
        }

        foreach (var wall in breachableWalls.Where(wall => !wall.IsBreachedWall))
        {
            _liveWallSections[wall] = new LiveWallSection(null, sectionMaxHitPoints, sectionMaxHitPoints);
        }

        _logger.Debug(
            $"Tracking {_liveWallSections.Count} live breachable wall segment(s). " +
            $"SectionMaxHitPoints={sectionMaxHitPoints:0.##}, Settlement={settlement?.StringId ?? "n/a"}.");
    }

    private void BreachWall(WallSegment wallSegment, LiveWallSection wallSection)
    {
        if (wallSegment.IsBreachedWall) return;

        wallSegment.OnChooseUsedWallSegment(isBroken: true);
        UpdateCampaignWallState(wallSection.CampaignWallIndex);
        var disabledLadderCount = DisableLaddersTargetingBreachedWall(wallSegment);
        RefreshSiegeLanes();
        ReapplyAttackerSiegeAi();

        _logger.Info(
            $"Breached live siege wall. WallSide={wallSegment.DefenseSide}, " +
            $"CampaignWallIndex={wallSection.CampaignWallIndex?.ToString() ?? "n/a"}, " +
            $"DisabledLadders={disabledLadderCount}.");
    }

    private static void UpdateCampaignWallState(int? campaignWallIndex)
    {
        if (!campaignWallIndex.HasValue) return;

        Settlement? settlement = PlayerSiege.BesiegedSettlement;
        if (settlement == null || campaignWallIndex.Value >= settlement.SettlementWallSectionHitPointsRatioList.Count) return;

        settlement.SetWallSectionHitPointsRatioAtIndex(campaignWallIndex.Value, 0f);
        settlement.Party.SetVisualAsDirty();
    }

    private static void RefreshSiegeLanes()
    {
        if (TeamAISiegeComponent.SiegeLanes == null) return;

        foreach (var siegeLane in TeamAISiegeComponent.SiegeLanes)
        {
            siegeLane.RefreshLane();
            siegeLane.DetermineLaneState();
            siegeLane.DetermineOrigins();
        }
    }

    private int DisableLaddersTargetingBreachedWall(WallSegment breachedWall)
    {
        var affectedLadders = Mission.ActiveMissionObjects
            .FindAllWithType<SiegeLadder>()
            .Where(ladder => !ladder.IsDisabled && IsLadderTargetingWall(ladder, breachedWall))
            .ToList();

        foreach (var ladder in affectedLadders)
        {
            StopFormationsUsingMachine(ladder);
            ladder.Disable();
            ladder.SetAbilityOfFaces(false);
            ladder.SetDisabledSynched();
        }

        return affectedLadders.Count;
    }

    private static bool IsLadderTargetingWall(SiegeLadder ladder, WallSegment breachedWall)
    {
        var targetWallEntity = ladder.TargetCastlePosition?.GameEntity;
        return targetWallEntity != null && ResolveBreachableWall(targetWallEntity) == breachedWall;
    }

    private void StopFormationsUsingMachine(UsableMachine machine)
    {
        foreach (var team in GetAttackerTeams())
        {
            foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
            {
                if (formation.Detachments.Contains(machine))
                {
                    formation.StopUsingMachine(machine);
                }
            }
        }
    }

    private IEnumerable<Team> GetAttackerTeams()
    {
        if (Mission.AttackerTeam != null) yield return Mission.AttackerTeam;
        if (Mission.AttackerAllyTeam != null) yield return Mission.AttackerAllyTeam;
    }

    private void ReapplyAttackerSiegeAi()
    {
        foreach (var team in GetAttackerTeams())
        {
            team.ResetTactic();
            team.QuerySystem.Expire();
        }
    }

    private static bool IsCannonball(in MissionWeapon weapon)
    {
        var itemId = weapon.Item?.StringId;
        return weapon.CurrentUsageItem?.WeaponClass == WeaponClass.Boulder &&
               itemId != null &&
               itemId.IndexOf(CannonballItemIdPart, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static WallSegment? ResolveBreachableWall(GameEntity hitEntity)
    {
        for (var entity = hitEntity; entity != null; entity = entity.Parent)
        {
            var wallSegment = entity.GetFirstScriptOfType<WallSegment>();
            if (wallSegment != null && IsBreachableWallRoot(wallSegment)) return wallSegment;
        }

        return null;
    }

    private static bool IsBreachableWallRoot(WallSegment wallSegment) =>
        wallSegment.DefenseSide != FormationAI.BehaviorSide.BehaviorSideNotSet &&
        wallSegment.GameEntity.GetChildren().Any(child => child.HasTag("solid_child")) &&
        wallSegment.GameEntity.GetChildren().Any(child => child.HasTag("broken_child"));

    private WallSegment? FindRightMostWall(List<WallSegment> walls)
    {
        if (walls.Count == 0) return null;
        if (walls.Count == 1) return walls[0];

        var ram = Mission.ActiveMissionObjects.FindAllWithType<BatteringRam>().FirstOrDefault();
        if (ram == null) return walls[0];

        if (walls.Count == 2)
        {
            var cross = Vec3.CrossProduct(
                walls[0].GameEntity.GlobalPosition - ram.GameEntity.GlobalPosition,
                walls[1].GameEntity.GlobalPosition - ram.GameEntity.GlobalPosition);
            return cross.z < 0f ? walls[1] : walls[0];
        }

        return walls
            .OrderByDescending(wall => wall.GameEntity.GlobalPosition.DistanceSquared(ram.GameEntity.GlobalPosition))
            .First();
    }

    private sealed class LiveWallSection
    {
        public int? CampaignWallIndex { get; }
        public float HitPoints { get; private set; }
        public float MaxHitPoints { get; }

        public LiveWallSection(int? campaignWallIndex, float hitPoints, float maxHitPoints)
        {
            CampaignWallIndex = campaignWallIndex;
            HitPoints = hitPoints;
            MaxHitPoints = maxHitPoints;
        }

        public void ApplyDamage(float damage)
        {
            HitPoints = MathF.Max(0f, HitPoints - damage);
        }
    }
}
