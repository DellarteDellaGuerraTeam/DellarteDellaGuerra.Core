using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Titles.Spi;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
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
        private const float DeclarationChance = 0.25f;
        private const float EvaluationIntervalDays = 30f;

        // No clan carries the empty id, so pricing a war against it puts on the attacker's side
        // exactly the clans that pledged to a pretender whose house does not exist yet.
        private const string UnfoundedCadetBranch = "";

        private static readonly SupportDecision NoSupport = new(Array.Empty<string>(), Array.Empty<string>());

        private readonly ITitleRepository _titleRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IGenealogy _genealogy;
        private readonly SuzeraintyPolicy _suzeraintyPolicy;
        private readonly IGetDeJureSettlementsUseCase _getDeJureSettlementsUseCase;
        private readonly IAwardWonClaimUseCase _awardWonClaimUseCase;
        private readonly IGenerateBloodClaimsUseCase _generateBloodClaimsUseCase;
        private readonly IEvaluatePressClaimUseCase _evaluatePressClaimUseCase;
        private readonly ISolicitSupportUseCase _solicitSupportUseCase;
        private readonly IPrivateWarDeclaration _privateWarDeclaration;
        private readonly ICadetBranch _cadetBranch;
        private readonly IPersonalBondPolicy _personalBondPolicy;

        private Dictionary<string, float> _nextEvaluationDayByPair = new();
        private List<string> _serialisedCooldowns = new();
        private Dictionary<string, string> _parentByCadetClanId = new();
        private List<string> _serialisedCadetBranches = new();

        public ClaimPressureCampaignBehavior(
            ITitleRepository titleRepository,
            IClaimRepository claimRepository,
            IGenealogy genealogy,
            SuzeraintyPolicy suzeraintyPolicy,
            IGetDeJureSettlementsUseCase getDeJureSettlementsUseCase,
            IAwardWonClaimUseCase awardWonClaimUseCase,
            IGenerateBloodClaimsUseCase generateBloodClaimsUseCase,
            IEvaluatePressClaimUseCase evaluatePressClaimUseCase,
            ISolicitSupportUseCase solicitSupportUseCase,
            IPrivateWarDeclaration privateWarDeclaration,
            ICadetBranch cadetBranch,
            IPersonalBondPolicy personalBondPolicy)
        {
            _titleRepository = titleRepository;
            _claimRepository = claimRepository;
            _genealogy = genealogy;
            _suzeraintyPolicy = suzeraintyPolicy;
            _getDeJureSettlementsUseCase = getDeJureSettlementsUseCase;
            _awardWonClaimUseCase = awardWonClaimUseCase;
            _generateBloodClaimsUseCase = generateBloodClaimsUseCase;
            _evaluatePressClaimUseCase = evaluatePressClaimUseCase;
            _solicitSupportUseCase = solicitSupportUseCase;
            _privateWarDeclaration = privateWarDeclaration;
            _cadetBranch = cadetBranch;
            _personalBondPolicy = personalBondPolicy;

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
                    if (!WeakClaimPolicy.IsPressable(claim.Strength, holder)) continue;
                    if (!clansById.TryGetValue(claim.ClaimantClanId, out var attackerClan)) continue;
                    if (attackerClan == Clan.PlayerClan || defenderClan == Clan.PlayerClan) continue;

                    // A clan still cannot go to war with itself, so a claim on its own dignity
                    // is only actionable if a hero of the house is behind it to leave with.
                    bool isInternal = claim.ClaimantClanId == defenderClanId;
                    Hero? pretender = isInternal ? Pretender(claim, title) : null;
                    if (isInternal && pretender is null) continue;

                    var kingdom = attackerClan.Kingdom;
                    if (kingdom is null || kingdom != defenderClan.Kingdom) continue;

                    if (_privateWarDeclaration.IsBelligerent(claim.ClaimantClanId)) continue;

                    string pair = $"{title.Id}:{claim.ClaimantClanId}";
                    if (_nextEvaluationDayByPair.TryGetValue(pair, out float nextDay) && today < nextDay) continue;
                    _nextEvaluationDayByPair[pair] = today + EvaluationIntervalDays;

                    string? mainGoalSettlementId = _privateWarDeclaration.SelectMainGoal(
                        defenderClanId,
                        _getDeJureSettlementsUseCase.Execute(title.Id));
                    if (mainGoalSettlementId is null) continue;

                    string attackerSideId = pretender is null ? claim.ClaimantClanId : UnfoundedCadetBranch;

                    ClaimOpportunity Price(SupportDecision support)
                    {
                        var (attackerStrength, defenderStrength) = WarSideStrength.Sum(
                            strengthByClanId,
                            GetSuzerain,
                            attackerSideId,
                            defenderClanId,
                            support.AttackerSupporters,
                            support.DefenderSupporters);

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

                    if (_evaluatePressClaimUseCase.Execute(Price(support)) < DeclarationThreshold) continue;
                    if (MBRandom.RandomFloat >= DeclarationChance) continue;

                    string attackerClanId = claim.ClaimantClanId;

                    if (pretender is not null)
                    {
                        string? cadetClanId = _cadetBranch.Split(pretender.StringId, defenderClanId, title.SeatSettlementId);
                        if (cadetClanId is null) continue;

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
                        support.DefenderSupporters);
                }
            }
        }

        private void OnWarConcluded(PrivateWarConclusion conclusion)
        {
            if (conclusion.AttackerWon) AwardTheClaim(conclusion);

            if (!_parentByCadetClanId.TryGetValue(conclusion.AttackerClanId, out string parentClanId)) return;

            _parentByCadetClanId.Remove(conclusion.AttackerClanId);

            // Winning makes the branch a house in its own right, holding the dignity it fought
            // for. Anything short of winning, a white peace included, leaves it nothing to be.
            if (!conclusion.AttackerWon) _cadetBranch.Reabsorb(conclusion.AttackerClanId, parentClanId);
        }

        // The winner takes the title and the de jure titles the loser held, and the fortified
        // seats among them that the loser still owns. A village goes with its castle or town.
        // A title won across the border takes the clans whose primary title it carries into the
        // winner's realm, fiefs and all.
        private void AwardTheClaim(PrivateWarConclusion conclusion)
        {
            var winner = BannerlordCampaign.Current?.CampaignObjectManager.Find<Clan>(conclusion.AttackerClanId);
            if (winner?.Leader is null) return;

            WonClaimAward award = _awardWonClaimUseCase.Execute(
                conclusion.TitleId, conclusion.AttackerClanId, conclusion.DefenderClanId);

            foreach (string seatId in award.MovedSeatIds)
            {
                var seat = Settlement.Find(seatId);
                if (seat is null || !seat.IsFortification || seat.OwnerClan?.StringId != conclusion.DefenderClanId)
                    continue;

                ChangeOwnerOfSettlementAction.ApplyByDefault(winner.Leader, seat);
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
                    defenderClan.StringId)
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
