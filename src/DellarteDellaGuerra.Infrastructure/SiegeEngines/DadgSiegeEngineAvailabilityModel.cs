using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.SiegeEngines;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class DadgSiegeEngineAvailabilityModel : SiegeEventModel
{
    private readonly ILogger _logger;
    private readonly SiegeEventModel _baseSiegeEventModel;
    private readonly GetDefaultSiegeEngine _defaultSiegeEngine;

    public DadgSiegeEngineAvailabilityModel(SiegeEventModel baseSiegeEventModel, ILoggerFactory loggerFactory,
        GetDefaultSiegeEngine defaultSiegeEngine)
    {
        _baseSiegeEventModel = baseSiegeEventModel
                               ?? throw new ArgumentNullException(nameof(baseSiegeEventModel));
        _defaultSiegeEngine = defaultSiegeEngine;
        _logger = loggerFactory.CreateLogger<DadgSiegeEngineAvailabilityModel>();
    }

    private IEnumerable<SiegeEngineType> GetUnwantedSiegeEngines()
    {
        return new List<SiegeEngineType>
        {
            DefaultSiegeEngineTypes.SiegeTower,
            DefaultSiegeEngineTypes.HeavySiegeTower,
            DefaultSiegeEngineTypes.Ballista,
            DefaultSiegeEngineTypes.FireBallista,
            DefaultSiegeEngineTypes.FireOnager,
            DefaultSiegeEngineTypes.Onager,
            DefaultSiegeEngineTypes.Catapult,
            DefaultSiegeEngineTypes.FireCatapult
        };
    }

    public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side,
        SiegeEngineType destroyedSiegeEngine)
    {
        return _baseSiegeEventModel.GetSiegeEngineDestructionCasualties(siegeEvent, side, destroyedSiegeEngine);
    }

    public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side)
    {
        return _baseSiegeEventModel.GetCasualtyChance(siegeParty, siegeEvent, side);
    }

    public override int GetColleteralDamageCasualties(SiegeEngineType attackerSiegeEngine, MobileParty party)
    {
        return _baseSiegeEventModel.GetColleteralDamageCasualties(attackerSiegeEngine, party);
    }

    public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide,
        SiegeBombardTargets target, Town town)
    {
        return _baseSiegeEventModel.GetSiegeEngineHitChance(siegeEngineType, battleSide, target, town);
    }

    public override string GetSiegeEngineMapPrefabName(SiegeEngineType siegeEngineType, int wallLevel,
        BattleSideEnum side)
    {
        return _baseSiegeEventModel.GetSiegeEngineMapPrefabName(siegeEngineType, wallLevel, side);
    }

    public override string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType siegeEngineType)
    {
        return _baseSiegeEventModel.GetSiegeEngineMapProjectilePrefabName(siegeEngineType);
    }

    public override string GetSiegeEngineMapReloadAnimationName(SiegeEngineType siegeEngineType,
        BattleSideEnum side)
    {
        return _baseSiegeEventModel.GetSiegeEngineMapReloadAnimationName(siegeEngineType, side);
    }

    public override string GetSiegeEngineMapFireAnimationName(SiegeEngineType siegeEngineType,
        BattleSideEnum side)
    {
        return _baseSiegeEventModel.GetSiegeEngineMapFireAnimationName(siegeEngineType, side);
    }

    public override sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType siegeEngineType,
        BattleSideEnum side)
    {
        return _baseSiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineType, side);
    }

    public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy)
    {
        return _baseSiegeEventModel.GetSiegeStrategyScore(siege, side, strategy);
    }

    public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent,
        ISiegeEventSide side)
    {
        return _baseSiegeEventModel.GetConstructionProgressPerHour(type, siegeEvent, side);
    }

    public override MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum side)
    {
        return _baseSiegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, side);
    }

    public override float GetAvailableManDayPower(ISiegeEventSide side)
    {
        return _baseSiegeEventModel.GetAvailableManDayPower(side);
    }

    public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party)
    {
        return _baseSiegeEventModel.GetAvailableAttackerRangedSiegeEngines(party)
            .Where(siegeEngineType => !GetUnwantedSiegeEngines().Contains(siegeEngineType));
    }

    public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party)
    {
        return _baseSiegeEventModel.GetAvailableDefenderSiegeEngines(party)
            .Where(siegeEngineType => !GetUnwantedSiegeEngines().Contains(siegeEngineType));
    }

    public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party)
    {
        return _baseSiegeEventModel.GetAvailableAttackerRamSiegeEngines(party);
    }

    public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party)
    {
        return _baseSiegeEventModel.GetAvailableAttackerTowerSiegeEngines(party)
            .Where(siegeEngineType => !GetUnwantedSiegeEngines().Contains(siegeEngineType));
    }

    public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
    {
        var defaultAllowedSiegeEngine =
            MBObjectManager.Instance.GetObject<SiegeEngineType>(_defaultSiegeEngine.GetSiegeEngine().Id);
        if (defaultAllowedSiegeEngine is null)
        {
            _logger.Warn("No pre-built defender alternative could be found to forbidden native siege engines");
            return _baseSiegeEventModel.GetPrebuiltSiegeEnginesOfSettlement(settlement).Where(siegeEngineType =>
                !GetUnwantedSiegeEngines().Contains(siegeEngineType));
        }

        return _baseSiegeEventModel.GetPrebuiltSiegeEnginesOfSettlement(settlement).Select(siegeEngineType =>
            GetUnwantedSiegeEngines().Contains(siegeEngineType) ? defaultAllowedSiegeEngine : siegeEngineType);
    }

    public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp camp)
    {
        var defaultAllowedSiegeEngine = GetAvailableAttackerRangedSiegeEngines(camp.LeaderParty.Party).FirstOrDefault();
        if (defaultAllowedSiegeEngine is null)
        {
            _logger.Warn("No pre-built attacker alternative could be found to forbidden native siege engines");
            return _baseSiegeEventModel.GetPrebuiltSiegeEnginesOfSiegeCamp(camp).Where(siegeEngineType =>
                !GetUnwantedSiegeEngines().Contains(siegeEngineType));
        }

        return _baseSiegeEventModel.GetPrebuiltSiegeEnginesOfSiegeCamp(camp).Select(siegeEngineType =>
            GetUnwantedSiegeEngines().Contains(siegeEngineType) ? defaultAllowedSiegeEngine : siegeEngineType);
    }

    public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine,
        BattleSideEnum battleSide)
    {
        return _baseSiegeEventModel.GetSiegeEngineHitPoints(siegeEvent, siegeEngine, battleSide);
    }

    public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side,
        SiegeEngineType siegeEngine)
    {
        return _baseSiegeEventModel.GetRangedSiegeEngineReloadTime(siegeEvent, side, siegeEngine);
    }

    public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide,
        SiegeEngineType siegeEngine, SiegeBombardTargets target)
    {
        return _baseSiegeEventModel.GetSiegeEngineDamage(siegeEvent, battleSide, siegeEngine, target);
    }

    public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush()
    {
        return _baseSiegeEventModel.GetPriorityTroopsForSallyOutAmbush();
    }
}