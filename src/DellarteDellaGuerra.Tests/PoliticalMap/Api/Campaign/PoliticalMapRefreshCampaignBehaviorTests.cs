using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;

namespace DellarteDellaGuerra.Tests.PoliticalMap.Api.Campaign
{
    public class PoliticalMapRefreshCampaignBehaviorTests
    {
        [Fact]
        public void ATitleChangingHandsQueuesItsCountyAndBuildsNothing()
        {
            var scheduler = new PoliticalMapRefreshScheduler();
            var behavior = NewBehavior(scheduler);

            behavior.OnTitleChanged("barony_piel");
            behavior.OnTitleChanged("county_lancashire");

            Assert.Equal(new[] { "county_lancashire" }, scheduler.Pending);
        }

        [Fact]
        public void ASeatlessCrownChangingHandsQueuesNothing()
        {
            var scheduler = new PoliticalMapRefreshScheduler();

            NewBehavior(scheduler).OnTitleChanged("kingdom_england");

            Assert.False(scheduler.HasWork);
        }

        // Before the session launches no territory has settlements, so marking reads no game state.
        private static PoliticalMapRefreshCampaignBehavior NewBehavior(PoliticalMapRefreshScheduler scheduler)
        {
            var titles = new Titles(
                new Title("kingdom_england", "England", TitleRank.King, "", "henry"),
                new Title("county_lancashire", "County of Lancashire", TitleRank.Earl, "Lancaster_town", "henry"),
                new Title("barony_piel", "Barony of Piel", TitleRank.Baron, "dadg_Piel_castle", "harrington"));
            var structure = new Structure(new Dictionary<string, string?>
            {
                ["kingdom_england"] = null,
                ["county_lancashire"] = "kingdom_england",
                ["barony_piel"] = "county_lancashire"
            }, titles);

            return new PoliticalMapRefreshCampaignBehavior(
                new PoliticalMapTerritories(structure, titles), scheduler, new PoliticalMapPaintLedger(), titles, null!);
        }

        private sealed class Titles : ITitleRepository
        {
            private readonly Dictionary<string, Title> _titles;

            public Titles(params Title[] titles) => _titles = titles.ToDictionary(title => title.Id);

            public Title? GetTitle(string titleId) => _titles.GetValueOrDefault(titleId);
            public Title? GetTitleBySeat(string settlementId) => _titles.Values.FirstOrDefault(title => title.SeatSettlementId == settlementId);
            public IReadOnlyList<Title> GetTitlesByClan(string clanId) => Array.Empty<Title>();
            public IReadOnlyList<Title> GetAllTitles() => _titles.Values.ToList();
            public void SaveTitle(Title title) => _titles[title.Id] = title;
        }

        private sealed class Structure : IFeudalStructure
        {
            public string? GetAllegianceSuzerainTitleId(string titleId) => GetDeJureSuzerainTitleId(titleId);
            private readonly Dictionary<string, string?> _suzerains;
            private readonly Titles _titles;

            public Structure(Dictionary<string, string?> suzerains, Titles titles)
            {
                _suzerains = suzerains;
                _titles = titles;
            }

            public string? GetDeJureSuzerainTitleId(string titleId) => _suzerains.GetValueOrDefault(titleId);
            public IReadOnlyList<string> GetDeJureVassalTitleIds(string titleId) =>
                _suzerains.Where(entry => entry.Value == titleId).Select(entry => entry.Key).ToList();
            public string? GetTitleIdBySeat(string settlementId) => _titles.GetTitleBySeat(settlementId)?.Id;
            public IReadOnlyList<string> GetAllTitleIds() => _suzerains.Keys.ToList();
            public TitleRank? GetRank(string titleId) => _titles.GetTitle(titleId)?.Rank;
            public TitleRank? GetMapLabelRank(string titleId) => GetRank(titleId);
            public string? GetTitleName(string titleId) => _titles.GetTitle(titleId)?.Name;
            public void Reattach(string titleId, string? suzerainTitleId) => _suzerains[titleId] = suzerainTitleId;
        }
    }
}
