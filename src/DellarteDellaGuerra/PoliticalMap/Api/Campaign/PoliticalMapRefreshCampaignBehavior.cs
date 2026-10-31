using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace DellarteDellaGuerra.PoliticalMap.Api.Campaign
{
    /**
     * <summary>
     *  Turns the campaign's political events into dirty territories on the political map. It
     *  never builds a mesh: it only marks territories in the scheduler, which the overlay drains a
     *  few chunks a frame, so no event handler pays for a rebuild.
     * </summary>
     * <remarks>
     *  A title changing holder or occupant repaints its county (feudal view). A settlement
     *  changing owner, a clan changing kingdom and a kingdom being created or destroyed repaint
     *  the counties of the settlements concerned (kingdom view), and a war or peace involving the
     *  player's faction repaints the other side's counties (the kingdom view hatches land at war
     *  with the player). Once a day every territory's paint is recomputed and compared with the
     *  last, which catches what no event announces: a holder changing clan, a clan its colour.
     * </remarks>
     */
    public class PoliticalMapRefreshCampaignBehavior : CampaignBehaviorBase
    {
        private readonly PoliticalMapTerritories _territories;
        private readonly PoliticalMapRefreshScheduler _scheduler;
        private readonly PoliticalMapPaintLedger _ledger;
        private readonly ITitleRepository _titleRepository;
        private readonly IGenealogy _genealogy;

        private Dictionary<string, List<Settlement>> _settlementsByTerritoryId = new();

        public PoliticalMapRefreshCampaignBehavior(
            PoliticalMapTerritories territories,
            PoliticalMapRefreshScheduler scheduler,
            PoliticalMapPaintLedger ledger,
            ITitleRepository titleRepository,
            IGenealogy genealogy)
        {
            _territories = territories;
            _scheduler = scheduler;
            _ledger = ledger;
            _titleRepository = titleRepository;
            _genealogy = genealogy;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomChanged);
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomChanged);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        /// <summary>
        /// Wired to the title registry's holder-or-occupant change. Every path that moves a title
        /// (assignment, succession, a won claim) saves it there, so this sees them all.
        /// </summary>
        public void OnTitleChanged(string titleId) => MarkDirty(_territories.GetTerritoriesAffectedByTitle(titleId));

        // The overlay is built whole when the map loads, so what it painted is the baseline the
        // daily comparison starts from, and nothing queued before it is still owed.
        private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            _settlementsByTerritoryId = Settlement.All
                .Select(settlement => (settlement, territoryId: TerritoryOf(settlement)))
                .Where(entry => entry.territoryId is not null)
                .GroupBy(entry => entry.territoryId!)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(entry => entry.settlement).OrderBy(settlement => settlement.StringId).ToList());

            _ledger.Clear();
            foreach (string territoryId in _settlementsByTerritoryId.Keys) _ledger.Record(territoryId, SignatureOf(territoryId));
            _scheduler.Clear();
        }

        private void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail) =>
            MarkSettlementsDirty(new[] { settlement });

        private void OnClanChangedKingdom(
            Clan clan,
            Kingdom oldKingdom,
            Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail,
            bool showNotification)
        {
            // The player's realm decides which land is hatched as hostile, so all of it repaints.
            if (clan == Clan.PlayerClan) MarkDirty(_settlementsByTerritoryId.Keys.ToList());
            else MarkSettlementsDirty(clan.Settlements);
        }

        private void OnKingdomChanged(Kingdom kingdom) => MarkSettlementsDirty(kingdom.Settlements);

        private void OnWarDeclared(IFaction side1, IFaction side2, DeclareWarAction.DeclareWarDetail detail) =>
            OnStanceChanged(side1, side2);

        private void OnMakePeace(IFaction side1, IFaction side2, MakePeaceAction.MakePeaceDetail detail) =>
            OnStanceChanged(side1, side2);

        // Only the player's wars are hatched, so a war between two other realms repaints nothing.
        private void OnStanceChanged(IFaction side1, IFaction side2)
        {
            IFaction? playerFaction = Hero.MainHero?.MapFaction;
            if (playerFaction is null) return;
            if (side1 == playerFaction) MarkSettlementsDirty(side2.Settlements);
            else if (side2 == playerFaction) MarkSettlementsDirty(side1.Settlements);
        }

        private void OnDailyTick()
        {
            foreach (string territoryId in _settlementsByTerritoryId.Keys)
            {
                if (_ledger.Record(territoryId, SignatureOf(territoryId))) _scheduler.MarkDirty(territoryId);
            }
        }

        private void MarkSettlementsDirty(IEnumerable<Settlement> settlements) =>
            MarkDirty(_territories.GetTerritoriesOfSeats(settlements
                .Select(SeatOf)
                .Where(seat => seat is not null)
                .Select(seat => seat!.StringId)));

        // The ledger takes the new paint now, so the daily comparison does not repaint the same
        // change a second time.
        private void MarkDirty(IEnumerable<string> territoryIds)
        {
            foreach (string territoryId in territoryIds)
            {
                _scheduler.MarkDirty(territoryId);
                if (_settlementsByTerritoryId.ContainsKey(territoryId)) _ledger.Record(territoryId, SignatureOf(territoryId));
            }
        }

        private string? TerritoryOf(Settlement settlement)
        {
            Settlement? seat = SeatOf(settlement);
            return seat is null ? null : _territories.GetTerritoryOfSeat(seat.StringId);
        }

        // A village's cells belong to the town or castle it is bound to.
        private static Settlement? SeatOf(Settlement settlement) =>
            settlement.IsVillage ? settlement.Village?.Bound : settlement.IsFortification ? settlement : null;

        private IReadOnlyList<uint> SignatureOf(string territoryId)
        {
            Title? title = _titleRepository.GetTitle(territoryId);
            IFaction? playerFaction = Hero.MainHero?.MapFaction;
            IEnumerable<Settlement> settlements = _settlementsByTerritoryId.TryGetValue(territoryId, out List<Settlement> found)
                ? found
                : Enumerable.Empty<Settlement>();

            return PoliticalMapPalette.Signature(
                ClanColor(_genealogy.GetHolderClanOf(title)),
                title is { IsContested: true } ? ClanColor(title.OccupantClanId) : null,
                settlements.Select(settlement => (
                    settlement.MapFaction is null
                        ? (uint?)null
                        : PoliticalMapPalette.KingdomColor(settlement.MapFaction.StringId, settlement.MapFaction.Color),
                    playerFaction is not null && settlement.MapFaction is not null
                                              && settlement.MapFaction.IsAtWarWith(playerFaction))));
        }

        private static uint? ClanColor(string? clanId) =>
            clanId is null ? null : BannerlordCampaign.Current?.CampaignObjectManager.Find<Clan>(clanId)?.Color;
    }
}
