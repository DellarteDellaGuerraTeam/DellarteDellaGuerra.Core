using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Church.Pilgrimage;
using DellarteDellaGuerra.Domain.Church.Port;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    /**
     * <summary>
     * Spawns pilgrim bands that walk from the church villages to the shrine and back.
     * Protecting them from bandits earns relation with their home clergy; attacking them
     * is sacrilege. Without a shrine configured the behavior stays dormant.
     * </summary>
     */
    public class PilgrimageCampaignBehavior : CampaignBehaviorBase
    {
        private const float SpawnRadius = 1f;
        private const float ProtectionRadius = 5f;

        private readonly ChurchSettlements _churchSettlements;
        private readonly ChurchSacrilege _churchSacrilege;
        private readonly IChurchSettingsProvider _churchSettingsProvider;
        private readonly ILogger _logger;

        private Dictionary<MobileParty, Settlement> _pilgrimHomes = new();
        private Settlement _shrine;

        public PilgrimageCampaignBehavior(
            ChurchSettlements churchSettlements,
            ChurchSacrilege churchSacrilege,
            IChurchSettingsProvider churchSettingsProvider,
            ILoggerFactory loggerFactory)
        {
            _churchSettlements = churchSettlements;
            _churchSacrilege = churchSacrilege;
            _churchSettingsProvider = churchSettingsProvider;
            _logger = loggerFactory.CreateLogger<PilgrimageCampaignBehavior>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, SpawnPilgrimBands);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, TickPilgrimParties);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_dadgChurchPilgrimHomes", ref _pilgrimHomes);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _shrine = Settlement.All.FirstOrDefault(settlement => _churchSettlements.IsShrine(settlement));
            if (_shrine == null)
                _logger.Warn("No church settlement is marked as a shrine: pilgrimage stays dormant");

            AddDialogs(starter);
        }

        private void AddDialogs(CampaignGameStarter starter)
        {
            starter.AddDialogLine(
                "dadg_pilgrim_greeting",
                "start",
                "dadg_pilgrim_talk",
                "{=tR8vNc5Q}God keep you, my {?PLAYER.GENDER}lady{?}lord{\\?}. We are pilgrims of " +
                "{ABBEY_NAME}, bound for the holy shrine at {SHRINE_NAME} to pray for our village " +
                "and our dead.",
                IsConversationWithPilgrims,
                null,
                200);
            starter.AddPlayerLine(
                "dadg_pilgrim_farewell",
                "dadg_pilgrim_talk",
                "close_window",
                "{=bK4wHs7M}Go in peace, pilgrims.",
                null,
                null);
        }

        private bool IsConversationWithPilgrims()
        {
            var party = MobileParty.ConversationParty;
            if (party == null || _shrine == null || !_pilgrimHomes.TryGetValue(party, out var home))
                return false;

            MBTextManager.SetTextVariable("ABBEY_NAME", home.Name);
            MBTextManager.SetTextVariable("SHRINE_NAME", _shrine.Name);
            return true;
        }

        private void SpawnPilgrimBands()
        {
            if (_shrine == null) return;

            var maxPilgrimParties = _churchSettingsProvider.GetSettings().MaxPilgrimParties;
            foreach (var settlement in Settlement.All)
            {
                if (!_churchSettlements.IsChurchSettlement(settlement) ||
                    _churchSettlements.IsShrine(settlement)) continue;
                if (settlement.Culture?.VillagerPartyTemplate == null) continue;

                var abbot = settlement.Notables
                    .FirstOrDefault(notable => notable.IsPreacher && notable.IsAlive);
                if (abbot == null) continue;

                if (!PilgrimagePolicy.ShouldSpawn(
                        _pilgrimHomes.Count, maxPilgrimParties, MBRandom.RandomFloat)) continue;

                SpawnPilgrimBand(settlement, abbot);
            }
        }

        private void SpawnPilgrimBand(Settlement home, Hero abbot)
        {
            var name = new TextObject("{=gW6pZj3T}Pilgrims of {SETTLEMENT}");
            name.SetTextVariable("SETTLEMENT", home.Name);

            var party = CustomPartyComponent.CreateCustomPartyWithPartyTemplate(
                home.GatePosition,
                SpawnRadius,
                home,
                name,
                null,
                home.Culture.VillagerPartyTemplate,
                abbot,
                avoidHostileActions: true);
            party.Aggressiveness = 0f;
            MoveToSettlement(party, _shrine);
            _pilgrimHomes[party] = home;
        }

        // Villager travel-loop blueprint: re-issue the move order whenever the AI drifted or the
        // target became invalid, send arrivals at the shrine back home, and despawn bands that
        // made it home or can no longer travel.
        private void TickPilgrimParties()
        {
            foreach (var pilgrimage in _pilgrimHomes.ToList())
            {
                var party = pilgrimage.Key;
                var home = pilgrimage.Value;

                if (party == null || !party.IsActive)
                {
                    _pilgrimHomes.Remove(party);
                    continue;
                }

                if (party.MapEvent != null) continue;

                if (party.MemberRoster.TotalHealthyCount == 0 || _shrine == null || home == null ||
                    !_churchSettlements.IsChurchSettlement(home))
                {
                    Despawn(party);
                    continue;
                }

                if (party.CurrentSettlement == _shrine)
                {
                    LeaveSettlementAction.ApplyForParty(party);
                    MoveToSettlement(party, home);
                }
                else if (party.CurrentSettlement == home && party.TargetSettlement == home)
                {
                    Despawn(party);
                }
                else if (party.CurrentSettlement != null)
                {
                    LeaveSettlementAction.ApplyForParty(party);
                    MoveToSettlement(party, party.TargetSettlement == _shrine ? _shrine : home);
                }
                else
                {
                    var target = party.TargetSettlement == _shrine && IsReachable(party, _shrine)
                        ? _shrine
                        : home;
                    if (party.DefaultBehavior != AiBehavior.GoToSettlement || party.TargetSettlement != target)
                        MoveToSettlement(party, target);
                }
            }
        }

        // Villager pattern in 1.4.7 (War Sails): resolve the best land/naval route, then issue the
        // visit-settlement AI action.
        private static void MoveToSettlement(MobileParty party, Settlement settlement)
        {
            AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
                party, settlement, isTargetingPort: false, out var navigationType, out _, out var isFromPort);
            SetPartyAiAction.GetActionForVisitingSettlement(
                party, settlement, navigationType, isFromPort, isTargetingPort: false);
        }

        private static bool IsReachable(MobileParty party, Settlement target) =>
            !target.IsUnderSiege &&
            !FactionManager.IsAtWarAgainstFaction(party.MapFaction, target.MapFaction);

        private void Despawn(MobileParty party)
        {
            _pilgrimHomes.Remove(party);
            DestroyPartyAction.Apply(null, party);
        }

        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
        {
            _pilgrimHomes.Remove(mobileParty);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (_pilgrimHomes.Count == 0) return;

            ApplySacrilegeIfPlayerAttackedPilgrims(mapEvent);
            RewardPilgrimProtection(mapEvent);
        }

        private void ApplySacrilegeIfPlayerAttackedPilgrims(MapEvent mapEvent)
        {
            if (mapEvent.AttackerSide.Parties.All(party => party.Party != PartyBase.MainParty)) return;

            foreach (var pilgrimage in _pilgrimHomes.ToList())
            {
                if (mapEvent.DefenderSide.Parties.All(party => party.Party != pilgrimage.Key.Party)) continue;

                _churchSacrilege.Apply(Hero.MainHero, pilgrimage.Value);
                return;
            }
        }

        private void RewardPilgrimProtection(MapEvent mapEvent)
        {
            if (!mapEvent.HasWinner) return;

            BattleSideEnum playerSide;
            if (mapEvent.AttackerSide.Parties.Any(party => party.Party == PartyBase.MainParty))
                playerSide = BattleSideEnum.Attacker;
            else if (mapEvent.DefenderSide.Parties.Any(party => party.Party == PartyBase.MainParty))
                playerSide = BattleSideEnum.Defender;
            else return;

            if (mapEvent.WinningSide != playerSide) return;

            var enemySide = playerSide == BattleSideEnum.Attacker
                ? mapEvent.DefenderSide
                : mapEvent.AttackerSide;
            if (!enemySide.Parties.Any(party => party.Party.MobileParty?.IsBandit == true)) return;

            var protectionRelation = _churchSettingsProvider.GetSettings().PilgrimProtectionRelation;
            foreach (var pilgrimage in _pilgrimHomes)
            {
                var party = pilgrimage.Key;
                if (!party.IsActive || mapEvent.InvolvedParties.Contains(party.Party)) continue;
                if (party.Position.Distance(mapEvent.Position) > ProtectionRadius) continue;

                var abbot = pilgrimage.Value.Notables
                    .FirstOrDefault(notable => notable.IsPreacher && notable.IsAlive);
                if (abbot == null) continue;

                ChangeRelationAction.ApplyPlayerRelation(abbot, protectionRelation);
                var message = new TextObject(
                    "{=eF9qXb2S}The pilgrims of {SETTLEMENT} bless you for your protection.");
                message.SetTextVariable("SETTLEMENT", pilgrimage.Value.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
        }
    }
}
