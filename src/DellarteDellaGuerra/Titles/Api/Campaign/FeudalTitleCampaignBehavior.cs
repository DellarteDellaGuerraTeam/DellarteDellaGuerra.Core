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
     *  reassigning titles, and rederives the claims that descend by blood whenever a death
     *  or a change of holder moves the bloodlines they are computed from.
     * </summary>
     */
    public class FeudalTitleCampaignBehavior : CampaignBehaviorBase
    {
        private readonly IAssignTitleUseCase _assignTitleUseCase;
        private readonly IGenerateBloodClaimsUseCase _generateBloodClaimsUseCase;
        private readonly IFeudalStateStore _stateStore;
        private readonly Func<IReadOnlyList<Title>> _initialTitlesProvider;

        private List<string> _serialisedTitles = new();
        private List<string> _serialisedClaims = new();

        public FeudalTitleCampaignBehavior(
            IAssignTitleUseCase assignTitleUseCase,
            IGenerateBloodClaimsUseCase generateBloodClaimsUseCase,
            IFeudalStateStore stateStore,
            Func<IReadOnlyList<Title>> initialTitlesProvider)
        {
            _assignTitleUseCase = assignTitleUseCase;
            _generateBloodClaimsUseCase = generateBloodClaimsUseCase;
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
            }

            dataStore.SyncData("DadgFeudalTitles", ref _serialisedTitles);
            dataStore.SyncData("DadgFeudalClaims", ref _serialisedClaims);

            _serialisedTitles ??= new List<string>();
            _serialisedClaims ??= new List<string>();
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            if (_stateStore.SnapshotTitles().Count > 0) return;

            _stateStore.InitialiseTitles(_initialTitlesProvider());
            _generateBloodClaimsUseCase.Execute();
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

                _generateBloodClaimsUseCase.Execute();
                return;
            }

            _stateStore.InitialiseTitles(TitleStateSerialiser.DeserialiseTitles(_serialisedTitles));
            _stateStore.InitialiseClaims(TitleStateSerialiser.DeserialiseClaims(_serialisedClaims));

            // Blood claims are a pure function of the bloodlines, so they are rebuilt rather
            // than trusted: a save written before this system existed carries none, and one
            // written by an older rule set carries claims the current rules would not grant.
            _generateBloodClaimsUseCase.Execute();
        }

        private void OnSettlementOwnerChanged(
            TaleWorlds.CampaignSystem.Settlements.Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            // ByKingDecision is a legal grant only when it originates from the title's own
            // kingdom's election. A foreign king reassigning a freshly conquered seat to one
            // of his clans is still occupation — the dignity does not follow the engine event.
            var title = FeudalServices.Titles?.GetTitleBySeat(settlement.StringId);
            var titleKingdom = title is not null ? FeudalTitleKingdoms.GetTitleKingdom(title.Id) : null;
            bool newOwnerInTitleKingdom = newOwner?.Clan?.Kingdom is not null
                                          && newOwner.Clan.Kingdom == titleKingdom;

            SeatTransferKind kind = detail switch
            {
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege => SeatTransferKind.Conquest,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion => SeatTransferKind.Conquest,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByKingDecision =>
                    newOwnerInTitleKingdom ? SeatTransferKind.Grant : SeatTransferKind.Conquest,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByGift => SeatTransferKind.Grant,
                ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter => SeatTransferKind.Grant,
                _ => SeatTransferKind.Administrative
            };

            _assignTitleUseCase.Execute(
                settlement.StringId,
                newOwner?.Clan?.StringId,
                kind,
                (float)CampaignTime.Now.ToDays);

            // Dispossession moves the holder every blood claim on this title descends from.
            _generateBloodClaimsUseCase.Execute();
        }

        // A peace treaty cedes occupied titles: when the title's kingdom makes peace with the
        // faction holding the seat, the occupation is regularised as a grant (uti possidetis).
        private void OnMakePeace(
            IFaction side1Faction,
            IFaction side2Faction,
            MakePeaceAction.MakePeaceDetail detail)
        {
            bool cededAnyTitle = false;
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
                cededAnyTitle = true;
            }

            if (cededAnyTitle) _generateBloodClaimsUseCase.Execute();
        }

        // Any death can change the claims: the dead hero's own claims lapse, and a clan
        // leader's death moves the anchor every claim on that clan's titles descends from.
        private void OnHeroKilled(
            Hero victim,
            Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail,
            bool showNotification)
        {
            _generateBloodClaimsUseCase.Execute();
        }
    }
}
