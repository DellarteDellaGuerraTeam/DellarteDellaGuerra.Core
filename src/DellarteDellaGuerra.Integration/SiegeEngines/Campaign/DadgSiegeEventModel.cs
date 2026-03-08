using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Integration.SiegeEngines;
using DellarteDellaGuerra.Integration.SiegeEngines.Port;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Campaign
{
    public class DadgSiegeEventModel : SiegeEventModel
    {
        private readonly ILogger _logger;
        private readonly SiegeEventModel _defaultSiegeEventModel;
        private readonly ICannonPrefabProvider _prefabProvider;
        private readonly ICannonAvailabilityProvider _availabilityProvider;

        public DadgSiegeEventModel(
            SiegeEventModel defaultSiegeEventModel,
            ICannonPrefabProvider prefabProvider,
            ICannonAvailabilityProvider availabilityProvider,
            ILoggerFactory loggerFactory)
        {
            _defaultSiegeEventModel = defaultSiegeEventModel;
            _prefabProvider = prefabProvider;
            _availabilityProvider = availabilityProvider;
            _logger = loggerFactory.CreateLogger<DadgSiegeEventModel>();
        }

        private static IEnumerable<SiegeEngineType> GetUnwantedSiegeEngines() =>
            new List<SiegeEngineType>
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

        public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType destroyedSiegeEngine) =>
            _defaultSiegeEventModel.GetSiegeEngineDestructionCasualties(siegeEvent, side, destroyedSiegeEngine);

        public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side) =>
            _defaultSiegeEventModel.GetCasualtyChance(siegeParty, siegeEvent, side);

        public override int GetColleteralDamageCasualties(SiegeEngineType attackerSiegeEngine, MobileParty party) =>
            _defaultSiegeEventModel.GetColleteralDamageCasualties(attackerSiegeEngine, party);

        public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide,
            SiegeBombardTargets target, Town town) =>
            _defaultSiegeEventModel.GetSiegeEngineHitChance(siegeEngineType, battleSide, target, town);

        public override string GetSiegeEngineMapPrefabName(SiegeEngineType siegeEngineType, int wallLevel,
            BattleSideEnum side)
        {
            var prefabName = _prefabProvider.GetCampaignMapPrefabName(siegeEngineType.StringId, wallLevel, side);
            return prefabName ?? _defaultSiegeEventModel.GetSiegeEngineMapPrefabName(siegeEngineType, wallLevel, side);
        }

        public override string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType siegeEngineType)
        {
            var projectilePrefab = _prefabProvider.GetCampaignMapProjectilePrefabName(siegeEngineType.StringId);
            return projectilePrefab ?? _defaultSiegeEventModel.GetSiegeEngineMapProjectilePrefabName(siegeEngineType);
        }

        public override string GetSiegeEngineMapReloadAnimationName(SiegeEngineType siegeEngineType,
            BattleSideEnum side)
        {
            var reloadPrefab = _prefabProvider.GetCampaignMapReloadAnimationName(siegeEngineType.StringId);
            return reloadPrefab ?? _defaultSiegeEventModel.GetSiegeEngineMapReloadAnimationName(siegeEngineType, side);
        }

        public override string GetSiegeEngineMapFireAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            var firePrefab = _prefabProvider.GetCampaignMapFireAnimationName(siegeEngineType.StringId);
            return firePrefab ?? _defaultSiegeEventModel.GetSiegeEngineMapFireAnimationName(siegeEngineType, side);
        }

        public override sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            var boneIndex = _prefabProvider.GetCampaignMapProjectileBoneIndex(siegeEngineType.StringId);
            return boneIndex >= 0
                ? (sbyte)boneIndex
                : _defaultSiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineType, side);
        }

        public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy) =>
            _defaultSiegeEventModel.GetSiegeStrategyScore(siege, side, strategy);

        public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent,
            ISiegeEventSide side) =>
            _defaultSiegeEventModel.GetConstructionProgressPerHour(type, siegeEvent, side);

        public override MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum side) =>
            _defaultSiegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, side);

        public override float GetAvailableManDayPower(ISiegeEventSide side) =>
            _defaultSiegeEventModel.GetAvailableManDayPower(side);

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party)
        {
            var baseEngines = _defaultSiegeEventModel.GetAvailableAttackerRangedSiegeEngines(party)
                .Where(e => !GetUnwantedSiegeEngines().Contains(e));
            var cannonEngines = _availabilityProvider.GetAvailableCannons(party, BattleSideEnum.Attacker);
            return baseEngines.Concat(cannonEngines);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party)
        {
            var baseEngines = _defaultSiegeEventModel.GetAvailableDefenderSiegeEngines(party)
                .Where(e => !GetUnwantedSiegeEngines().Contains(e));
            var cannonEngines = _availabilityProvider.GetAvailableCannons(party, BattleSideEnum.Defender);
            return baseEngines.Concat(cannonEngines);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party) =>
            _defaultSiegeEventModel.GetAvailableAttackerRamSiegeEngines(party);

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party) =>
            new List<SiegeEngineType>();

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
        {
            var defaultCannon = _availabilityProvider
                .GetAvailableCannons(null, BattleSideEnum.Defender)
                .FirstOrDefault();

            if (defaultCannon is null)
            {
                _logger.Warn("No pre-built defender alternative could be found to forbidden native siege engines");
                return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSettlement(settlement)
                    .Where(e => !GetUnwantedSiegeEngines().Contains(e));
            }

            return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSettlement(settlement)
                .Select(e => GetUnwantedSiegeEngines().Contains(e) ? defaultCannon : e);
        }

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp camp)
        {
            var defaultCannon = _availabilityProvider
                .GetAvailableCannons(camp.LeaderParty.Party, BattleSideEnum.Attacker)
                .FirstOrDefault();

            if (defaultCannon is null)
            {
                _logger.Warn("No pre-built attacker alternative could be found to forbidden native siege engines");
                return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSiegeCamp(camp)
                    .Where(e => !GetUnwantedSiegeEngines().Contains(e));
            }

            return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSiegeCamp(camp)
                .Select(e => GetUnwantedSiegeEngines().Contains(e) ? defaultCannon : e);
        }

        public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine,
            BattleSideEnum battleSide) =>
            _defaultSiegeEventModel.GetSiegeEngineHitPoints(siegeEvent, siegeEngine, battleSide);

        public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType siegeEngine) =>
            _defaultSiegeEventModel.GetRangedSiegeEngineReloadTime(siegeEvent, side, siegeEngine);

        public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide,
            SiegeEngineType siegeEngine, SiegeBombardTargets target) =>
            _defaultSiegeEventModel.GetSiegeEngineDamage(siegeEvent, battleSide, siegeEngine, target);

        public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush() =>
            _defaultSiegeEventModel.GetPriorityTroopsForSallyOutAmbush();
    }
}
