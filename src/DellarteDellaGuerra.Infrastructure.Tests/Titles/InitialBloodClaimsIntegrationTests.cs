using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class InitialBloodClaimsIntegrationTests
{
    private const string ExpectedClaims =
        """
        barony_barnard|dadg_lord_51_2|clan_lumley|Weak
        barony_caerphilly|dadg_lord_2_4|clan_lancaster|Weak
        barony_durham|dadg_lord_50_2|clan_scrope_of_masham|Weak
        barony_farleigh|dadg_lord_5_2|clan_grey_of_groby|Weak
        barony_middleham|dadg_lord_2_4|clan_lancaster|Weak
        barony_rye|dadg_lord_2_4|clan_lancaster|Weak
        barony_sherborne|dadg_lord_5_2|clan_grey_of_groby|Weak
        barony_skelton|dadg_lord_31_2|clan_scrope_of_bolton|Weak
        barony_skelton|dadg_lord_43_4|clan_fienne_of_dacre|Weak
        barony_skelton|dadg_lord_49_2|clan_lovell|Weak
        barony_tonbridge|dadg_lord_48_2|clan_clinton|Weak
        barony_warwick|dadg_lord_2_4|clan_lancaster|Weak
        county_cheshire|dadg_lord_1_4|clan_york|Weak
        county_cheshire|dadg_lord_15_5|clan_stafford|Strong
        county_cheshire|dadg_lord_15_6|clan_stafford|Weak
        county_cheshire|dadg_lord_24_6|clan_grey_of_ruthin|Weak
        county_cheshire|dadg_lord_3_3|clan_fitzalan|Weak
        county_cheshire|dadg_mary_woodville_1443|clan_herbert|Weak
        county_cornwall|dadg_lord_8_6|clan_clifford|Weak
        county_cumberland|dadg_lord_53_4|clan_sutton|Weak
        county_devon|dadg_lord_5_2|clan_grey_of_groby|Weak
        county_essex|dadg_lord_23_3|clan_mowbray|Weak
        county_glamorgan|dadg_lord_11_2|clan_hastings|Weak
        county_glamorgan|dadg_lord_2_4|clan_lancaster|Weak
        county_glamorgan|dadg_lord_30_2|clan_fitzhugh|Weak
        county_glamorgan|dadg_lord_41_2|clan_de_vere|Weak
        county_glamorgan|dadg_lord_45_2|clan_blount|Weak
        county_hallamshire|dadg_lord_20_2|clan_vernon|Weak
        county_hallamshire|dadg_lord_23_2|clan_mowbray|Weak
        county_hampshire|dadg_lord_19_2|clan_talbot|Weak
        county_hampshire|dadg_lord_29_2|clan_beaumont|Weak
        county_hampshire|dadg_lord_41_4|clan_de_vere|Weak
        county_hampshire|dadg_lord_54_3|clan_beauchamp|Weak
        county_hereford|dadg_lord_15_4|clan_stafford|Weak
        county_hereford|dadg_lord_28_2|clan_stanley|Weak
        county_kent|dadg_lord_22_3|clan_greystoke|Weak
        duchy_northumberland|dadg_lord_41_7|clan_de_vere|Weak
        duchy_somerset|dadg_lord_25_2|clan_grey_of_wilton|Weak
        duchy_somerset|dadg_lord_32_4|clan_basset|Weak
        duchy_somerset|dadg_lord_7_3|clan_tudor|Weak
        duchy_york|dadg_lord_14_3|clan_de_la_pole|Weak
        duchy_york|dadg_lord_17_2|clan_holland|Weak
        """;

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void Real1471ContentProducesTheExpectedInitialClaims()
    {
        DadgContentPaths paths = DadgContentPaths.Find();
        using FileStream titlesStream = File.OpenRead(paths.Titles);

        DadgXmlGenealogy genealogy = DadgXmlGenealogy.Load(paths.Heroes, paths.Characters, paths.Clans);
        var feudalStructure = new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream));
        var titleRepository = new InMemoryTitleRegistry(genealogy);
        titleRepository.Initialise(feudalStructure.BuildInitialTitles(genealogy));

        var claimRepository = new InMemoryClaimRegistry();
        var useCase = new GenerateBloodClaimsUseCase(titleRepository, claimRepository, genealogy);

        IReadOnlyList<Claim> claims = useCase.Execute();

        Assert.Equal(ExpectedClaims, Project(claims));
        Assert.Equal(97, titleRepository.GetAllTitles().Count);
        Assert.Equal(
            53,
            titleRepository.GetAllTitles()
                .Where(title => genealogy.GetHolderClanOf(title) is not null)
                .Select(title => genealogy.GetHolderClanOf(title))
                .Distinct()
                .Count());
        Assert.Equal(442, genealogy.HeroCount);
        Assert.Equal(42, claims.Count);
        Assert.Equal(1, claims.Count(claim => claim.Strength == ClaimStrength.Strong));
        Assert.Equal(41, claims.Count(claim => claim.Strength == ClaimStrength.Weak));
        Assert.Equal(31, claims.Select(claim => claim.ClaimantClanId).Distinct().Count());
        Assert.Equal(23, claims.Select(claim => claim.TitleId).Distinct().Count());
        Assert.All(claims, claim => Assert.False(string.IsNullOrEmpty(claim.ClaimantHeroId)));
        Assert.Equal(claims.Count, claims.Select(claim => claim.Id).Distinct().Count());
        Assert.All(
            titleRepository.GetAllTitles().Where(title => genealogy.GetHolderClanOf(title) is not null),
            title =>
            {
                string? leaderId = genealogy.GetClanLeaderId(genealogy.GetHolderClanOf(title)!);
                Assert.False(string.IsNullOrEmpty(leaderId));
                Assert.NotNull(genealogy.GetHero(leaderId!));
            });
        Assert.All(
            claims,
            claim =>
            {
                HeroNode claimant = Assert.IsType<HeroNode>(genealogy.GetHero(claim.ClaimantHeroId!));
                Title title = Assert.IsType<Title>(titleRepository.GetTitle(claim.TitleId));
                Assert.True(claimant.IsAlive);
                Assert.Equal(claimant.ClanId, claim.ClaimantClanId);
                Assert.NotEqual(genealogy.GetHolderClanOf(title), claim.ClaimantClanId);
                Assert.Equal(ClaimOrigin.Inheritance, claim.Origin);
                Assert.Equal($"{claim.TitleId}:{claim.ClaimantHeroId}:blood", claim.Id);
            });

        AssertWeakClaim(claims, "clan_de_la_pole", "duchy_york");
        AssertWeakClaim(claims, "clan_holland", "duchy_york");
        AssertWeakClaim(claims, "clan_tudor", "duchy_somerset");
    }

    private static string Project(IEnumerable<Claim> claims) =>
        string.Join(
            "\n",
            claims
                .OrderBy(claim => claim.TitleId)
                .ThenBy(claim => claim.ClaimantHeroId)
                .ThenBy(claim => claim.ClaimantClanId)
                .ThenBy(claim => claim.Strength)
                .Select(claim =>
                    $"{claim.TitleId}|{claim.ClaimantHeroId}|{claim.ClaimantClanId}|{claim.Strength}"));

    private static void AssertWeakClaim(IEnumerable<Claim> claims, string claimantClanId, string titleId)
    {
        Assert.Contains(
            claims,
            claim => claim.ClaimantClanId == claimantClanId
                     && claim.TitleId == titleId
                     && claim.Strength == ClaimStrength.Weak
                     && claim.Origin == ClaimOrigin.Inheritance);
    }

}
