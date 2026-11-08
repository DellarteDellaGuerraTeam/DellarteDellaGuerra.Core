using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Runs the three hierarchy use cases — suzerain, direct vassals, de jure settlements — over the
/// real 1471 title configuration. Their unit tests build tidy trees; the authored one is not tidy,
/// because it was written to look like England rather than to look like a tree. Houses hold land
/// of houses that hold land of them, eight counties answer straight to the crown with no duchy in
/// between, and the crown's own dignity has no seat at all.
/// </summary>
/// <remarks>
/// Nothing here needs a campaign: the hierarchy is static configuration joined to the genealogy,
/// so the production adapters read it exactly as they do at runtime.
/// </remarks>
public class FeudalHierarchyOverRealContentTests
{
    private const string Crown = "clan_lancaster";
    private const string ScottishCrown = "clan_stewart";
    private const string IslesCrown = "clan_macdonald_isles";

    [Theory]
    [InlineData("duchy_wales", "dadg_lord_2_3")] // Edward, Prince of Wales, under Henry VI
    [InlineData("county_lincoln", "dadg_lord_14_4")] // John de la Pole, Earl of Lincoln, under his father
    public void AnHeirStyledWithTheDignityHoldsItWhileAnotherLeadsTheHouse(string titleId, string heroId)
    {
        Assert.Equal(heroId, Content.Load().AllTitles().Single(title => title.Id == titleId).HolderHeroId);
    }

