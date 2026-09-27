using System;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Runs the declaration chain of <c>ClaimPressureCampaignBehavior.OnDailyTick</c> over the real
/// 1471 content: derive the blood claims, keep each claimant's best claim per title, drop the
/// ones the evaluator will not act on, resolve both sides through the suzerain chains, and price
/// what is left. The behaviour itself needs a live campaign, so the chain is replicated here
/// rather than invoked.
/// </summary>
/// <remarks>
/// Clan strength is the one input the content cannot supply — it comes from
/// <c>Clan.CurrentTotalStrength</c> at runtime — so every house counts as one. That makes the
/// two side totals a count of houses rather than of men, which is exactly the quantity the
/// authored hierarchy determines and the part worth pinning; it is not a claim about what the
/// scores will be in a running campaign, where a duke's levy dwarfs a baron's. The engine-only
/// filters (the player's clan, the two clans sharing a kingdom, an existing war, the
/// thirty-day cooldown) are likewise left out.
/// </remarks>
public class ClaimPressureOverRealContentTests
{
    // ClaimPressureCampaignBehavior.DeclarationThreshold, which is private to it.
    private const float DeclarationThreshold = 0.8f;

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EveryClaimTheContentDerivesIsOneTheEvaluatorWillActOn()
    {
        // Derivation and evaluation have to agree or the daily tick spends its time on claims it
        // will never press. They do, on all 399: derivation bars the holder himself and nobody
        // else, and that is the same bar evaluation applies.
        Content content = Content.Load();

        Assert.Equal(399, content.Claims().Count);
        Assert.All(content.Claims(), claim => Assert.True(content.IsActionable(claim)));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AClaimantsClaimsLapseTheMomentSheBecomesTheHolder()
    {
        // The other side of the same rule, over the one succession in the content that moves five
        // dignities at once. Warwick's daughter holds a claim on each of them while her father
        // lives; when they pass to her the evaluator rejects all five, because a claimant cannot
        // press a claim against herself.
        const string warwick = "dadg_lord_9_1";
        const string daughter = "dadg_lord_2_4";

        Content content = Content.Load();
        Claim[] hers = content.Claims().Where(claim => claim.ClaimantHeroId == daughter).ToArray();

        Assert.Equal(5, hers.Length);
        Assert.All(hers, claim => Assert.True(content.IsActionable(claim)));

        Content afterDeath = content.WithDeceased(warwick);
        afterDeath.ExecuteSuccession(warwick);

        Assert.All(hers, claim => Assert.Equal(daughter, afterDeath.Holder(claim.TitleId)));
        Assert.All(hers, claim => Assert.False(afterDeath.IsActionable(claim)));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AWarGathersEachSidesAffinityAndLeavesEverybodyElseOut()
    {
        // Sides are resolved by walking each house up to the first belligerent it meets, so what
        // a war costs depends on where in the hierarchy it is fought. Against the crown the whole
        // realm is on the board — Warwick alone against the other fifty-two, or York with the
        // eleven houses of his affinity against the crown's forty-two. Two northern houses
        // fighting each other put eight on the board and leave the other forty-five out of it.
        Content content = Content.Load();

        Assert.Equal((1f, 52f), content.Sides("clan_neville_of_middleham", "clan_lancaster"));
        Assert.Equal((11f, 42f), content.Sides("clan_york", "clan_lancaster"));

        Assert.Equal((7f, 1f), content.Sides("clan_percy", "clan_lumley"));
        Assert.Equal((1f, 7f), content.Sides("clan_lumley", "clan_percy"));

        // Nearest ancestor wins, so a liege who attacks his own vassal loses that vassal's whole
        // branch: York musters eleven against the crown but only six against Talbot, because the
        // five houses that answer to Talbot answer to York only through him.
        Assert.Equal((6f, 5f), content.Sides("clan_york", "clan_talbot"));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void NoHouseHoldsAClaimOnACrownDignitySoTheCrownIsNeverTheDefender()
    {
        // The system models baronial feuds, not usurpation, and the content is authored to match:
        // each of the eight dignities Lancaster holds carries exactly one claim, and it is
        // Lancaster's own. So no inter-clan pair in the realm has the crown on the defending side;
        // the eight are contested from inside the house instead, by a pretender. Lancaster still
        // appears as an attacker.
        Content content = Content.Load();
        string[] crownTitles = content.TitlesHeldBy("clan_lancaster").ToArray();

        Assert.Equal(8, crownTitles.Length);
        Assert.All(
            crownTitles,
            titleId =>
            {
                Claim claim = Assert.Single(content.Claims().Where(c => c.TitleId == titleId));
                Assert.Equal("clan_lancaster", claim.ClaimantClanId);
            });

        Assert.DoesNotContain(content.Opportunities(), o => o.DefenderClanId == "clan_lancaster");
        Assert.Contains(content.Opportunities(), o => o.AttackerClanId == "clan_lancaster");
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void NoClaimIsWorthPressingWhileTheRealmIsAtPeaceWithItself()
    {
        // Ninety-four claimant-title pairs survive the filters; seventy-two of them are priced at
        // nothing because the claimant cannot field 1.25 times the defender, and the twenty-two
        // that do score top out at 0.6625 against a threshold of 0.8. So on a quiet day nobody
        // declares, which is the intended shape: ambition alone does not start a war.
        //
        // What starts one is opportunity or bad blood. Fourteen pairs cross the moment the holder
        // is committed elsewhere, the same fourteen cross on maximum hatred alone, and together
        // the two tip every pair that scores at all. Nothing tips on friendship: relation can only
        // ever move a score by 0.2 either way.
        Content content = Content.Load();
        IReadOnlyList<ClaimOpportunity> quiet = content.Opportunities();

        Assert.Equal(94, quiet.Count);
        Assert.Equal(3, quiet.Count(o => o.Strength == ClaimStrength.Strong));
        Assert.Equal(91, quiet.Count(o => o.Strength == ClaimStrength.Weak));

        Assert.Equal(22, quiet.Count(o => content.Score(o) > 0f));
        Assert.Equal(0, quiet.Count(o => content.Score(o) >= DeclarationThreshold));
        Assert.Equal(0.6625f, quiet.Max(content.Score), 0.0001f);

        Assert.Equal(14, quiet.Count(o => content.Score(o with { DefenderDistracted = true }) >= DeclarationThreshold));
        Assert.Equal(14, quiet.Count(o => content.Score(o with { Relation = -100f }) >= DeclarationThreshold));
        Assert.Equal(
            22,
            quiet.Count(o =>
                content.Score(o with { DefenderDistracted = true, Relation = -100f }) >= DeclarationThreshold));
        Assert.Equal(0, quiet.Count(o => content.Score(o with { Relation = 100f }) >= DeclarationThreshold));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void MoreMenStopBuyingAppetiteLongBeforeTheRealmsWorstMismatch()
    {
        // The ratio is capped at 2.25:1, and the real hierarchy produces mismatches far past it.
        // The crown could bring fifty-two houses against Warwick's one and would price that claim
        // at exactly what Mowbray prices three-against-one at, because both saturate the cap and
        // both claims are weak. How good the claim is decides the rest: Stafford's strong claim on
        // Cheshire outscores both on a bare three-to-two advantage.
        Content content = Content.Load();

        ClaimOpportunity crownAgainstWarwick = content.Opportunity("barony_middleham", "clan_lancaster");
        ClaimOpportunity mowbrayAgainstBourchier = content.Opportunity("county_essex", "clan_mowbray");
        ClaimOpportunity staffordAgainstWoodville = content.Opportunity("county_cheshire", "clan_stafford");

        Assert.Equal((52f, 1f), (crownAgainstWarwick.AttackerStrength, crownAgainstWarwick.DefenderStrength));
        Assert.Equal((3f, 1f), (mowbrayAgainstBourchier.AttackerStrength, mowbrayAgainstBourchier.DefenderStrength));
        Assert.Equal((3f, 2f), (staffordAgainstWoodville.AttackerStrength, staffordAgainstWoodville.DefenderStrength));

        Assert.Equal(ClaimStrength.Weak, crownAgainstWarwick.Strength);
        Assert.Equal(ClaimStrength.Weak, mowbrayAgainstBourchier.Strength);
        Assert.Equal(ClaimStrength.Strong, staffordAgainstWoodville.Strength);

        Assert.Equal(0.6f, content.Score(crownAgainstWarwick), 0.0001f);
        Assert.Equal(0.6f, content.Score(mowbrayAgainstBourchier), 0.0001f);
        Assert.Equal(0.6625f, content.Score(staffordAgainstWoodville), 0.0001f);

        // Below the cap the ratio still counts, and below the floor nothing counts at all.
        // Clifford is twice Courtenay's size and so prices his weak claim under both of those.
        // Fitzalan holds a weak claim on the same Cheshire, but musters exactly what Woodville
        // musters: an even match buys nothing, because the floor asks for a 1.25-to-1 advantage
        // and parity is not one. He still scores nothing with the holder distracted and the two
        // houses at maximum hatred — the floor is a gate, not a term the bonuses can outweigh.
        Assert.Equal(0.5375f, content.Score(content.Opportunity("county_cornwall", "clan_clifford")), 0.0001f);

        ClaimOpportunity fitzalanAgainstWoodville = content.Opportunity("county_cheshire", "clan_fitzalan");
        Assert.Equal((2f, 2f), (fitzalanAgainstWoodville.AttackerStrength, fitzalanAgainstWoodville.DefenderStrength));
        Assert.Equal(0f, content.Score(fitzalanAgainstWoodville));
        Assert.Equal(
            0f,
            content.Score(fitzalanAgainstWoodville with { DefenderDistracted = true, Relation = -100f }));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EveryClaimAHouseHoldsOnItsOwnDignityHasAPretenderToPressIt()
    {
        // The spinoff is what makes a claim on one's own house actionable, and it needs a living
        // member of that house who is not the holder to found the branch with. All eighty-six of
        // them have one, across forty-seven houses, so nothing the content authors goes unpressed
        // for want of a claimant — the eight crown dignities the inter-clan pass has to leave out
        // among them.
        Content content = Content.Load();
        IReadOnlyList<Claim> sameClan = content.SameClanBestClaims();
        IReadOnlyList<ClaimOpportunity> internals = content.InternalOpportunities();

        Assert.Equal(86, sameClan.Count);
        Assert.All(sameClan, claim => Assert.NotNull(content.Pretender(claim)));

        Assert.Equal(86, internals.Count);
        Assert.Equal(47, internals.Select(o => o.DefenderClanId).Distinct().Count());
        Assert.Equal(
            content.TitlesHeldBy("clan_lancaster"),
            internals.Where(o => o.DefenderClanId == "clan_lancaster").Select(o => o.TitleId).OrderBy(id => id));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void APretenderMustersNothingFromTheHouseHeIsLeaving()
    {
        // A branch that does not exist yet has no vassals, so the chain walk hands the pretender
        // nothing while it hands the holder the whole house and everything sworn under it. Not one
        // of the eighty-six prices above zero, which is the intended shape: a war inside a house is
        // fought with the retinue its claimant rides out with and the friends he can talk round,
        // never on the strength of the house he is leaving.
        Content content = Content.Load();
        IReadOnlyList<ClaimOpportunity> internals = content.InternalOpportunities();

        Assert.All(internals, o => Assert.Equal(0f, o.AttackerStrength));
        Assert.All(internals, o => Assert.True(o.DefenderStrength >= 1f));
        Assert.All(internals, o => Assert.Equal(0f, content.Score(o)));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AWeakClaimInsideAHouseTurnsTheWholeRealmAgainstThePretender()
    {
        // What the calls to arms are worth when nobody has a grievance to trade on: relation is
        // runtime state, so every house here is indifferent and answers on the claim and the odds
        // alone. Against odds of nothing to one those odds alone would send the uncommitted houses
        // to the holder — except that a strong claim's legitimacy is worth just enough to keep them
        // home. So the seventy-seven strong claims move nobody, and the nine weak ones put all
        // fifty-three houses of England behind the holder.
        //
        // Nobody ever answers the pretender, which is the same finding from the other side: with
        // no grievance in play, a claim inside a house draws men only against it.
        Content content = Content.Load();
        ClaimOpportunity[] internals = content.InternalOpportunities().ToArray();
        ClaimOpportunity[] strong = internals.Where(o => o.Strength == ClaimStrength.Strong).ToArray();
        ClaimOpportunity[] weak = internals.Where(o => o.Strength == ClaimStrength.Weak).ToArray();

        Assert.Equal(77, strong.Length);
        Assert.Equal(9, weak.Length);

        Assert.All(internals, o => Assert.Empty(content.Solicit(o).AttackerSupporters));

        Assert.All(strong, o => Assert.Empty(content.Solicit(o).DefenderSupporters));
        Assert.All(
            strong,
            o => Assert.Equal((o.AttackerStrength, o.DefenderStrength), content.SidesAfter(o, content.Solicit(o))));

        Assert.All(weak, o => Assert.NotEmpty(content.Solicit(o).DefenderSupporters));
        Assert.All(weak, o => Assert.Equal((0f, 53f), content.SidesAfter(o, content.Solicit(o))));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void OnlyARoutDrawsAnAnswerWhileNoHouseHasAGrievance()
    {
        // The same indifferent realm asked about the ninety-four inter-clan pairs. Only claimants
        // with no affinity at all draw an answer, and what they draw is the whole uncommitted realm
        // onto the holder: hopeless odds are the one thing that moves a house with no grievance to
        // trade on.
        //
        // The crown's claims against Warwick draw nobody, though fifty-one houses would fight for
        // the crown if it came to it. They are already mustering for it down the chain, so there is
        // no pledge for them to make: only a house that ends up somewhere the hierarchy would not
        // have put it is named, which is what keeps fifty-one needless ids out of the war.
        Content content = Content.Load();
        var answered = content.Opportunities()
            .Select(opportunity => (Opportunity: opportunity, Support: content.Solicit(opportunity)))
            .Where(pair => pair.Support.AttackerSupporters.Count > 0 || pair.Support.DefenderSupporters.Count > 0)
            .ToArray();

        Assert.Equal(5, answered.Length);
        Assert.All(answered, pair => Assert.Equal(0f, pair.Opportunity.AttackerStrength));
        Assert.All(answered, pair => Assert.Empty(pair.Support.AttackerSupporters));

        ClaimOpportunity middleham = content.Opportunity("barony_middleham", "clan_lancaster");
        SupportDecision crownsCall = content.Solicit(middleham);

        Assert.Empty(crownsCall.AttackerSupporters);
        Assert.Empty(crownsCall.DefenderSupporters);
        Assert.Equal(
            51,
            content.Candidates(middleham).Count(clanId => content.SideOf(middleham, clanId) == "clan_lancaster"));
        Assert.Equal((52f, 1f), content.SidesAfter(middleham, crownsCall));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void NoForeignHouseIsKinToAnEnglishLeaderSoOnlyFriendshipCallsOneAcrossTheBorder()
    {
        // The content authors three realms, and not one clan leader in any of them is kin to a
        // leader in another, by blood or by marriage. English marriages do bind English leaders,
        // York to Woodville through York's Woodville wife, but every one stays inside the realm. Kin
        // never carries a call to arms across a border at the start of a campaign, so with no
        // relations in play every English war asks English houses only, as it did before bonds
        // existed. A foreign house joins an English feud only once its leader has made a friend of
        // a principal, which is runtime state.
        Content content = Content.Load();
        ILookup<string, string> realms = content.Realms();

        Assert.Equal(
            new[] { ("clan_lancaster", 53), ("clan_macdonald_isles", 13), ("clan_stewart", 13) },
            realms.Select(realm => (realm.Key, realm.Count())).OrderBy(realm => realm.Key));

        Assert.True(content.IsBonded("clan_york", "clan_woodville", 0f));

        Assert.All(
            realms,
            realm => Assert.All(
                realm,
                clanId => Assert.All(
                    realms.Where(other => other.Key != realm.Key).SelectMany(other => other),
                    foreignClanId => Assert.False(content.IsBonded(clanId, foreignClanId, 0f)))));

        string[] english = realms["clan_lancaster"].ToArray();
        Assert.All(
            content.Opportunities().Where(o => english.Contains(o.DefenderClanId)),
            o => Assert.All(content.Candidates(o), clanId => Assert.Contains(clanId, english)));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AnEnglishLadyWhoWedsAScottishLordCallsHisHouseToHerFathersFeuds()
    {
        // Courtenay's eldest daughter marries the lord of Maxwell. She joins his house, which
        // makes Courtenay his father-in-law: Maxwell is now asked each time Clifford presses
        // a claim on a Courtenay seat, though with no relation in play he answers neither side.
        // Kinship gets him asked; it does not make him fight. He is still not asked into
        // English feuds that Courtenay has no part in.
        //
        // Her blood claims go with her, so Maxwell now holds her three claims on Courtenay's
        // seats. They cross the border, and ClaimPressureCampaignBehavior drops any pair whose
        // clans do not share a kingdom, so none is ever pressed. Pressing one would be the
        // cross-border claim, which has no design yet.
        const string maxwell = "clan_maxwell";
        const string courtenay = "clan_courtenay";
        string[] courtenaySeats = { "barony_okehampton", "barony_saint_michaels_mount", "county_cornwall" };

        Content content = Content.Load();
        Content married = content.WithMarriage("dadg_sco_lord_clan_maxwell", "dadg_lord_16_2");

        Assert.False(content.IsBonded(maxwell, courtenay, 0f));
        Assert.True(married.IsBonded(maxwell, courtenay, 0f));

        ClaimOpportunity[] cliffordsFeuds = married.Opportunities()
            .Where(o => o.AttackerClanId == "clan_clifford" && o.DefenderClanId == courtenay)
            .ToArray();
        Assert.Equal(courtenaySeats, cliffordsFeuds.Select(o => o.TitleId).OrderBy(id => id));
        Assert.All(cliffordsFeuds, o =>
        {
            Assert.DoesNotContain(maxwell, content.Candidates(content.Opportunity(o.TitleId, "clan_clifford")));
            Assert.Contains(maxwell, married.Candidates(o));

            SupportDecision support = married.Solicit(o);
            Assert.DoesNotContain(maxwell, support.AttackerSupporters);
            Assert.DoesNotContain(maxwell, support.DefenderSupporters);
        });
        Assert.DoesNotContain(maxwell, married.Candidates(married.Opportunity("county_cheshire", "clan_stafford")));

        Assert.Empty(content.Claims().Where(c => c.ClaimantClanId == maxwell));
        Assert.Equal(
            courtenaySeats,
            married.Claims().Where(c => c.ClaimantClanId == maxwell).Select(c => c.TitleId).OrderBy(id => id));
        Assert.Contains(maxwell, married.Realms()["clan_stewart"]);
        Assert.Contains(courtenay, married.Realms()["clan_lancaster"]);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AScottishLadyWhoWedsAnEnglishLordCallsHerFathersHouseToHisFeuds()
    {
        // The other way round: the lord of Percy marries a daughter of the lord of Maxwell. The
        // Scottish houses do not author their families yet, so she is a stand-in, as a daughter
        // born in play would be. She joins Percy's house, which makes Maxwell his father-in-law:
        // Maxwell is now asked each time De Vere presses a claim on a Percy seat, and again, with
        // no relation in play, he answers neither side.
        //
        // Her blood claims go with her, as the English bride's do: she is heir to every seat her
        // father holds, so Percy now has a claim on each of Maxwell's Scottish dignities. Those
        // cross the border and are dropped by the same shared-kingdom check, so the pairs
        // ClaimPressureCampaignBehavior would actually price are De Vere's two, as before.
        const string maxwell = "clan_maxwell";
        const string percy = "clan_percy";
        const string daughter = "stand_in_maxwell_daughter";

        Content content = Content.Load();
        Content married = content
            .WithDaughter("dadg_sco_lord_clan_maxwell", daughter, 18f)
            .WithMarriage("dadg_lord_10_1", daughter);

        Assert.False(content.IsBonded(maxwell, percy, 0f));
        Assert.True(married.IsBonded(maxwell, percy, 0f));

        Assert.Empty(content.Opportunities().Where(o => o.AttackerClanId == percy));
        Assert.Equal(
            content.TitlesHeldBy(maxwell),
            married.Opportunities()
                .Where(o => o.AttackerClanId == percy)
                .Select(o =>
                {
                    Assert.Equal(maxwell, o.DefenderClanId);
                    return o.TitleId;
                })
                .OrderBy(id => id));
        Assert.Contains(percy, married.Realms()["clan_lancaster"]);
        Assert.Contains(maxwell, married.Realms()["clan_stewart"]);

        ClaimOpportunity[] percysFeuds = married.Opportunities()
            .Where(o => o.DefenderClanId == percy)
            .ToArray();
        Assert.Equal(
            new[] { ("clan_de_vere", "county_warkworth"), ("clan_de_vere", "duchy_northumberland") },
            percysFeuds.Select(o => (o.AttackerClanId, o.TitleId)).OrderBy(pair => pair.TitleId));
        Assert.All(percysFeuds, o =>
        {
            Assert.DoesNotContain(maxwell, content.Candidates(content.Opportunity(o.TitleId, o.AttackerClanId)));
            Assert.Contains(maxwell, married.Candidates(o));

            SupportDecision support = married.Solicit(o);
            Assert.DoesNotContain(maxwell, support.AttackerSupporters);
            Assert.DoesNotContain(maxwell, support.DefenderSupporters);
        });

        Assert.Empty(married.Claims().Where(c => c.ClaimantClanId == maxwell));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AScottishFriendOfTheClaimantRidesToAnEnglishFeud()
    {
        // Stafford presses his strong claim on Cheshire against Woodville, three houses against
        // two. Maxwell is a Border house under the Stewart crown and has no part in it, until
        // his leader counts Stafford's leader a firm friend: then he is asked, he answers for the
        // claimant, and the claimant rides with four. At a relation of exactly 50, one short of
        // friendship, he is not even asked, because a house of another crown is nobody's to call on.
        const string maxwell = "clan_maxwell";

        Content content = Content.Load();
        ClaimOpportunity cheshire = content.Opportunity("county_cheshire", "clan_stafford");

        Assert.DoesNotContain(maxwell, content.Candidates(cheshire));
        Assert.DoesNotContain(maxwell, content.Candidates(cheshire, Befriended(maxwell, 50f)));

        SupportDecision support = content.Solicit(cheshire, Befriended(maxwell, 51f));

        Assert.Equal(new[] { maxwell }, support.AttackerSupporters);
        Assert.DoesNotContain(maxwell, support.DefenderSupporters);
        Assert.Equal((4f, 2f), content.SidesAfter(cheshire, support));
    }

    private static Relations Befriended(string clanId, float toClaimant) =>
        candidate => candidate == clanId ? (toClaimant, 0f) : (0f, 0f);

    /// <summary>
    /// The real content wired through the production adapters, with the campaign behaviour's
    /// candidate selection replicated over it.
    /// </summary>
    private delegate (float ToClaimant, float ToHolder) Relations(string clanId);

    private sealed class Content
    {
        private static readonly Relations NoRelations = _ => (0f, 0f);

        private static readonly string[] None = Array.Empty<string>();

        // ClaimPressureCampaignBehavior.UnfoundedCadetBranch, which is private to it.
        private const string UnfoundedCadetBranch = "";

        private readonly DadgXmlGenealogy _genealogy;
        private readonly XmlFeudalStructure _structure;
        private readonly InMemoryTitleRegistry _titles;
        private readonly InMemoryClaimRegistry _claimRegistry = new();
        private readonly IReadOnlyList<Claim> _claims;
        private readonly GetSuzerainUseCase _suzerain;
        private readonly EvaluateClaimUseCase _evaluateClaim;
        private readonly EvaluatePressClaimUseCase _evaluatePressClaim = new();
        private readonly SolicitSupportUseCase _solicitSupport = new();
        private readonly PersonalBondPolicy _personalBond;

        private Content(DadgXmlGenealogy genealogy, XmlFeudalStructure structure, IReadOnlyList<Title> initialTitles)
        {
            _genealogy = genealogy;
            _structure = structure;
            _personalBond = new PersonalBondPolicy(genealogy);
            _titles = new InMemoryTitleRegistry(genealogy);
            _titles.Initialise(initialTitles);
            _suzerain = new GetSuzerainUseCase(_titles, structure, genealogy);
            _evaluateClaim = new EvaluateClaimUseCase(_titles, genealogy);
            _claims = new GenerateBloodClaimsUseCase(_titles, _claimRegistry, genealogy).Execute();
        }

        public static Content Load()
        {
            using Stream titlesStream = DadgContent.OpenTitles();
            using Stream heroesStream = DadgContent.OpenHeroes();
            using Stream charactersStream = DadgContent.OpenCharacters();
            using Stream clansStream = DadgContent.OpenClans();

            DadgXmlGenealogy genealogy = DadgXmlGenealogy.Load(heroesStream, charactersStream, clansStream);
            var structure = new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream));

            return new Content(genealogy, structure, structure.BuildInitialTitles(genealogy));
        }

        public Content WithDeceased(string heroId) =>
            new(_genealogy.WithDeceased(heroId), _structure, _titles.GetAllTitles());

        public Content WithMarriage(string husbandId, string wifeId) =>
            new(_genealogy.WithMarriage(husbandId, wifeId), _structure, _titles.GetAllTitles());

        public Content WithDaughter(string fatherId, string daughterId, float age) =>
            new(_genealogy.WithDaughter(fatherId, daughterId, age), _structure, _titles.GetAllTitles());

        public IReadOnlyList<Claim> Claims() => _claims;

        public bool IsActionable(Claim claim) => _evaluateClaim.Execute(claim);

        public string? Holder(string titleId) => _titles.GetTitle(titleId)?.HolderHeroId;

        public IReadOnlyList<SuccessionResult> ExecuteSuccession(string deceasedHeroId) =>
            new ExecuteSuccessionUseCase(_titles, _genealogy, new SilentLoggerFactory()).Execute(deceasedHeroId);

        public IOrderedEnumerable<string> TitlesHeldBy(string clanId) =>
            _titles.GetAllTitles()
                .Where(title => _genealogy.GetHolderClanOf(title) == clanId)
                .Select(title => title.Id)
                .OrderBy(id => id);

        public (float Attacker, float Defender) Sides(string attackerClanId, string defenderClanId) =>
            WarSideStrength.Sum(UniformStrength(), Suzerain, attackerClanId, defenderClanId, None, None);

        public float Score(ClaimOpportunity opportunity) => _evaluatePressClaim.Execute(opportunity);

        public ClaimOpportunity Opportunity(string titleId, string attackerClanId) =>
            Opportunities().Single(o => o.TitleId == titleId && o.AttackerClanId == attackerClanId);

        /// <summary>
        /// The candidates OnDailyTick would price: every title with a holder, each claimant clan's
        /// best claim on it, minus the claimant clan that already holds it.
        /// </summary>
        public IReadOnlyList<ClaimOpportunity> Opportunities()
        {
            IReadOnlyDictionary<string, float> strengths = UniformStrength();
            var opportunities = new List<ClaimOpportunity>();

            foreach (Title title in _titles.GetAllTitles())
            {
                if (title.HolderHeroId is null) continue;
                if (_genealogy.GetClanOf(title.HolderHeroId) is not { } defenderClanId) continue;

                foreach (Claim claim in BestClaimPerClaimant(title.Id))
                {
                    if (claim.ClaimantClanId == defenderClanId) continue;

                    var (attacker, defender) =
                        WarSideStrength.Sum(strengths, Suzerain, claim.ClaimantClanId, defenderClanId, None, None);
                    opportunities.Add(new ClaimOpportunity(
                        title.Id, claim.ClaimantClanId, defenderClanId, claim.Strength,
                        attacker, defender, false, 0f));
                }
            }

            return opportunities;
        }

        /// <summary>
        /// The hero who would leave his house to press its own dignity: a living member of the
        /// holding clan who is not the holder himself. Without one there is nobody to found a
        /// cadet branch with, and the claim cannot be pressed at all.
        /// </summary>
        public string? Pretender(Claim claim) =>
            claim.ClaimantHeroId is { } heroId
            && heroId != Holder(claim.TitleId)
            && _genealogy.GetHero(heroId) is { IsAlive: true }
            && _genealogy.GetClanOf(heroId) == claim.ClaimantClanId
                ? heroId
                : null;

        /// <summary>
        /// The pairs the spinoff adds, priced as the behaviour prices them: against the empty
        /// clan id, so the pretender's side holds whoever pledged to him and nothing else. The
        /// retinue he leaves with is left out for the same reason clan strength is - the content
        /// cannot supply it.
        /// </summary>
        public IReadOnlyList<ClaimOpportunity> InternalOpportunities()
        {
            IReadOnlyDictionary<string, float> strengths = UniformStrength();
            var opportunities = new List<ClaimOpportunity>();

            foreach (Title title in _titles.GetAllTitles())
            {
                if (title.HolderHeroId is null) continue;
                if (_genealogy.GetClanOf(title.HolderHeroId) is not { } defenderClanId) continue;

                foreach (Claim claim in BestClaimPerClaimant(title.Id))
                {
                    if (claim.ClaimantClanId != defenderClanId || Pretender(claim) is null) continue;

                    var (attacker, defender) =
                        WarSideStrength.Sum(strengths, Suzerain, UnfoundedCadetBranch, defenderClanId, None, None);
                    opportunities.Add(new ClaimOpportunity(
                        title.Id, claim.ClaimantClanId, defenderClanId, claim.Strength,
                        attacker, defender, false, 0f));
                }
            }

            return opportunities;
        }

        /// <summary>
        /// Both calls to arms, sent round the realm on the hierarchy-only pricing exactly as the
        /// behaviour sends them. Relation is runtime state, so it is nought unless a test supplies
        /// it: with none, what answers here is what the claim and the odds alone are worth.
        /// </summary>
        public SupportDecision Solicit(ClaimOpportunity opportunity, Relations? relations = null)
        {
            relations ??= NoRelations;

            return _solicitSupport.Execute(
                opportunity,
                Candidates(opportunity, relations)
                    .Select(clanId => new SupportCandidate(
                        clanId,
                        Allegiance(opportunity, clanId),
                        relations(clanId).ToClaimant,
                        relations(clanId).ToHolder))
                    .ToList());
        }

        /// <summary>The two totals re-summed once the answers are in.</summary>
        public (float Attacker, float Defender) SidesAfter(ClaimOpportunity opportunity, SupportDecision support) =>
            WarSideStrength.Sum(
                UniformStrength(), Suzerain, AttackerSideId(opportunity), opportunity.DefenderClanId,
                support.AttackerSupporters, support.DefenderSupporters);

        // A war inside one house is fought by a branch that does not exist yet.
        private static string AttackerSideId(ClaimOpportunity opportunity) =>
            opportunity.AttackerClanId == opportunity.DefenderClanId
                ? UnfoundedCadetBranch
                : opportunity.AttackerClanId;

        /// <summary>Every claim a house holds on one of its own dignities, best claim per claimant.</summary>
        public IReadOnlyList<Claim> SameClanBestClaims()
        {
            var claims = new List<Claim>();
            foreach (Title title in _titles.GetAllTitles())
            {
                if (title.HolderHeroId is null) continue;
                if (_genealogy.GetClanOf(title.HolderHeroId) is not { } defenderClanId) continue;
                claims.AddRange(BestClaimPerClaimant(title.Id).Where(c => c.ClaimantClanId == defenderClanId));
            }

            return claims;
        }

        /// <summary>The belligerent a house musters for on the hierarchy alone, or null for neither.</summary>
        public string? SideOf(ClaimOpportunity opportunity, string clanId) =>
            WarSideStrength.ResolveSide(
                clanId, Suzerain, AttackerSideId(opportunity), opportunity.DefenderClanId, None, None);

        /// <summary>
        /// Every house the calls to arms go out to, both principals excepted: a house the chains
        /// already commit is always asked, an uncommitted one only if it belongs to the defender's
        /// realm or its leader has a personal bond with a principal's leader. The content has
        /// several realm roots, and a house of another crown is nobody's to call on in a war it
        /// has no part in unless it is kin or a friend.
        /// </summary>
        /// <remarks>
        /// The behaviour tests the bond against the pretender when there is one; the content
        /// opportunities do not carry him, so the claimant clan's leader stands in for him.
        /// </remarks>
        public IEnumerable<string> Candidates(ClaimOpportunity opportunity, Relations? relations = null)
        {
            relations ??= NoRelations;

            return UniformStrength().Keys
                .Where(clanId => clanId != opportunity.AttackerClanId && clanId != opportunity.DefenderClanId)
                .Where(clanId =>
                    SideOf(opportunity, clanId) is not null
                    || Realm(clanId) == Realm(opportunity.DefenderClanId)
                    || IsBonded(clanId, opportunity.AttackerClanId, relations(clanId).ToClaimant)
                    || IsBonded(clanId, opportunity.DefenderClanId, relations(clanId).ToHolder));
        }

        /// <summary>Whether two houses' leaders are bonded, as the behaviour asks it of each candidate.</summary>
        public bool IsBonded(string clanId, string principalClanId, float relation) =>
            _genealogy.GetClanLeaderId(clanId) is { } leaderId
            && _genealogy.GetClanLeaderId(principalClanId) is { } principalId
            && _personalBond.IsBonded(leaderId, principalId, relation);

        /// <summary>Every house of the content, grouped under the root of its suzerain chain.</summary>
        public ILookup<string, string> Realms() => UniformStrength().Keys.ToLookup(Realm);

        /// <summary>The house at the top of a house's suzerain chain.</summary>
        private string Realm(string clanId)
        {
            string current = clanId;
            while (Suzerain(current) is { } liege) current = liege;
            return current;
        }

        private FeudalAllegiance Allegiance(ClaimOpportunity opportunity, string clanId)
        {
            string? side = SideOf(opportunity, clanId);
            if (side == AttackerSideId(opportunity)) return FeudalAllegiance.Claimant;

            return side == opportunity.DefenderClanId ? FeudalAllegiance.Holder : FeudalAllegiance.Uncommitted;
        }

        private IEnumerable<Claim> BestClaimPerClaimant(string titleId) =>
            _claimRegistry
                .GetClaimsOn(titleId)
                .GroupBy(claim => claim.ClaimantClanId)
                .Select(claims => claims.OrderByDescending(claim => claim.Strength).First());

        private string? Suzerain(string clanId) => _suzerain.Execute(clanId);

        private IReadOnlyDictionary<string, float> UniformStrength() =>
            _titles.GetAllTitles()
                .Select(title => _genealogy.GetHolderClanOf(title))
                .Where(clanId => clanId is not null)
                .Select(clanId => clanId!)
                .Distinct()
                .ToDictionary(clanId => clanId, _ => 1f);
    }
}
