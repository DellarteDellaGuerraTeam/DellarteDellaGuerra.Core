using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Titles.Spi;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    /**
     * <summary>
     *  Owns the lifecycle of the feudal title state: seeds it on new campaigns, persists it
     *  through saves as flat string lists, reacts to settlement ownership changes by
     *  reassigning titles and to clan leader deaths by generating inheritance claims for
     *  passed-over heirs.
     * </summary>
     */
    public class FeudalTitleCampaignBehavior : CampaignBehaviorBase
    {
        private readonly IAssignTitleUseCase _assignTitleUseCase;
        private readonly IGenerateInheritanceClaimsUseCase _generateInheritanceClaimsUseCase;
        private readonly IFeudalStateStore _stateStore;
        private readonly Func<IReadOnlyList<Title>> _initialTitlesProvider;

        private List<string> _serialisedTitles = new();
        private List<string> _serialisedClaims = new();
        private List<string> _serialisedTensions = new();

        public FeudalTitleCampaignBehavior(
            IAssignTitleUseCase assignTitleUseCase,
            IGenerateInheritanceClaimsUseCase generateInheritanceClaimsUseCase,
            IFeudalStateStore stateStore,
            Func<IReadOnlyList<Title>> initialTitlesProvider)
        {
            _assignTitleUseCase = assignTitleUseCase;
            _generateInheritanceClaimsUseCase = generateInheritanceClaimsUseCase;
            _stateStore = stateStore;
            _initialTitlesProvider = initialTitlesProvider;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                _serialisedTitles = TitleStateSerialiser.SerialiseTitles(_stateStore.SnapshotTitles());
                _serialisedClaims = TitleStateSerialiser.SerialiseClaims(_stateStore.SnapshotClaims());
                _serialisedTensions = TitleStateSerialiser.SerialiseTensions(_stateStore.SnapshotTensions());
            }

            dataStore.SyncData("DadgFeudalTitles", ref _serialisedTitles);
            dataStore.SyncData("DadgFeudalClaims", ref _serialisedClaims);
            dataStore.SyncData("DadgFeudalTensions", ref _serialisedTensions);

            _serialisedTitles ??= new List<string>();
            _serialisedClaims ??= new List<string>();
            _serialisedTensions ??= new List<string>();
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            if (_stateStore.SnapshotTitles().Count > 0) return;

            _stateStore.InitialiseTitles(_initialTitlesProvider());
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            // The save did not contain feudal state (mod added to an existing campaign):
            // seed the de jure layout instead of restoring.
            if (_serialisedTitles.Count == 0)
            {
                if (_stateStore.SnapshotTitles().Count == 0)
                {
                    _stateStore.InitialiseTitles(_initialTitlesProvider());
                }

                return;
            }

            _stateStore.InitialiseTitles(TitleStateSerialiser.DeserialiseTitles(_serialisedTitles));
            _stateStore.InitialiseClaims(TitleStateSerialiser.DeserialiseClaims(_serialisedClaims));
            _stateStore.InitialiseTensions(TitleStateSerialiser.DeserialiseTensions(_serialisedTensions));
        }

        private void OnSettlementOwnerChanged(
            TaleWorlds.CampaignSystem.Settlements.Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            _assignTitleUseCase.Execute(
                settlement.StringId,
                newOwner?.Clan?.StringId,
                ToSeatTransferKind(detail),
                (float)CampaignTime.Now.ToDays);
        }

        // Conquest leaves the dignity with the de jure holder; a king's decision (the claimant
        // election outcome) or a consensual conveyance moves it; the rest are bookkeeping.
        private static SeatTransferKind ToSeatTransferKind(
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            return detail switch
            {
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege => SeatTransferKind.Conquest,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion => SeatTransferKind.Conquest,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByKingDecision => SeatTransferKind.Grant,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByGift => SeatTransferKind.Grant,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter => SeatTransferKind.Grant,
                _ => SeatTransferKind.Administrative
            };
        }

        // A peace treaty cedes occupied titles: when the title's kingdom makes peace with the
        // faction holding the seat, the occupation is regularised as a grant (uti possidetis).
        private void OnMakePeace(
            IFaction side1Faction,
            IFaction side2Faction,
            MakePeaceAction.MakePeaceDetail detail)
        {
            foreach (var title in _stateStore.SnapshotTitles().Where(t => t.IsContested).ToList())
            {
                var settlement = TaleWorlds.CampaignSystem.Settlements.Settlement.Find(title.SeatSettlementId);
                var ownerFaction = settlement?.OwnerClan?.MapFaction;
                var titleKingdom = FeudalTitleKingdoms.GetTitleKingdom(title.Id);
                if (ownerFaction is null || titleKingdom is null || ownerFaction == titleKingdom) continue;

                bool peaceCoversTitle =
                    (side1Faction == titleKingdom && side2Faction == ownerFaction)
                    || (side2Faction == titleKingdom && side1Faction == ownerFaction);
                if (!peaceCoversTitle) continue;

                _assignTitleUseCase.Execute(
                    title.SeatSettlementId,
                    settlement!.OwnerClan.StringId,
                    SeatTransferKind.Grant,
                    (float)CampaignTime.Now.ToDays);
            }
        }

        private void OnHeroKilled(
            Hero victim,
            Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail,
            bool showNotification)
        {
            if (victim?.Clan is null || victim.Clan.Leader != victim) return;

            var passedOverHeirClanIds = CollectPassedOverHeirClanIds(victim);
            if (passedOverHeirClanIds.Count == 0) return;

            _generateInheritanceClaimsUseCase.Execute(victim.Clan.StringId, passedOverHeirClanIds);
        }

        private static IReadOnlyList<string> CollectPassedOverHeirClanIds(Hero victim)
        {
            var relatives = new List<Hero>();
            relatives.AddRange(victim.Children);
            relatives.AddRange(victim.Siblings);
            if (victim.Spouse != null) relatives.Add(victim.Spouse);

            return relatives
                .Where(relative => relative.IsAlive
                                   && relative.Clan != null
                                   && relative.Clan != victim.Clan
                                   && !relative.Clan.IsEliminated)
                .Select(relative => relative.Clan.StringId)
                .Distinct()
                .ToList();
        }
    }
}