    [Theory]
    [InlineData("clan_courtenay", "Earldom of Devon")]
    [InlineData("clan_tudor", "Earldom of Pembroke")]
    [InlineData("clan_woodville", "Earldom of Rivers")]
    [InlineData("clan_talbot", "Earldom of Shrewsbury")]
    [InlineData("clan_bourchier", "Earldom of Essex")]
    [InlineData("clan_grey_of_ruthin", "Earldom of Kent")]
    [InlineData("clan_fitzalan", "Earldom of Arundel")]
    [InlineData("clan_de_vere", "Earldom of Oxford")]
    [InlineData("clan_lindsay", "Earldom of Crawford")]
    [InlineData("clan_de_la_pole", "Duchy of Suffolk")]
    [InlineData("clan_holland", "Duchy of Exeter")]
    public void ThePrimaryHeldTitleNamesTheDignityRatherThanItsGeographicCounty(string clanId, string expectedName)
    {
        Content content = Content.Load();
        string titleId = content.PrimaryTitleId(clanId)!;

        Assert.Equal(expectedName, content.AllTitles().Single(title => title.Id == titleId).Name);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EveryHouseInTheRealmAnswersToTheCrownThroughAChainThatTerminates()
    {
        // The chain is walked at runtime to resolve war sides, so a loop in it would hang the
        // daily tick rather than merely mis-answer. Seventy-two houses hold land across three
        // realms, and only the three kings answer to nobody. Forty-six of them are England's,
        // and every chain must terminate after ranks strictly increase.
        Content content = Content.Load();
        IReadOnlyList<string> holdingClans = content.HoldingClans();

        Assert.Equal(72, holdingClans.Count);
        Assert.Equal(
            new[] { Crown, ScottishCrown, IslesCrown }.OrderBy(clan => clan),
            holdingClans.Where(clan => content.Suzerain(clan) is null));

        var depths = new Dictionary<string, int>();
        var realms = new Dictionary<string, string>();
        foreach (string clanId in holdingClans)
        {
            var walked = new List<string> { clanId };
            string? current = content.Suzerain(clanId);
            while (current is not null)
            {
                Assert.DoesNotContain(current, walked);
                walked.Add(current);
                current = content.Suzerain(current);
            }

            realms[clanId] = walked[^1];
            depths[clanId] = walked.Count - 1;
        }

        Assert.Equal(46, realms.Count(entry => entry.Value == Crown));
        Assert.Equal(14, realms.Count(entry => entry.Value == ScottishCrown));
        Assert.Equal(12, realms.Count(entry => entry.Value == IslesCrown));

        // Each realm's houses are the houses holding land in that realm's own title tree, save the
        // Lord of the Isles: he holds Urquhart in Scotland's tree but, a king, answers to nobody.
        Assert.Equal(content.HoldingClansUnder("kingdom_england"), InRealm(Crown));
        Assert.Equal(
            content.HoldingClansUnder("kingdom_scotland").Where(clan => clan != IslesCrown),
            InRealm(ScottishCrown));
        Assert.Equal(content.HoldingClansUnder("kingdom_isles"), InRealm(IslesCrown));

        Assert.Equal(0, depths[Crown]);
        Assert.Equal(2, InRealm(Crown).Max(clan => depths[clan]));
        Assert.Equal(1, InRealm(ScottishCrown).Max(clan => depths[clan]));
        Assert.Equal(1, InRealm(IslesCrown).Max(clan => depths[clan]));
        Assert.All(holdingClans.Where(clan => content.Suzerain(clan) is not null), clan =>
            Assert.True(content.HighestRank(content.Suzerain(clan)!) > content.HighestRank(clan), clan));

        IEnumerable<string> InRealm(string king) =>
            realms.Where(entry => entry.Value == king).Select(entry => entry.Key).OrderBy(clan => clan);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheSuzerainAndDirectVassalViewsPartitionTheRealmTheSameWay()
    {
        // Two use cases answer the same question from opposite ends, and the campaign layer uses
        // both. They agree here on all seventy-three houses: each non-royal house is named by
        // exactly one liege, and the three kings are named by none, which is the same statement
        // as "the chains form one tree per realm" arrived at the other way round.
        Content content = Content.Load();
        IReadOnlyList<string> holdingClans = content.HoldingClans();
        string[] kings = { Crown, ScottishCrown, IslesCrown };

        var vassalsByLiege = holdingClans.ToDictionary(clan => clan, content.DirectVassals);
        IReadOnlyList<string> england = content.HoldingClansUnder("kingdom_england");

        Assert.Equal(25, vassalsByLiege[Crown].Count);
        Assert.Equal(14, vassalsByLiege.Count(entry => england.Contains(entry.Key) && entry.Value.Count > 0));

        foreach (string clanId in holdingClans)
        {
            string[] liegesNamingIt = vassalsByLiege
                .Where(entry => entry.Value.Contains(clanId))
                .Select(entry => entry.Key)
                .ToArray();

            if (kings.Contains(clanId)) Assert.Empty(liegesNamingIt);
            else Assert.Equal(content.Suzerain(clanId), Assert.Single(liegesNamingIt));
        }

        Assert.All(
            vassalsByLiege,
            entry => Assert.All(entry.Value, vassal => Assert.Equal(entry.Key, content.Suzerain(vassal))));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AHouseAnswersForItsHighestDignityEvenWhereItHoldsLandOfItsOwnVassal()
    {
        // Territorial nesting still places Talbot's Hallamshire under York. His earldom of
        // Shrewsbury answers to the Crown; York's lesser Ludlow estate does not reverse that.
        Content content = Content.Load();

        Assert.Equal(TitleRank.Duke, content.HighestRank("clan_york"));
        Assert.Equal(TitleRank.Earl, content.HighestRank("clan_talbot"));

        Assert.Equal(Crown, content.Suzerain("clan_york"));
        Assert.Equal(Crown, content.Suzerain("clan_talbot"));

        Assert.Equal("clan_york", content.ClanHolding("barony_ludlow"));
        Assert.Equal("clan_talbot", content.ClanHolding("county_shrewsbury"));
        Assert.Equal("county_shrewsbury", content.SuzerainTitleOf("barony_ludlow"));

        Assert.Equal("clan_talbot", content.ClanHolding("county_hallamshire"));
        Assert.Equal("duchy_york", content.SuzerainTitleOf("county_hallamshire"));

        // Cumberland was not a Clifford earldom in 1471, and its map position is not evidence
        // that Clifford was a Warwick retainer.
        Assert.Equal(TitleRank.Baron, content.HighestRank("clan_clifford"));
        Assert.Equal(Crown, content.Suzerain("clan_clifford"));
        Assert.Equal(TitleRank.Earl, content.HighestRank("clan_neville_of_middleham"));
        Assert.Equal(Crown, content.Suzerain("clan_neville_of_middleham"));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AHistoricalDignityOutranksOtherEstatesAndLegacyTiesUseTenure()
    {
        // Suffolk's dukedom is independent of Norfolk. Lovell's remaining Northampton estate
        // uses the territorial fallback; the Crown holds his forfeited Wardour estate.
        Content content = Content.Load();

        Assert.All(
            content.AllTitles().Where(title => title.HolderHeroId is not null),
            title => Assert.True(title.HeldSinceDay > 0f, $"{title.Id} has no heldSince year"));

        Assert.Equal("county_suffolk", content.PrimaryTitleId("clan_de_la_pole"));
        Assert.Equal(TitleRank.Duke, content.HighestRank("clan_de_la_pole"));
        Assert.Equal(Crown, content.Suzerain("clan_de_la_pole"));

        Assert.Equal(TitleRank.Duke, content.HighestRank("clan_holland"));
        Assert.Equal(Crown, content.Suzerain("clan_holland"));

        Assert.Equal("barony_northampton", content.PrimaryTitleId("clan_lovell"));
        Assert.Equal("clan_neville_of_middleham", content.Suzerain("clan_lovell"));
        Assert.Equal(Crown, content.ClanHolding("barony_wardour"));
    }

    [Fact]
    public void SourcedFollowingAndScottishDignitiesDoNotDependOnMapTiers()
    {
        Content content = Content.Load();
        Assert.Equal("clan_neville_of_middleham", content.Suzerain("clan_fitzhugh"));
        Assert.Equal("duchy_warwick", content.SuzerainTitleOf("barony_skelton"));
        Assert.Equal(Crown, content.Suzerain("clan_hastings"));
        Assert.Equal(TitleRank.Earl, content.HighestRank("clan_gordon"));
        Assert.Equal(TitleRank.Earl, content.HighestRank("clan_douglas_angus"));
        Assert.Equal("clan_douglas_lochleven", content.ClanHolding("barony_lochleven"));
        Assert.Equal("barony_lochleven", content.PrimaryTitleId("clan_douglas_lochleven"));
        Assert.Equal(TitleRank.Baron, content.HighestRank("clan_douglas_lochleven"));
        Assert.Equal(ScottishCrown, content.Suzerain("clan_douglas_lochleven"));
        Assert.Equal(1463f * 84, content.AllTitles().Single(title => title.Id == "barony_lochleven").HeldSinceDay);
        Assert.Equal(TitleRank.Earl, content.HighestRank("clan_sinclair"));
        Assert.Equal("barony_caithness", content.PrimaryTitleId("clan_sinclair"));
        Assert.Equal(TitleRank.Baron, content.HighestRank("clan_hamilton"));
        Assert.Equal(TitleRank.Baron, content.HighestRank("clan_maxwell"));
        Assert.Equal(TitleRank.Baron, content.HighestRank("clan_kennedy"));
        Assert.Equal(TitleRank.Baron, content.HighestRank("clan_howard"));
        Assert.NotEmpty(content.DirectVassals("clan_neville_of_middleham"));
        Assert.NotEmpty(content.DirectVassals("clan_talbot"));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EverySeatLiesInExactlyOneOfTheThreeRealms()
    {
        // England's realm holds ninety-six seats, Scotland's twenty-eight and the Isles' fifteen.
        // The three kingdoms and their regions and shires are the only titles authored without a
        // seat of their own, and the three realms between them account for every seat in the
        // content, none twice.
        Content content = Content.Load();

        Assert.Equal(207, content.AllTitles().Count);
        Assert.Equal(
            new[] { "kingdom_england", "kingdom_isles", "kingdom_scotland" },
            content.AllTitles()
                .Where(title => string.IsNullOrEmpty(title.SeatSettlementId) && title.Rank == TitleRank.King)
                .Select(title => title.Id)
                .OrderBy(id => id));
        Assert.Equal(68, content.AllTitles().Count(title => string.IsNullOrEmpty(title.SeatSettlementId)));

        IReadOnlyList<string> realm = content.SeatsUnder("kingdom_england").ToList();
        IReadOnlyList<string> scotland = content.SeatsUnder("kingdom_scotland").ToList();
        IReadOnlyList<string> isles = content.SeatsUnder("kingdom_isles").ToList();

        Assert.Equal(96, realm.Count);
        Assert.Equal(96, realm.Distinct().Count());
        Assert.Equal(28, scotland.Count);
        Assert.Equal(28, scotland.Distinct().Count());
        Assert.Equal(15, isles.Count);
        Assert.Equal(15, isles.Distinct().Count());

        Assert.Equal(
            content.AllTitles()
                .Select(title => title.SeatSettlementId)
                .Where(seat => !string.IsNullOrEmpty(seat))
                .OrderBy(seat => seat),
            realm.Concat(scotland).Concat(isles).OrderBy(seat => seat));

        // And every one of them has a house to hold it: no Scottish dignity is left to nobody.
        Assert.All(
            content.AllTitles().Where(title => title.Id != "kingdom_england" && !realm.Contains(title.SeatSettlementId)
                && (title.Rank == TitleRank.King || !string.IsNullOrEmpty(title.SeatSettlementId))),
            title => Assert.NotNull(content.ClanHolding(title.Id)));
    }

    [Fact]
    public void AClaimOnTheDuchyOfYorkPutsTheYorkInheritanceAtStake_NotItsFollowers()
    {
        var content = Content.Load();

        IReadOnlyList<string> inheritance = content.InheritanceSettlements("duchy_york");

        Assert.Equal(
            new[] { "dadg_Fotheringhay_castle", "York_town", "dadg_Richmond_castle", "dadg_Helmsley_castle", "Kingston_upon_Hull_town" },
            inheritance);
        // Sheffield (Talbot) and Lincoln (de la Pole) follow York but are their own houses' inheritances.
        Assert.DoesNotContain(content.SeatOf("county_hallamshire"), inheritance);
        Assert.DoesNotContain(content.SeatOf("county_lincoln"), inheritance);
        // A claim on a barony still disputes just that estate.
        Assert.Equal(new[] { content.SeatOf("barony_middleham") }, content.InheritanceSettlements("barony_middleham"));
    }

    /// <summary>
    /// The real content wired through the production adapters, exposing the hierarchy use
    /// cases over one shared load.
    /// </summary>
    private sealed class Content
    {
        private readonly DadgXmlGenealogy _genealogy;
        private readonly InMemoryTitleRegistry _titles;
        private readonly XmlFeudalStructure _structure;
        private readonly SuzeraintyPolicy _suzerain;
        private readonly GetDirectVassalsUseCase _directVassals;
        private readonly GetInheritanceSettlementsUseCase _inheritanceSettlements;

        private Content(DadgXmlGenealogy genealogy, XmlFeudalStructure structure)
        {
            _genealogy = genealogy;
            _structure = structure;
            _titles = new InMemoryTitleRegistry(genealogy);
            _titles.Initialise(structure.BuildInitialTitles(genealogy));
            _suzerain = new SuzeraintyPolicy(_titles, structure, genealogy);
            _directVassals = new GetDirectVassalsUseCase(_titles, structure, _suzerain, genealogy);
            _inheritanceSettlements = new GetInheritanceSettlementsUseCase(_titles, structure, genealogy);
        }

        public static Content Load()
        {
            using Stream titlesStream = DadgContent.OpenTitles();
            using Stream heroesStream = DadgContent.OpenHeroes();
            using Stream charactersStream = DadgContent.OpenCharacters();
            using Stream clansStream = DadgContent.OpenClans();

            return new Content(
                DadgXmlGenealogy.Load(heroesStream, charactersStream, clansStream),
                new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream)));
        }

        public IReadOnlyList<Title> AllTitles() => _titles.GetAllTitles();

        public string? Suzerain(string clanId) => _suzerain.GetSuzerain(clanId);

        public TitleRank? HighestRank(string clanId) => _suzerain.GetPrimaryTitle(clanId)?.Rank;

        public string? PrimaryTitleId(string clanId) => _suzerain.GetPrimaryTitle(clanId)?.Id;

        public IReadOnlyList<string> DirectVassals(string clanId) => _directVassals.Execute(clanId);

        public IReadOnlyList<string> InheritanceSettlements(string titleId) => _inheritanceSettlements.Execute(titleId);

        public string? SeatOf(string titleId) => _titles.GetTitle(titleId)?.SeatSettlementId;

        public string? SuzerainTitleOf(string titleId) => _structure.GetAllegianceSuzerainTitleId(titleId);

        public string? ClanHolding(string titleId) => _genealogy.GetHolderClanOf(_titles.GetTitle(titleId));

        public IReadOnlyList<string> HoldingClans() => HoldingClansOf(_titles.GetAllTitles());

        /// <summary>Every house holding a title in the tree under a root title, the root included.</summary>
        public IReadOnlyList<string> HoldingClansUnder(string rootTitleId) =>
            HoldingClansOf(_titles.GetAllTitles().Where(title => RootOf(title.Id) == rootTitleId));

        /// <summary>The seats of every title in the tree under a root title, read off the titles themselves.</summary>
        public IOrderedEnumerable<string> SeatsUnder(string rootTitleId) =>
            _titles.GetAllTitles()
                .Where(title => RootOf(title.Id) == rootTitleId && !string.IsNullOrEmpty(title.SeatSettlementId))
                .Select(title => title.SeatSettlementId)
                .OrderBy(seat => seat);

        private string RootOf(string titleId)
        {
            string current = titleId;
            while (_structure.GetDeJureSuzerainTitleId(current) is { } parent) current = parent;
            return current;
        }

        private IReadOnlyList<string> HoldingClansOf(IEnumerable<Title> titles) =>
            titles
                .Select(title => _genealogy.GetHolderClanOf(title))
                .Where(clanId => clanId is not null)
                .Select(clanId => clanId!)
                .Distinct()
                .OrderBy(clanId => clanId)
                .ToList();
    }
}
