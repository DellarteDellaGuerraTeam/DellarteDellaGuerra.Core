using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Campaign
{
    public class DadgSiegeEventModel : SiegeEventModel
    {
        private readonly SiegeEventModel _defaultSiegeEventModel;
        private readonly CannonPrefabProvider _prefabProvider;
        private readonly CannonIconProvider _iconProvider;
        private readonly CannonAvailabilityProvider _availabilityProvider;

        public DadgSiegeEventModel(
            SiegeEventModel defaultSiegeEventModel,
            CannonPrefabProvider prefabProvider,
            CannonIconProvider iconProvider,
            CannonAvailabilityProvider availabilityProvider)
        {
            _defaultSiegeEventModel = defaultSiegeEventModel;
            _prefabProvider = prefabProvider;
            _iconProvider = iconProvider;
            _availabilityProvider = availabilityProvider;
        }

        public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType destroyedSiegeEngine)
        {
            return _defaultSiegeEventModel.GetSiegeEngineDestructionCasualties(siegeEvent, side, destroyedSiegeEngine);
        }

        public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return _defaultSiegeEventModel.GetCasualtyChance(siegeParty, siegeEvent, side);
        }

        public override int GetColleteralDamageCasualties(SiegeEngineType attackerSiegeEngine, MobileParty party)
        {
            return _defaultSiegeEventModel.GetColleteralDamageCasualties(attackerSiegeEngine, party);
        }

        public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide,
            SiegeBombardTargets target,
            Town town)
        {
            return _defaultSiegeEventModel.GetSiegeEngineHitChance(siegeEngineType, battleSide, target, town);
        }

        public override string GetSiegeEngineMapPrefabName(SiegeEngineType siegeEngineType, int wallLevel,
            BattleSideEnum side)
        {
            var prefabName = _prefabProvider.GetMapPrefabName(siegeEngineType.StringId, wallLevel, side);
            return prefabName ?? _defaultSiegeEventModel.GetSiegeEngineMapPrefabName(siegeEngineType, wallLevel, side);
        }

        public override string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType siegeEngineType)
        {
            var projectilePrefab = _prefabProvider.GetProjectilePrefabName(siegeEngineType.StringId);
            return projectilePrefab ?? _defaultSiegeEventModel.GetSiegeEngineMapProjectilePrefabName(siegeEngineType);
        }

        public override string GetSiegeEngineMapReloadAnimationName(SiegeEngineType siegeEngineType,
            BattleSideEnum side)
        {
            var reloadPrefab = _prefabProvider.GetReloadPrefabName(siegeEngineType.StringId);
            return reloadPrefab ?? _defaultSiegeEventModel.GetSiegeEngineMapReloadAnimationName(siegeEngineType, side);
        }

        public override string GetSiegeEngineMapFireAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            var firePrefab = _prefabProvider.GetFirePrefabName(siegeEngineType.StringId);
            return firePrefab ?? _defaultSiegeEventModel.GetSiegeEngineMapFireAnimationName(siegeEngineType, side);
        }

        public override sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            var boneIndex = _prefabProvider.GetProjectileBoneIndex(siegeEngineType.StringId);
            return boneIndex >= 0
                ? (sbyte)boneIndex
                : _defaultSiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineType, side);
        }

        public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy)
        {
            return _defaultSiegeEventModel.GetSiegeStrategyScore(siege, side, strategy);
        }

        public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent,
            ISiegeEventSide side)
        {
            return _defaultSiegeEventModel.GetConstructionProgressPerHour(type, siegeEvent, side);
        }

        public override MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return _defaultSiegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, side);
        }

        public override float GetAvailableManDayPower(ISiegeEventSide side)
        {
            return _defaultSiegeEventModel.GetAvailableManDayPower(side);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party)
        {
            var baseEngines = _defaultSiegeEventModel.GetAvailableAttackerRangedSiegeEngines(party);
            var cannonEngines = _availabilityProvider.GetAvailableCannonTypes(party, BattleSideEnum.Attacker);
            return baseEngines.ToList().Concat(cannonEngines);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party)
        {
            var baseEngines = _defaultSiegeEventModel.GetAvailableDefenderSiegeEngines(party);
            var cannonEngines = _availabilityProvider.GetAvailableCannonTypes(party, BattleSideEnum.Defender);
            return baseEngines.Concat(cannonEngines);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party)
        {
            return _defaultSiegeEventModel.GetAvailableAttackerRamSiegeEngines(party);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party)
        {
            return new List<SiegeEngineType>();
            // return _defaultSiegeEventModel.GetAvailableAttackerTowerSiegeEngines(party);
        }

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
        {
            return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSettlement(settlement);
        }

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp camp)
        {
            return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSiegeCamp(camp);
        }

        public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine,
            BattleSideEnum battleSide)
        {
            return _defaultSiegeEventModel.GetSiegeEngineHitPoints(siegeEvent, siegeEngine, battleSide);
        }

        public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType siegeEngine)
        {
            return _defaultSiegeEventModel.GetRangedSiegeEngineReloadTime(siegeEvent, side, siegeEngine);
        }

        public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide,
            SiegeEngineType siegeEngine,
            SiegeBombardTargets target)
        {
            return _defaultSiegeEventModel.GetSiegeEngineDamage(siegeEvent, battleSide, siegeEngine, target);
        }

        public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush()
        {
            return _defaultSiegeEventModel.GetPriorityTroopsForSallyOutAmbush();
        }
    }
}