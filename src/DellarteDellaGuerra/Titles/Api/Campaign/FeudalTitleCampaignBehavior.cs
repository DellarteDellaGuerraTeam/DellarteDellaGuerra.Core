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
        private readonly IExecuteSuccessionUseCase _executeSuccessionUseCase;
        private readonly IFeudalStateStore _stateStore;
        private readonly Func<IReadOnlyList<Title>> _initialTitlesProvider;
        private readonly Func<IReadOnlyList<Claim>> _initialClaimsProvider;

        private List<string> _serialisedTitles = new();
        private List<string> _serialisedClaims = new();
        private List<string> _serialisedReattachments = new();

        public FeudalTitleCampaignBehavior(
            IAssignTitleUseCase assignTitleUseCase,
            IGenerateBloodClaimsUseCase generateBloodClaimsUseCase,
            IExecuteSuccessionUseCase executeSuccessionUseCase,
            IFeudalStateStore stateStore,
            Func<IReadOnlyList<Title>> initialTitlesProvider,
            Func<IReadOnlyList<Claim>> initialClaimsProvider)
        {
            _assignTitleUseCase = assignTitleUseCase;
            _generateBloodClaimsUseCase = generateBloodClaimsUseCase;
            _executeSuccessionUseCase = executeSuccessionUseCase;
            _stateStore = stateStore;
            _initialTitlesProvider = initialTitlesProvider;
            _initialClaimsProvider = initialClaimsProvider;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                _serialisedTitles = TitleStateSerialiser.SerialiseTitles(_stateStore.SnapshotTitles());
                _serialisedClaims = TitleStateSerialiser.SerialiseClaims(_stateStore.SnapshotClaims());
                _serialisedReattachments = TitleStateSerialiser.SerialiseLinks(_stateStore.SnapshotReattachments());
            }

            dataStore.SyncData("DadgFeudalTitles", ref _serialisedTitles);
            dataStore.SyncData("DadgFeudalClaims", ref _serialisedClaims);
            dataStore.SyncData("DadgTitleReattachments", ref _serialisedReattachments);

            _serialisedTitles ??= new List<string>();
            _serialisedClaims ??= new List<string>();
            _serialisedReattachments ??= new List<string>();
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            // The structure and the registries outlive a campaign, so a new one starts from the
            // configured realms, holders and claims rather than those an earlier campaign in this
            // session left behind.
            _stateStore.InitialiseReattachments(new Dictionary<string, string?>());
            _stateStore.InitialiseTitles(_initialTitlesProvider());
            _stateStore.InitialiseClaims(_initialClaimsProvider());
            _generateBloodClaimsUseCase.Execute();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            // Empty in a save written before titles could change realm: the configured realms.
            _stateStore.InitialiseReattachments(TitleStateSerialiser.DeserialiseLinks(_serialisedReattachments));

            // The save did not contain feudal state (mod added to an existing campaign):
            // seed the de jure layout instead of restoring, whatever an earlier campaign left.
            if (_serialisedTitles.Count == 0)
            {
                _stateStore.InitialiseTitles(_initialTitlesProvider());
                _stateStore.InitialiseClaims(_initialClaimsProvider());
                _generateBloodClaimsUseCase.Execute();
                return;
            }

            // Names come from current configuration; the save owns the campaign's holder history.
            var configuredNames = _initialTitlesProvider().ToDictionary(title => title.Id, title => title.Name);
            _stateStore.InitialiseTitles(TitleStateSerialiser.DeserialiseTitles(_serialisedTitles).Select(title =>
                configuredNames.TryGetValue(title.Id, out string name) && name != title.Name
                    ? Title.Restore(title.Id, name, title.Rank, title.SeatSettlementId, title.Holders,
                        title.OccupantClanId, title.ContestedSinceDay)
                    : title).ToList());
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

        // Any death can change the claims: the dead hero's own claims lapse, and a clan
        // leader's death moves the anchor every claim on that clan's titles descends from.
        // Succession runs first, so the re-derivation below sees the new holders rather than
        // deriving a whole realm's claims from a corpse.
        private void OnHeroKilled(
            Hero victim,
            Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail,
            bool showNotification)
        {
            _executeSuccessionUseCase.Execute(victim.StringId, (float)CampaignTime.Now.ToDays);
            _generateBloodClaimsUseCase.Execute();
        }
    }
}
