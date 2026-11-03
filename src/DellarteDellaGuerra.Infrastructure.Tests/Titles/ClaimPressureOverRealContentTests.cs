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
        // will never press. They do, on all 483: derivation bars the holder himself and nobody
        // else, and that is the same bar evaluation applies.
        Content content = Content.Load();

        Assert.Equal(483, content.Claims().Count);
        Assert.All(content.Claims(), claim => Assert.True(content.IsActionable(claim)));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AClaimantsClaimsLapseTheMomentSheBecomesTheHolder()
    {
        // The other side of the same rule, over the one succession in the content that moves eight
        // dignities at once. Warwick's daughter holds a claim on each of them while her father
        // lives; when they pass to her the evaluator rejects all eight, because a claimant cannot
        // press a claim against herself.
        const string warwick = "dadg_lord_9_1";
        const string daughter = "dadg_lord_2_4";

        Content content = Content.Load();
        Claim[] hers = content.Claims().Where(claim => claim.ClaimantHeroId == daughter).ToArray();

        Assert.Equal(8, hers.Length);
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
        // The corrected dignities and allegiances give Warwick three following houses and York
        // one. Lovell follows Warwick through Northampton; Talbot answers independently to the
        // Crown, so York cannot muster his following.
        Content content = Content.Load();

        Assert.Equal((4f, 43f), content.Sides("clan_neville_of_middleham", "clan_lancaster"));
        Assert.Equal((2f, 45f), content.Sides("clan_york", "clan_lancaster"));

        Assert.Equal((2f, 4f), content.Sides("clan_percy", "clan_neville_of_middleham"));

        Assert.Equal((2f, 3f), content.Sides("clan_york", "clan_talbot"));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void NoHouseHoldsAClaimOnACrownDignitySoTheCrownIsNeverTheDefender()
    {
        // This harness derives blood claims; it does not seed the separately authored claims,
        // such as Lovell's claim on Crown-held Wardour. Each of Lancaster's thirteen dignities
        // carries exactly one blood claim, Lancaster's own. So no derived inter-clan pair has
        // the Crown defending; all thirteen can instead be contested by a pretender inside the
        // house. Lancaster still attacks once its weak claims on other holders become actionable.
        Content content = Content.Load();
        string[] crownTitles = content.TitlesHeldBy("clan_lancaster").ToArray();

        Assert.Equal(13, crownTitles.Length);
        Assert.All(
            crownTitles,
            titleId =>
            {
                Claim claim = Assert.Single(content.Claims().Where(c => c.TitleId == titleId));
                Assert.Equal("clan_lancaster", claim.ClaimantClanId);
            });

        Assert.DoesNotContain(content.Opportunities(), o => o.DefenderClanId == "clan_lancaster");
        Assert.DoesNotContain(content.Opportunities(), o => o.AttackerClanId == "clan_lancaster");

        Content minors = content.WithMinorHolders();
        Assert.DoesNotContain(minors.Opportunities(), o => o.DefenderClanId == "clan_lancaster");
        Assert.Contains(minors.Opportunities(), o => o.AttackerClanId == "clan_lancaster");
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void NoClaimIsWorthPressingWhileTheRealmIsAtPeaceWithItself()
    {
        // Adult holders leave only the three strong inter-house claims actionable. Making the
        // holders minors exposes 117 opportunities, but the corrected followings still produce
        // no declaration without distraction or hostility. Friendship never tips a claim into war.
        Content adults = Content.Load();
        Assert.Equal(3, adults.Opportunities().Count);
        Assert.All(adults.Opportunities(), o => Assert.Equal(ClaimStrength.Strong, o.Strength));

        Content content = adults.WithMinorHolders();
        IReadOnlyList<ClaimOpportunity> quiet = content.Opportunities();

        Assert.Equal(117, quiet.Count);
        Assert.Equal(3, quiet.Count(o => o.Strength == ClaimStrength.Strong));
        Assert.Equal(114, quiet.Count(o => o.Strength == ClaimStrength.Weak));

        Assert.Equal(19, quiet.Count(o => content.Score(o) > 0f));
        Assert.Equal(0, quiet.Count(o => content.Score(o) >= DeclarationThreshold));
        Assert.Equal(0.6f, quiet.Max(content.Score), 0.0001f);

        Assert.Equal(11, quiet.Count(o => content.Score(o with { DefenderDistracted = true }) >= DeclarationThreshold));
        Assert.Equal(11, quiet.Count(o => content.Score(o with { Relation = -100f }) >= DeclarationThreshold));
        Assert.Equal(
            19,
            quiet.Count(o =>
                content.Score(o with { DefenderDistracted = true, Relation = -100f }) >= DeclarationThreshold));
        Assert.Equal(0, quiet.Count(o => content.Score(o with { Relation = 100f }) >= DeclarationThreshold));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void MoreMenStopBuyingAppetiteLongBeforeTheRealmsWorstMismatch()
    {
        // The Crown's 43-to-4 advantage and Mowbray's 3-to-1 both exceed the strength cap,
        // so their weak claims score equally. Stafford's strong claim cannot overcome parity.
        // Weak claims are made actionable by giving the holders minority status.
        Content content = Content.Load().WithMinorHolders();

        ClaimOpportunity crownAgainstWarwick = content.Opportunity("barony_middleham", "clan_lancaster");
        ClaimOpportunity mowbrayAgainstBourchier = content.Opportunity("county_essex", "clan_mowbray");
        ClaimOpportunity staffordAgainstWoodville = content.Opportunity("county_cheshire", "clan_stafford");

        Assert.Equal((43f, 4f), (crownAgainstWarwick.AttackerStrength, crownAgainstWarwick.DefenderStrength));
        Assert.Equal((3f, 1f), (mowbrayAgainstBourchier.AttackerStrength, mowbrayAgainstBourchier.DefenderStrength));
        Assert.Equal((2f, 2f), (staffordAgainstWoodville.AttackerStrength, staffordAgainstWoodville.DefenderStrength));

        Assert.Equal(ClaimStrength.Weak, crownAgainstWarwick.Strength);
        Assert.Equal(ClaimStrength.Weak, mowbrayAgainstBourchier.Strength);
        Assert.Equal(ClaimStrength.Strong, staffordAgainstWoodville.Strength);

        Assert.Equal(0.6f, content.Score(crownAgainstWarwick), 0.0001f);
        Assert.Equal(0.6f, content.Score(mowbrayAgainstBourchier), 0.0001f);
        Assert.Equal(0f, content.Score(staffordAgainstWoodville));

        // Equal followings cannot pass the strength floor, even with distraction and hatred.
        ClaimOpportunity mowbrayAgainstTalbot = content.Opportunity("county_hallamshire", "clan_mowbray");
        Assert.Equal((3f, 3f), (mowbrayAgainstTalbot.AttackerStrength, mowbrayAgainstTalbot.DefenderStrength));
        Assert.Equal(0f, content.Score(mowbrayAgainstTalbot));

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
        // member of that house who is not the holder to found the branch with. All hundred and sixteen
        // of them have one, across fifty-nine houses, so nothing the content authors goes unpressed
        // for want of a claimant — the thirteen crown dignities the inter-clan pass has to leave out
        // among them. Seven of the hundred and sixteen are weak and wait for the holder to be a woman
        // or a child: against the grown men who hold them today, a hundred and nine can be pressed.
        Content adults = Content.Load();
        Content content = adults.WithMinorHolders();
        IReadOnlyList<Claim> sameClan = content.SameClanBestClaims();
        IReadOnlyList<ClaimOpportunity> internals = content.InternalOpportunities();

        Assert.Equal(116, sameClan.Count);
        Assert.All(sameClan, claim => Assert.NotNull(content.Pretender(claim)));

        Assert.Equal(109, adults.InternalOpportunities().Count);
        Assert.All(adults.InternalOpportunities(), o => Assert.Equal(ClaimStrength.Strong, o.Strength));

        Assert.Equal(116, internals.Count);
        Assert.Equal(59, internals.Select(o => o.DefenderClanId).Distinct().Count());
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
        // of the hundred and sixteen prices above zero, which is the intended shape: a war inside a house is
        // fought with the retinue its claimant rides out with and the friends he can talk round,
        // never on the strength of the house he is leaving.
        Content content = Content.Load().WithMinorHolders();
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
        // home. So the hundred and nine strong claims move nobody, and the seven weak ones put all
        // forty-seven houses of England behind the holder.
        //
        // Nobody ever answers the pretender, which is the same finding from the other side: with
        // no grievance in play, a claim inside a house draws men only against it.
        //
        // The seven weak claims can only be pressed against a woman or a child, so the holders here
        // are minors; against the grown men of the content there are none to press.
        Assert.DoesNotContain(Content.Load().InternalOpportunities(), o => o.Strength == ClaimStrength.Weak);

        Content content = Content.Load().WithMinorHolders();
        ClaimOpportunity[] internals = content.InternalOpportunities().ToArray();
        ClaimOpportunity[] strong = internals.Where(o => o.Strength == ClaimStrength.Strong).ToArray();
        ClaimOpportunity[] weak = internals.Where(o => o.Strength == ClaimStrength.Weak).ToArray();

        Assert.Equal(109, strong.Length);
        Assert.Equal(7, weak.Length);

        Assert.All(internals, o => Assert.Empty(content.Solicit(o).AttackerSupporters));

        Assert.All(strong, o => Assert.Empty(content.Solicit(o).DefenderSupporters));
        Assert.All(
            strong,
            o => Assert.Equal((o.AttackerStrength, o.DefenderStrength), content.SidesAfter(o, content.Solicit(o))));

        Assert.All(weak, o => Assert.NotEmpty(content.Solicit(o).DefenderSupporters));
        Assert.All(weak, o => Assert.Equal((0f, 47f), content.SidesAfter(o, content.Solicit(o))));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void OnlyARoutDrawsAnAnswerWhileNoHouseHasAGrievance()
    {
        // With neutral relations, only hopeless odds draw unsolicited support, always for the
        // defender. The Crown's existing following must not also appear as pledged supporters.
        // Minority makes the weak claims actionable.
        Content content = Content.Load().WithMinorHolders();
        var answered = content.Opportunities()
            .Select(opportunity => (Opportunity: opportunity, Support: content.Solicit(opportunity)))
            .Where(pair => pair.Support.AttackerSupporters.Count > 0 || pair.Support.DefenderSupporters.Count > 0)
            .ToArray();

        Assert.Equal(6, answered.Length);
        Assert.All(
            answered,
            pair => Assert.True(pair.Opportunity.AttackerStrength * 4f <= pair.Opportunity.DefenderStrength));
        Assert.All(answered, pair => Assert.Empty(pair.Support.AttackerSupporters));

        ClaimOpportunity middleham = content.Opportunity("barony_middleham", "clan_lancaster");
        SupportDecision crownsCall = content.Solicit(middleham);

        Assert.Empty(crownsCall.AttackerSupporters);
        Assert.Empty(crownsCall.DefenderSupporters);
        Assert.Equal(
            42,
            content.Candidates(middleham).Count(clanId => content.SideOf(middleham, clanId) == "clan_lancaster"));
        Assert.Equal((43f, 4f), content.SidesAfter(middleham, crownsCall));
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
            new[] { ("clan_lancaster", 47), ("clan_macdonald_isles", 12), ("clan_stewart", 14) },
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
        // Courtenay's eldest daughter marries the lord of Ruthven, who has no wife. She joins his
        // house, which makes Courtenay his father-in-law: Ruthven is now asked each time Clifford
        // presses a claim on a Courtenay seat, though with no relation in play he answers neither
        // side. Kinship gets him asked; it does not make him fight. He is still not asked into
        // English feuds that Courtenay has no part in. Clifford's claims are weak, so all of this
        // waits for Courtenay's seats to be held by a woman or a child: here the holders are minors.
        //
        // Her blood claims go with her, so Ruthven, which held none, now holds her three claims on
        // Courtenay's seats. They are weak too, so while Courtenay's grown lord holds the seats none
        // can be pressed. Against a minor they could be, and it is then the shared-kingdom check in
        // ClaimPressureCampaignBehavior, which this harness leaves out, that still drops them.
        const string ruthven = "clan_ruthven";
        const string courtenay = "clan_courtenay";
        string[] courtenaySeats = { "barony_okehampton", "barony_saint_michaels_mount", "county_devon" };

        Content content = Content.Load();
        Content married = content.WithMarriage("dadg_sco_lord_clan_ruthven", "dadg_lord_16_2");
        Content contentMinors = content.WithMinorHolders();
        Content marriedMinors = married.WithMinorHolders();

        Assert.False(content.IsBonded(ruthven, courtenay, 0f));
        Assert.True(married.IsBonded(ruthven, courtenay, 0f));

        Assert.DoesNotContain(
            married.Opportunities(), o => o.AttackerClanId == "clan_clifford" && o.DefenderClanId == courtenay);

        ClaimOpportunity[] cliffordsFeuds = marriedMinors.Opportunities()
            .Where(o => o.AttackerClanId == "clan_clifford" && o.DefenderClanId == courtenay)
            .ToArray();
        Assert.Equal(courtenaySeats, cliffordsFeuds.Select(o => o.TitleId).OrderBy(id => id));
        Assert.All(cliffordsFeuds, o =>
        {
            Assert.DoesNotContain(
                ruthven, contentMinors.Candidates(contentMinors.Opportunity(o.TitleId, "clan_clifford")));
            Assert.Contains(ruthven, marriedMinors.Candidates(o));

            SupportDecision support = marriedMinors.Solicit(o);
            Assert.DoesNotContain(ruthven, support.AttackerSupporters);
            Assert.DoesNotContain(ruthven, support.DefenderSupporters);
        });
        Assert.DoesNotContain(ruthven, married.Candidates(married.Opportunity("county_cheshire", "clan_stafford")));

        Assert.Empty(content.Claims().Where(c => c.ClaimantClanId == ruthven));
        Assert.Equal(
            courtenaySeats,
            married.Claims().Where(c => c.ClaimantClanId == ruthven).Select(c => c.TitleId).OrderBy(id => id));
        Assert.All(
            married.Claims().Where(c => c.ClaimantClanId == ruthven),
            c => Assert.Equal(ClaimStrength.Weak, c.Strength));
        Assert.DoesNotContain(married.Opportunities(), o => o.AttackerClanId == ruthven);
        Assert.Equal(
            courtenaySeats,
            marriedMinors.Opportunities().Where(o => o.AttackerClanId == ruthven).Select(o => o.TitleId).OrderBy(id => id));
        Assert.Contains(ruthven, married.Realms()["clan_stewart"]);
        Assert.Contains(courtenay, married.Realms()["clan_lancaster"]);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AScottishLadyWhoWedsAnEnglishLordCallsHerFathersHouseToHisFeuds()
    {
        // The other way round: the lord of Percy marries Katherine, the unwed daughter of the lord
        // of Kennedy. She joins Percy's house, which makes Kennedy his father-in-law: Kennedy is
        // now asked each time De Vere presses a claim on a Percy seat, and again, with no relation
        // in play, he answers neither side.
        //
        // Her blood claim goes with her, as the English bride's do: Percy now has her claim on
        // Carrick, her father's seat, and Kennedy keeps only her brother's. A daughter's claim is
        // weak, so while Kennedy's grown lord holds the seat it cannot be pressed. Against a minor
        // it could be, and would then be dropped by the same shared-kingdom check, which this
        // harness leaves out.
        //
        // De Vere's claims on Percy's seats are weak as well, so Kennedy's call to them is also
        // shown with the holders as minors.
        const string kennedy = "clan_kennedy";
        const string percy = "clan_percy";
        const string bride = "dadg_sco_family_kennedy_katherine";

        Content content = Content.Load();
        Content married = content.WithMarriage("dadg_lord_10_1", bride);
        Content contentMinors = content.WithMinorHolders();
        Content marriedMinors = married.WithMinorHolders();

        Assert.False(content.IsBonded(kennedy, percy, 0f));
        Assert.True(married.IsBonded(kennedy, percy, 0f));

        Assert.Empty(contentMinors.Opportunities().Where(o => o.AttackerClanId == percy));
        Assert.Empty(married.Opportunities().Where(o => o.AttackerClanId == percy));
        Assert.Equal(
            content.TitlesHeldBy(kennedy),
            marriedMinors.Opportunities()
                .Where(o => o.AttackerClanId == percy)
                .Select(o =>
                {
                    Assert.Equal(kennedy, o.DefenderClanId);
                    return o.TitleId;
                })
                .OrderBy(id => id));
        Assert.Contains(percy, married.Realms()["clan_lancaster"]);
        Assert.Contains(kennedy, married.Realms()["clan_stewart"]);

        Assert.Empty(married.Opportunities().Where(o => o.DefenderClanId == percy));
        ClaimOpportunity[] percysFeuds = marriedMinors.Opportunities()
            .Where(o => o.DefenderClanId == percy)
            .ToArray();
        Assert.Equal(
            new[] { ("clan_de_vere", "county_warkworth") },
            percysFeuds.Select(o => (o.AttackerClanId, o.TitleId)).OrderBy(pair => pair.TitleId));
        Assert.All(percysFeuds, o =>
        {
            Assert.DoesNotContain(
                kennedy, contentMinors.Candidates(contentMinors.Opportunity(o.TitleId, o.AttackerClanId)));
            Assert.Contains(kennedy, marriedMinors.Candidates(o));

            SupportDecision support = marriedMinors.Solicit(o);
            Assert.DoesNotContain(kennedy, support.AttackerSupporters);
            Assert.DoesNotContain(kennedy, support.DefenderSupporters);
        });

        Claim hers = Assert.Single(content.Claims(), c => c.ClaimantHeroId == bride);
        Assert.Equal((kennedy, "county_carrick", ClaimStrength.Weak), (hers.ClaimantClanId, hers.TitleId, hers.Strength));
        Assert.Equal(
            new[] { "county_carrick" },
            married.Claims().Where(c => c.ClaimantClanId == percy && c.ClaimantHeroId == bride).Select(c => c.TitleId));
        Assert.Equal(
            content.Claims().Where(c => c.ClaimantClanId == kennedy && c.ClaimantHeroId != bride),
            married.Claims().Where(c => c.ClaimantClanId == kennedy));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AScottishFriendOfTheClaimantRidesToAnEnglishFeud()
    {
        // Stafford and Woodville each muster two houses. Maxwell joins only above the foreign
        // friendship threshold, giving Stafford a third house without changing title allegiance.
        const string maxwell = "clan_maxwell";

        Content content = Content.Load();
        ClaimOpportunity cheshire = content.Opportunity("county_cheshire", "clan_stafford");

        Assert.DoesNotContain(maxwell, content.Candidates(cheshire));
        Assert.DoesNotContain(maxwell, content.Candidates(cheshire, Befriended(maxwell, 50f)));

        SupportDecision support = content.Solicit(cheshire, Befriended(maxwell, 51f));

        Assert.Equal(new[] { maxwell }, support.AttackerSupporters);
        Assert.DoesNotContain(maxwell, support.DefenderSupporters);
        Assert.Equal((3f, 2f), content.SidesAfter(cheshire, support));
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
        private readonly SuzeraintyPolicy _suzerain;
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
            _suzerain = new SuzeraintyPolicy(_titles, structure, genealogy);
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

        /// <summary>
        /// The same content with every holder a boy of seventeen. Every holder the content
        /// authors is a grown man, so this is the view in which a weak claim can be pressed.
        /// </summary>
        public Content WithMinorHolders() =>
            new(
                _genealogy.WithAge(
                    _titles.GetAllTitles().Select(title => title.HolderHeroId).OfType<string>().Distinct(), 17f),
                _structure,
                _titles.GetAllTitles());

        public IReadOnlyList<Claim> Claims() => _claims;

        public bool IsActionable(Claim claim) => _evaluateClaim.Execute(claim);

        public string? Holder(string titleId) => _titles.GetTitle(titleId)?.HolderHeroId;

        public IReadOnlyList<SuccessionResult> ExecuteSuccession(string deceasedHeroId) =>
            new ExecuteSuccessionUseCase(_titles, _genealogy, new SilentLoggerFactory()).Execute(deceasedHeroId, 0f);

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

                foreach (Claim claim in PressableBestClaims(title))
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

                foreach (Claim claim in PressableBestClaims(title))
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

        // A weak claim is dropped unless the holder is a woman or a child, as the behaviour drops it.
        private IEnumerable<Claim> PressableBestClaims(Title title) =>
            BestClaimPerClaimant(title.Id)
                .Where(claim => WeakClaimPolicy.IsPressable(claim.Strength, _genealogy.GetHero(title.HolderHeroId!)));

        private string? Suzerain(string clanId) => _suzerain.GetSuzerain(clanId);

        private IReadOnlyDictionary<string, float> UniformStrength() =>
            _titles.GetAllTitles()
                .Select(title => _genealogy.GetHolderClanOf(title))
                .Where(clanId => clanId is not null)
                .Select(clanId => clanId!)
                .Distinct()
                .ToDictionary(clanId => clanId, _ => 1f);
    }
}
