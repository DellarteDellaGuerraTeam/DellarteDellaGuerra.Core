using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Kills a title holder in the real 1471 content and runs the same two use cases, in the same
/// order, that <c>FeudalTitleCampaignBehavior.OnHeroKilled</c> runs: succession first, then claim
/// re-derivation. The authored bloodlines are far knottier than any hand-built fake — cadet
/// branches, daughters married into rival clans, lines that died out — so these pin the rules
/// down against families nobody wrote to suit them.
/// </summary>
/// <remarks>
/// Seniority is real: <c>age</c> sits on the NPCCharacter template rather than on the hero record
/// that carries the bloodline, and the adapter joins the two by id. Ordering is therefore under
/// test here, not just the fallbacks.
/// </remarks>
public class HolderDeathOverRealContentTests
{
    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AnEarlsFiveDignitiesPassToHisOnlyDaughterRatherThanToHisBrother()
    {
        // Warwick's only child is a daughter of fourteen who married into Lancaster; his brother
        // is forty and heads a cadet branch of six. Male preference asks for a son, not for a man:
        // with no son's line left, the girl takes precedence over her uncle whatever their ages,
        // and five dignities leave the clan with her.
        const string warwick = "dadg_lord_9_1";
        const string daughter = "dadg_lord_2_4";
        const string brother = "dadg_lord_9_2";

        Content content = Content.Load();
        Content afterDeath = content.WithDeceased(warwick);

        IReadOnlyList<SuccessionResult> results = afterDeath.ExecuteSuccession(warwick);

        Assert.Equal(
            new[]
            {
                "barony_caerphilly", "barony_middleham", "barony_rye", "barony_warwick",
                "county_glamorgan"
            },
            results.Select(result => result.TitleId).OrderBy(id => id));
        Assert.All(results, result => Assert.Equal(daughter, result.NewHolderHeroId));
        Assert.All(results, result => Assert.Equal(warwick, result.PreviousHolderHeroId));

        Assert.True(content.Hero(daughter).IsFemale);
        Assert.False(content.Hero(brother).IsFemale);
        Assert.True(content.Hero(brother).IsAlive);
        Assert.Equal(new[] { daughter }, content.Hero(warwick).ChildIds);
        Assert.DoesNotContain(results, result => result.NewHolderHeroId == brother);

        // The uncle is the elder by twenty-six years, so only the order of the two rules —
        // the holder's own body before his father's other children — can explain this.
        Assert.True(content.Hero(brother).Age > content.Hero(daughter).Age);

        // The dignities do not merely change hands, they change clan.
        Assert.Equal("clan_neville_of_middleham", content.ClanHolding("barony_middleham"));
        Assert.Equal("clan_lancaster", afterDeath.ClanHolding("barony_middleham"));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void ChildlessTheDukeIsSucceededByHisEldestSurvivingBrother()
    {
        // Somerset dies childless with four siblings living and one dead. Two rules bite at once:
        // his eldest sister is fifty-two and takes nothing, because every brother is asked first;
        // and his dead brother's son is passed over, because representation stands a nephew in
        // his father's place, not ahead of an uncle who was the elder of the two to begin with.
        const string somerset = "dadg_lord_6_1";
        const string survivingBrother = "dadg_lord_6_2";
        const string predeceasedBrother = "dadg_dead_lord_6_3";
        const string nephew = "dadg_lord_6_3";
        const string eldestSister = "dadg_lord_32_4";

        Content content = Content.Load();
        Content afterDeath = content.WithDeceased(somerset);

        IReadOnlyList<SuccessionResult> results = afterDeath.ExecuteSuccession(somerset);

        Assert.Equal(
            new[] { "county_bristol", "duchy_somerset" },
            results.Select(result => result.TitleId).OrderBy(id => id));
        Assert.All(results, result => Assert.Equal(survivingBrother, result.NewHolderHeroId));

        Assert.Empty(content.Hero(somerset).ChildIds);
        Assert.Equal(content.Hero(somerset).FatherId, content.Hero(survivingBrother).FatherId);

        // The sister is the eldest of the generation and is still never considered.
        Assert.True(content.Hero(eldestSister).IsFemale);
        Assert.True(content.Hero(eldestSister).Age > content.Hero(survivingBrother).Age);
        Assert.Equal(content.Hero(somerset).FatherId, content.Hero(eldestSister).FatherId);

        // The nephew loses on his father's seniority, not on his own.
        Assert.Equal(predeceasedBrother, content.Hero(nephew).FatherId);
        Assert.False(content.Hero(predeceasedBrother).IsAlive);
        Assert.True(content.Hero(survivingBrother).Age > content.Hero(predeceasedBrother).Age);
    }

    [Theory]
    [Trait("Category", "DADG content integration")]
    [InlineData("dadg_lord_23_1", "county_lynn", "duchy_norfolk")]
    [InlineData("dadg_lord_26_1", "barony_penrith", "county_durham")]
    [InlineData("dadg_lord_49_1", "barony_northampton")]
    [InlineData("dadg_lord_52_1", "barony_montgomery")]
    [InlineData("dadg_lord_8_1", "barony_skipton", "county_cumberland")]
    public void ATitleIsLeftVacantWhenTheLineDiesOutWithItsHolder(
        string holderId,
        params string[] expectedTitleIds)
    {
        // These five head their clans and leave neither issue nor sibling. The clan-head backstop
        // names the dead man himself, so it refuses, and the dignity waits for the next grant
        // rather than being handed to a corpse.
        Content content = Content.Load();
        Content afterDeath = content.WithDeceased(holderId);

        IReadOnlyList<SuccessionResult> results = afterDeath.ExecuteSuccession(holderId);

        Assert.Equal(expectedTitleIds.OrderBy(id => id), results.Select(r => r.TitleId).OrderBy(id => id));
        Assert.All(results, result => Assert.Null(result.NewHolderHeroId));
        Assert.Empty(content.Hero(holderId).ChildIds);
        Assert.Equal(holderId, content.ClanLeader(content.Hero(holderId).ClanId!));
        Assert.All(expectedTitleIds, titleId => Assert.Null(afterDeath.ClanHolding(titleId)));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EveryHolderInTheContentCanDieWithoutOrphaningOrMisplacingHisDignities()
    {
        // The sweep that matters: a death is the one event this system cannot decline to handle,
        // and it fires for all 79 holders over bloodlines authored by hand — England's 53 and the
        // 26 of Scotland and the Isles. It is what caught a
        // duke listed as his own mother, whose cycle overflowed the descent walk's stack; the
        // record has since been corrected, and the walk's guard is unit-tested against a fake.
        Content content = Content.Load();
        IReadOnlyList<string> holderIds = content.HolderIds();

        Assert.Equal(79, holderIds.Count);

        foreach (string holderId in holderIds)
        {
            Content afterDeath = content.WithDeceased(holderId);
            IReadOnlyList<SuccessionResult> results = afterDeath.ExecuteSuccession(holderId);

            Assert.NotEmpty(results);
            Assert.Equal(content.TitlesHeldBy(holderId), results.Select(r => r.TitleId).OrderBy(id => id));

            foreach (SuccessionResult result in results)
            {
                Assert.Equal(holderId, result.PreviousHolderHeroId);
                if (result.NewHolderHeroId is not { } heirId) continue;

                Assert.NotEqual(holderId, heirId);
                Assert.True(afterDeath.Hero(heirId).IsAlive, $"'{holderId}' left '{heirId}' a dead heir");
                Assert.Equal(heirId, afterDeath.Title(result.TitleId).HolderHeroId);
            }

            // One death, one heir: a man's dignities do not scatter.
            Assert.Single(results.Select(result => result.NewHolderHeroId).Distinct());
        }
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheClaimsAreRederivedFromTheNewHolderOnceTheSuccessionHasRun()
    {
        // The whole OnHeroKilled chain. Warwick's five dignities pass to his only child and every
        // claim on them lapses: she was a claimant and is now the holder, she has neither children
        // nor a brother or sister, and the aunts and cousins who held claims through Warwick's
        // father now stand two levels above her — out of reach of the collateral rule derivation
        // mirrors. Sixty claims go, none appear, and no other title is touched.
        const string warwick = "dadg_lord_9_1";
        string[] warwicksTitles =
        {
            "barony_caerphilly", "barony_middleham", "barony_rye", "barony_warwick", "county_glamorgan"
        };

        Content content = Content.Load();
        IReadOnlyList<Claim> before = content.GenerateClaims();

        Content afterDeath = content.WithDeceased(warwick);
        afterDeath.ExecuteSuccession(warwick);
        IReadOnlyList<Claim> after = afterDeath.GenerateClaims();

        Assert.Equal(399, before.Count);
        Assert.Equal(339, after.Count);

        Assert.Equal(60, Key(before).Except(Key(after)).Count());
        Assert.Equal(
            warwicksTitles,
            Key(before).Except(Key(after)).Select(key => key.Split('|')[0]).Distinct().OrderBy(id => id));
        Assert.Empty(Key(after).Except(Key(before)));

        Assert.DoesNotContain(after, claim => warwicksTitles.Contains(claim.TitleId));
        Assert.DoesNotContain(after, claim => claim.ClaimantHeroId == "dadg_lord_2_4");
    }

    private static IEnumerable<string> Key(IEnumerable<Claim> claims) =>
        claims.Select(claim => $"{claim.TitleId}|{claim.ClaimantHeroId}");

    /// <summary>
    /// The real content wired through the production adapters, with the titles rebuilt from the
    /// living structure so that a death only ever changes who is alive.
    /// </summary>
    private sealed class Content
    {
        private readonly DadgXmlGenealogy _genealogy;
        private readonly InMemoryTitleRegistry _titles;

        private Content(DadgXmlGenealogy genealogy, IReadOnlyList<Title> initialTitles)
        {
            _genealogy = genealogy;
            _titles = new InMemoryTitleRegistry(genealogy);
            _titles.Initialise(initialTitles);
        }

        public static Content Load()
        {
            using Stream titlesStream = DadgContent.OpenTitles();
            using Stream heroesStream = DadgContent.OpenHeroes();
            using Stream charactersStream = DadgContent.OpenCharacters();
            using Stream clansStream = DadgContent.OpenClans();

            DadgXmlGenealogy genealogy = DadgXmlGenealogy.Load(heroesStream, charactersStream, clansStream);
            var structure = new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream));

            return new Content(genealogy, structure.BuildInitialTitles(genealogy));
        }

        public Content WithDeceased(string heroId) =>
            new(_genealogy.WithDeceased(heroId), _titles.GetAllTitles());

        public IReadOnlyList<SuccessionResult> ExecuteSuccession(string deceasedHeroId) =>
            new ExecuteSuccessionUseCase(_titles, _genealogy, new SilentLoggerFactory())
                .Execute(deceasedHeroId, 0f);

        public IReadOnlyList<Claim> GenerateClaims() =>
            new GenerateBloodClaimsUseCase(_titles, new InMemoryClaimRegistry(), _genealogy).Execute();

        public HeroNode Hero(string heroId) =>
            _genealogy.GetHero(heroId) ?? throw new InvalidOperationException($"no hero '{heroId}'");

        public Title Title(string titleId) =>
            _titles.GetTitle(titleId) ?? throw new InvalidOperationException($"no title '{titleId}'");

        public string? ClanLeader(string clanId) => _genealogy.GetClanLeaderId(clanId);

        public string? ClanHolding(string titleId) => _genealogy.GetHolderClanOf(Title(titleId));

        public IReadOnlyList<string> HolderIds() =>
            _titles.GetAllTitles()
                .Select(title => title.HolderHeroId)
                .Where(holderId => holderId is not null)
                .Select(holderId => holderId!)
                .Distinct()
                .OrderBy(holderId => holderId)
                .ToList();

        public IOrderedEnumerable<string> TitlesHeldBy(string holderId) =>
            _titles.GetAllTitles()
                .Where(title => title.HolderHeroId == holderId)
                .Select(title => title.Id)
                .OrderBy(id => id);
    }
}
