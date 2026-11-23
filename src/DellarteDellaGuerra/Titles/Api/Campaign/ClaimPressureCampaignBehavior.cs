using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Titles.Spi;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    /**
     * <summary>
     *  Turns standing claims into wars. Once a day it looks over the realm's claims, discards
     *  the ones nobody could act on, prices the rest, sends the calls to arms round the realm
     *  and declares a private war on the ones worth pressing.
     * </summary>
     * <remarks>
     *  Claims are evaluated per claimant clan and title rather than per claim: several heroes
     *  of one house commonly hold a claim on the same dignity, but they would all be fighting
     *  the same war, so the house's best claim stands for all of them and the cooldown is
     *  keyed the same way.
     *
     *  That cooldown is what makes the threshold mean anything. Re-evaluated daily, a pair
     *  sitting just under it declares the first time the numbers wobble across; looked at
     *  monthly, it declares when the situation actually changes. It is stamped whether or not
     *  the evaluation ends in a war, and it is persisted — restoring it on load is the whole
     *  point of keeping it.
     *
     *  A claim on a dignity the claimant's own house already holds is pressed by a hero rather
     *  than by the house: he founds a cadet branch and takes it to war, and unless he wins the
     *  branch is folded back in when the war ends.
     * </remarks>
     */
    public class ClaimPressureCampaignBehavior : CampaignBehaviorBase
    {
        private const float DeclarationThreshold = 0.8f;
        private const float EvaluationIntervalDays = 30f;

        // No clan carries the empty id, so pricing a war against it puts on the attacker's side
        // exactly the clans that pledged to a pretender whose house does not exist yet.
        private const string UnfoundedCadetBranch = "";

        private static readonly SupportDecision NoSupport = new(Array.Empty<string>(), Array.Empty<string>());

        private readonly ITitleRepository _titleRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IGenealogy _genealogy;
        private readonly SuzeraintyPolicy _suzeraintyPolicy;
        private readonly IGetInheritanceSettlementsUseCase _getInheritanceSettlementsUseCase;
        private readonly IAwardWonClaimUseCase _awardWonClaimUseCase;
        private readonly IGenerateBloodClaimsUseCase _generateBloodClaimsUseCase;
        private readonly IScreenClaimUseCase _screenClaimUseCase;
        private readonly IEvaluatePressClaimUseCase _evaluatePressClaimUseCase;
        private readonly ISolicitSupportUseCase _solicitSupportUseCase;
        private readonly IPrivateWarDeclaration _privateWarDeclaration;
        private readonly ICadetBranch _cadetBranch;
        private readonly IPersonalBondPolicy _personalBondPolicy;
        private readonly ILogger _logger;

        private Dictionary<string, float> _nextEvaluationDayByPair = new();
        private List<string> _serialisedCooldowns = new();
        private Dictionary<string, string> _parentByCadetClanId = new();
        private List<string> _serialisedCadetBranches = new();

        public ClaimPressureCampaignBehavior(
            ITitleRepository titleRepository,
            IClaimRepository claimRepository,
            IGenealogy genealogy,
            SuzeraintyPolicy suzeraintyPolicy,
            IGetInheritanceSettlementsUseCase getInheritanceSettlementsUseCase,
            IAwardWonClaimUseCase awardWonClaimUseCase,
            IGenerateBloodClaimsUseCase generateBloodClaimsUseCase,
            IScreenClaimUseCase screenClaimUseCase,
            IEvaluatePressClaimUseCase evaluatePressClaimUseCase,
            ISolicitSupportUseCase solicitSupportUseCase,
            IPrivateWarDeclaration privateWarDeclaration,
            ICadetBranch cadetBranch,
            IPersonalBondPolicy personalBondPolicy,
            ILoggerFactory loggerFactory)
        {
            _titleRepository = titleRepository;
            _claimRepository = claimRepository;
            _genealogy = genealogy;
            _suzeraintyPolicy = suzeraintyPolicy;
            _getInheritanceSettlementsUseCase = getInheritanceSettlementsUseCase;
            _awardWonClaimUseCase = awardWonClaimUseCase;
            _generateBloodClaimsUseCase = generateBloodClaimsUseCase;
            _screenClaimUseCase = screenClaimUseCase;
            _evaluatePressClaimUseCase = evaluatePressClaimUseCase;
            _solicitSupportUseCase = solicitSupportUseCase;
            _privateWarDeclaration = privateWarDeclaration;
            _cadetBranch = cadetBranch;
            _personalBondPolicy = personalBondPolicy;
            _logger = loggerFactory.CreateLogger<ClaimPressureCampaignBehavior>();

            // Subscribed here and not in RegisterEvents, which runs once per campaign started
            // while this behaviour is a container singleton: loading a save, quitting to the
            // menu and loading again would stack a second handler on the same object.
            _privateWarDeclaration.WarConcluded += OnWarConcluded;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                _serialisedCooldowns = ClaimCooldownSerialiser.Serialise(_nextEvaluationDayByPair);
                _serialisedCadetBranches = CadetBranchSerialiser.Serialise(_parentByCadetClanId);
            }

            dataStore.SyncData("DadgClaimPressureCooldowns", ref _serialisedCooldowns);
            dataStore.SyncData("DadgCadetBranches", ref _serialisedCadetBranches);

            _serialisedCooldowns ??= new List<string>();
            _serialisedCadetBranches ??= new List<string>();

            if (!dataStore.IsSaving)
            {
                _nextEvaluationDayByPair = ClaimCooldownSerialiser.Deserialise(_serialisedCooldowns);
                _parentByCadetClanId = CadetBranchSerialiser.Deserialise(_serialisedCadetBranches);
            }
        }

        private void OnDailyTick()
        {
            EndVoidClaimWars();
            PressClaims(onlyClaimantClanId: null, trace: null);
        }

        /// <summary>
        ///  Debug: evaluates one house's claims today, ignoring their cooldown, through the same gates
        ///  as the daily sweep. Returns why each claim was or was not pressed.
        /// </summary>
        public IReadOnlyList<string> PressClaimsNow(string claimantClanId)
        {
            foreach (string pair in _nextEvaluationDayByPair.Keys.Where(pair => pair.EndsWith(":" + claimantClanId)).ToList())
                _nextEvaluationDayByPair.Remove(pair);

            var trace = new List<string>();
            PressClaims(claimantClanId, trace);
            return trace;
        }

        private void PressClaims(string? onlyClaimantClanId, List<string>? trace)
        {
            float today = (float)CampaignTime.Now.ToDays;
            var clansById = Clan.All.ToDictionary(clan => clan.StringId);
            var strengthByClanId = clansById.ToDictionary(entry => entry.Key, entry => entry.Value.CurrentTotalStrength);

            // The suzerain of a clan is a repository walk, and every candidate pair needs the
            // whole realm's chains to sum its side, so resolve them once for the tick.
            var suzerainByClanId = clansById.Keys.ToDictionary(
                clanId => clanId,
                clanId => _suzeraintyPolicy.GetSuzerain(clanId));

            string? GetSuzerain(string clanId) =>
                suzerainByClanId.TryGetValue(clanId, out string? suzerain) ? suzerain : null;

            foreach (var title in _titleRepository.GetAllTitles())
            {
                if (title.HolderHeroId is null) continue;

                string? defenderClanId = _genealogy.GetClanOf(title.HolderHeroId);
                if (defenderClanId is null || !clansById.TryGetValue(defenderClanId, out var defenderClan)) continue;

                var holder = _genealogy.GetHero(title.HolderHeroId);

                foreach (var claim in BestClaimPerClaimant(title.Id))
                {
                    if (onlyClaimantClanId is not null && claim.ClaimantClanId != onlyClaimantClanId) continue;

                    bool Skip(string reason)
                    {
                        trace?.Add($"{title.Id} (held by {defenderClanId}): {reason}");
                        return true;
                    }

                    if (!clansById.TryGetValue(claim.ClaimantClanId, out var attackerClan)) continue;

                    bool isInternal = claim.ClaimantClanId == defenderClanId;
                    Hero? pretender = isInternal ? Pretender(claim, title) : null;
                    var rejection = _screenClaimUseCase.Execute(new ClaimCandidacy(
                        claim.ClaimantClanId,
                        defenderClanId,
                        claim.Strength,
                        holder,
                        Clan.PlayerClan?.StringId,
                        attackerClan.Kingdom?.RulingClan?.StringId,
                        attackerClan.Kingdom?.StringId,
                        defenderClan.Kingdom?.StringId,
                        HasPretender: pretender is not null,
                        ClaimantAtPrivateWar: _privateWarDeclaration.IsBelligerent(claim.ClaimantClanId)));
                    if (rejection is not null && Skip(rejection == ClaimRejection.TooWeak ? $"claim too weak ({claim.Strength})" : rejection.ToString())) continue;
                    var kingdom = attackerClan.Kingdom!;

                    string pair = $"{title.Id}:{claim.ClaimantClanId}";
                    if (_nextEvaluationDayByPair.TryGetValue(pair, out float nextDay) && today < nextDay) continue;
                    _nextEvaluationDayByPair[pair] = today + EvaluationIntervalDays;

                    string? mainGoalSettlementId = _privateWarDeclaration.SelectMainGoal(
                        defenderClanId,
                        _getInheritanceSettlementsUseCase.Execute(title.Id));
                    if (mainGoalSettlementId is null)
                    {
                        Skip("no main goal");
                        continue;
                    }

                    string attackerSideId = pretender is null ? claim.ClaimantClanId : UnfoundedCadetBranch;

                    // A house that rallies from another realm answers for itself: its vassals owe
                    // service to a liege abroad, not to this quarrel.
                    IReadOnlyCollection<string> Foreign(SupportDecision support) =>
                        support.AttackerSupporters
                            .Concat(support.DefenderSupporters)
                            .Where(clanId => clansById.TryGetValue(clanId, out var clan) && clan.Kingdom != kingdom)
                            .ToList();

                    ClaimOpportunity Price(SupportDecision support)
                    {
                        var (attackerStrength, defenderStrength) = WarSideStrength.Sum(
                            strengthByClanId,
                            GetSuzerain,
                            attackerSideId,
                            defenderClanId,
                            support.AttackerSupporters,
                            support.DefenderSupporters,
                            Foreign(support));

                        // A cadet branch's whole army is the men its founder leads, and the house
                        // he is leaving is still counting them among its own.
                        float ownMen = pretender?.PartyBelongedTo?.Party.EstimatedStrength ?? 0f;

                        return new ClaimOpportunity(
                            title.Id,
                            claim.ClaimantClanId,
                            defenderClanId,
                            claim.Strength,
                            attackerStrength + ownMen,
                            defenderStrength - ownMen,
                            IsDistracted(defenderClan),
                            GetRelation(pretender ?? attackerClan.Leader, defenderClan.Leader));
                    }

                    // Priced twice, because nobody pledges to a side on the strength of pledges
                    // not yet made: the calls to arms go out on what the hierarchy alone fields,
                    // and the war is then priced on the sides those calls actually produced.
                    var support = _solicitSupportUseCase.Execute(
                        Price(NoSupport),
                        Candidates(clansById, kingdom, attackerClan, defenderClan, pretender, attackerSideId, GetSuzerain));

                    float pressScore = _evaluatePressClaimUseCase.Execute(Price(support));
                    if (pressScore < DeclarationThreshold && Skip($"press score {pressScore:0.##} < {DeclarationThreshold}")) continue;

                    // Strength on paper is not an army in the field. A house whose lords are broke,
                    // starving or spent would declare and then never march, so the war has to be one
                    // its side could start fighting today. A pretender's branch is asked for before it
                    // is founded: it starts with no influence to raise an army on, so only the houses
                    // that pledged to him can lead his war.
                    if (!_privateWarDeclaration.CanProsecute(
                            attackerSideId,
                            defenderClanId,
                            mainGoalSettlementId,
                            support.AttackerSupporters,
                            support.DefenderSupporters)
                        && Skip($"cannot prosecute (score {pressScore:0.##}, goal {mainGoalSettlementId})")) continue;

                    string attackerClanId = claim.ClaimantClanId;

                    if (pretender is not null)
                    {
                        string? cadetClanId = _cadetBranch.Split(pretender.StringId, defenderClanId, title.SeatSettlementId);
                        if (cadetClanId is null)
                        {
                            Skip("cadet branch split failed");
                            continue;
                        }

                        _parentByCadetClanId[cadetClanId] = defenderClanId;
                        attackerClanId = cadetClanId;
                    }

                    _privateWarDeclaration.Declare(
                        attackerClanId,
                        defenderClanId,
                        title.Id,
                        mainGoalSettlementId,
                        today,
                        support.AttackerSupporters,
                        support.DefenderSupporters,
                        Foreign(support));
                    trace?.Add($"{title.Id} (held by {defenderClanId}): DECLARED by {attackerClanId}, score {pressScore:0.##}, goal {mainGoalSettlementId}, "
                               + $"supporters {support.AttackerSupporters.Count}/{support.DefenderSupporters.Count}");
                }
            }
        }

        // A claim war is fought against the house holding the title. Once the dignity has passed to
        // another, there is nothing left to take from it: the war ends in a white peace, which
        // reverts what was taken and folds a cadet branch back in. The claimant is free to press
        // the claim against the new holder once the pair's cooldown has run.
        private void EndVoidClaimWars()
        {
            foreach (var war in _privateWarDeclaration.GetActiveClaimWars())
            {
                var title = _titleRepository.GetTitle(war.TitleId);
                string? holderClanId = _genealogy.GetHolderClanOf(title);
                if (!ClaimWarValidity.IsVoid(war.DefenderClanId, holderClanId)) continue;

                _logger.Debug($"[PW] {war.WarId} cancelled: title {war.TitleId} no longer held by {war.DefenderClanId}; holder now {title?.HolderHeroId ?? "none"}({holderClanId ?? "none"})");
                _privateWarDeclaration.EndInWhitePeace(war.WarId);
            }
        }

        private void OnWarConcluded(PrivateWarConclusion conclusion)
        {
            bool titleWon = conclusion.AttackerWon && AwardTheClaim(conclusion);

            if (!_parentByCadetClanId.TryGetValue(conclusion.AttackerClanId, out string parentClanId)) return;

            _parentByCadetClanId.Remove(conclusion.AttackerClanId);

            // Taking the title makes the branch a house in its own right, holding the dignity it
            // fought for. Anything short of that, a white peace or a victory over a loser who no
            // longer held it included, leaves it nothing to be.
            if (!titleWon) _cadetBranch.Reabsorb(conclusion.AttackerClanId, parentClanId);
        }

        // The winner takes the title and the de jure titles the loser held, and the fortified
        // seats among them that the loser still owns or that his own side took during the war;
        // a house outside the war keeps what it holds. A village goes with its castle or town.
        // A title won across the border takes the clans whose primary title it carries into the
        // winner's realm, fiefs and all. Returns whether the winner took the title fought for.
        private bool AwardTheClaim(PrivateWarConclusion conclusion)
        {
            var winner = BannerlordCampaign.Current?.CampaignObjectManager.Find<Clan>(conclusion.AttackerClanId);
            if (winner?.Leader is null) return false;

            WonClaimAward award = _awardWonClaimUseCase.Execute(
                conclusion.TitleId, conclusion.AttackerClanId, conclusion.DefenderClanId, (float)CampaignTime.Now.ToDays);

            foreach (string seatId in award.MovedSeatIds)
            {
                var seat = Settlement.Find(seatId);
                if (seat is null || !seat.IsFortification || seat.OwnerClan is null) continue;
                if (seat.OwnerClan == winner)
                {
                    _logger.Info($"[PW] {conclusion.WarId} award: seat {seatId} (title {conclusion.TitleId}) skipped, already held by winner {winner.StringId}");
                    continue;
                }

                string ownerId = seat.OwnerClan.StringId;
                if (ownerId != conclusion.DefenderClanId)
                {
                    // A seat an attacker-side clan already held before the war stays with it.
                    string? preWarOwnerId = _privateWarDeclaration.GetPreWarOwnerClanId(conclusion.WarId, seatId);
                    if (!_privateWarDeclaration.IsOnAttackerSide(conclusion.WarId, ownerId)
                        || preWarOwnerId is null
                        || _privateWarDeclaration.IsOnAttackerSide(conclusion.WarId, preWarOwnerId))
                    {
                        _logger.Info($"[PW] {conclusion.WarId} award: seat {seatId} (title {conclusion.TitleId}) skipped, kept by {ownerId} (pre-war owner {preWarOwnerId ?? "unknown"})");
                        continue;
                    }
                }

                ChangeOwnerOfSettlementAction.ApplyByDefault(winner.Leader, seat);
                _logger.Info($"[PW] {conclusion.WarId} award: seat {seatId} (title {conclusion.TitleId}) owner {ownerId} -> winner {winner.StringId}");
            }

            foreach (string clanId in award.ClansJoiningWinnersRealm)
            {
                var clan = BannerlordCampaign.Current?.CampaignObjectManager.Find<Clan>(clanId);
                if (clan is null || winner.Kingdom is null || clan.Kingdom == winner.Kingdom) continue;
                if (clan.Kingdom?.RulingClan == clan) continue;

                ChangeKingdomAction.ApplyByJoinToKingdom(clan, winner.Kingdom);
            }

            // The holders moved, and every blood claim on these titles descends from its holder.
            _generateBloodClaimsUseCase.Execute();

            return award.TitleWon;
        }

        /**
         * <summary>
         *  The hero who would leave his house to press its own dignity, or null if there is
         *  nobody to leave: a clan-level claim belongs to no one hero, the holder cannot rise
         *  against himself, and a claimant who has already left presses it as his own house.
         * </summary>
         */
        private static Hero? Pretender(Claim claim, Title title)
        {
            if (claim.ClaimantHeroId is null || claim.ClaimantHeroId == title.HolderHeroId) return null;

            var pretender = BannerlordCampaign.Current?.CampaignObjectManager.Find<Hero>(claim.ClaimantHeroId);

            return pretender is not null && pretender.IsAlive && pretender.Clan?.StringId == claim.ClaimantClanId
                ? pretender
                : null;
        }

        /**
         * <summary>
         *  The other houses the war concerns, each with the two things that decide which way it
         *  answers: where the feudal hierarchy already puts it, and how its leader stands with
         *  the two principals. Which houses those are is <see cref="SupportCandidacy"/>'s call.
         * </summary>
         */
        private IReadOnlyCollection<SupportCandidate> Candidates(
            IReadOnlyDictionary<string, Clan> clansById,
            Kingdom kingdom,
            Clan attackerClan,
            Clan defenderClan,
            Hero? pretender,
            string attackerSideId,
            Func<string, string?> getSuzerain)
        {
            Hero? claimant = pretender ?? attackerClan.Leader;

            var eligibleClanIds = clansById.Values
                // The player is never enlisted into somebody else's quarrel, on either side, for
                // the same reason his house never starts one of these by itself.
                .Where(clan => clan != attackerClan && clan != defenderClan && clan != Clan.PlayerClan)
                .Where(clan => clan.Leader is not null)
                .Select(clan => clan.StringId);

            return SupportCandidacy
                .Select(
                    eligibleClanIds,
                    clanId => clansById[clanId].Kingdom == kingdom
                              || IsBonded(clansById[clanId].Leader, claimant)
                              || IsBonded(clansById[clanId].Leader, defenderClan.Leader),
                    getSuzerain,
                    attackerSideId,
                    defenderClan.StringId,
                    kingdom.RulingClan?.StringId)
                .Select(candidate =>
                {
                    Hero leader = clansById[candidate.ClanId].Leader;

                    return new SupportCandidate(
                        candidate.ClanId,
                        candidate.Allegiance,
                        GetRelation(leader, claimant),
                        GetRelation(leader, defenderClan.Leader));
                })
                .ToList();
        }

        private IEnumerable<Claim> BestClaimPerClaimant(string titleId)
        {
            return _claimRepository
                .GetClaimsOn(titleId)
                .GroupBy(claim => claim.ClaimantClanId)
                .Select(claims => claims.OrderByDescending(claim => claim.Strength).First());
        }

        private bool IsDistracted(Clan defenderClan)
        {
            var kingdom = defenderClan.Kingdom;

            return _privateWarDeclaration.IsBelligerent(defenderClan.StringId)
                   || (kingdom is not null && Kingdom.All.Any(other => other != kingdom && kingdom.IsAtWarWith(other)));
        }

        private bool IsBonded(Hero hero, Hero? principal)
        {
            return principal is not null
                   && _personalBondPolicy.IsBonded(hero.StringId, principal.StringId, GetRelation(hero, principal));
        }

        private static float GetRelation(Hero? claimant, Hero? holder)
        {
            return claimant is null || holder is null ? 0f : claimant.GetRelation(holder);
        }
    }
}
