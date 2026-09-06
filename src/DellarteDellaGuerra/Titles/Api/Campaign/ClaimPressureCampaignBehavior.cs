using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Titles.Spi;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    /**
     * <summary>
     *  Turns standing claims into wars. Once a day it looks over the realm's claims, discards
     *  the ones nobody could act on, prices the rest and declares a private war on the ones
     *  worth pressing.
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
     * </remarks>
     */
    public class ClaimPressureCampaignBehavior : CampaignBehaviorBase
    {
        private const float DeclarationThreshold = 0.8f;
        private const float DeclarationChance = 0.25f;
        private const float EvaluationIntervalDays = 30f;

        private readonly ITitleRepository _titleRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IGenealogy _genealogy;
        private readonly IGetSuzerainUseCase _getSuzerainUseCase;
        private readonly IGetDeJureSettlementsUseCase _getDeJureSettlementsUseCase;
        private readonly IEvaluatePressClaimUseCase _evaluatePressClaimUseCase;
        private readonly IPrivateWarDeclaration _privateWarDeclaration;

        private Dictionary<string, float> _nextEvaluationDayByPair = new();
        private List<string> _serialisedCooldowns = new();

        public ClaimPressureCampaignBehavior(
            ITitleRepository titleRepository,
            IClaimRepository claimRepository,
            IGenealogy genealogy,
            IGetSuzerainUseCase getSuzerainUseCase,
            IGetDeJureSettlementsUseCase getDeJureSettlementsUseCase,
            IEvaluatePressClaimUseCase evaluatePressClaimUseCase,
            IPrivateWarDeclaration privateWarDeclaration)
        {
            _titleRepository = titleRepository;
            _claimRepository = claimRepository;
            _genealogy = genealogy;
            _getSuzerainUseCase = getSuzerainUseCase;
            _getDeJureSettlementsUseCase = getDeJureSettlementsUseCase;
            _evaluatePressClaimUseCase = evaluatePressClaimUseCase;
            _privateWarDeclaration = privateWarDeclaration;
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
            }

            dataStore.SyncData("DadgClaimPressureCooldowns", ref _serialisedCooldowns);

            _serialisedCooldowns ??= new List<string>();

            if (!dataStore.IsSaving)
            {
                _nextEvaluationDayByPair = ClaimCooldownSerialiser.Deserialise(_serialisedCooldowns);
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
                clanId => _getSuzerainUseCase.Execute(clanId));

            foreach (var title in _titleRepository.GetAllTitles())
            {
                if (title.HolderHeroId is null) continue;

                string? defenderClanId = _genealogy.GetClanOf(title.HolderHeroId);
                if (defenderClanId is null || !clansById.TryGetValue(defenderClanId, out var defenderClan)) continue;

                foreach (var claim in BestClaimPerClaimant(title.Id))
                {
                    // A clan cannot go to war with itself. A passed-over brother's claim on his
                    // own house's dignity waits for the cadet spinoff.
                    if (claim.ClaimantClanId == defenderClanId) continue;
                    if (!clansById.TryGetValue(claim.ClaimantClanId, out var attackerClan)) continue;
                    if (attackerClan == Clan.PlayerClan || defenderClan == Clan.PlayerClan) continue;

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

                    var (attackerStrength, defenderStrength) = WarSideStrength.Sum(
                        strengthByClanId,
                        clanId => suzerainByClanId.TryGetValue(clanId, out string? suzerain) ? suzerain : null,
                        claim.ClaimantClanId,
                        defenderClanId);

                    var opportunity = new ClaimOpportunity(
                        title.Id,
                        claim.ClaimantClanId,
                        defenderClanId,
                        claim.Strength,
                        attackerStrength,
                        defenderStrength,
                        IsDistracted(defenderClan, kingdom),
                        GetRelation(attackerClan, defenderClan));

                    if (_evaluatePressClaimUseCase.Execute(opportunity) < DeclarationThreshold) continue;
                    if (MBRandom.RandomFloat >= DeclarationChance) continue;

                    _privateWarDeclaration.Declare(
                        claim.ClaimantClanId,
                        defenderClanId,
                        title.Id,
                        mainGoalSettlementId,
                        today);
                }
            }
        }

        private IEnumerable<Claim> BestClaimPerClaimant(string titleId)
        {
            return _claimRepository
                .GetClaimsOn(titleId)
                .GroupBy(claim => claim.ClaimantClanId)
                .Select(claims => claims.OrderByDescending(claim => claim.Strength).First());
        }

        private bool IsDistracted(Clan defenderClan, Kingdom kingdom)
        {
            return _privateWarDeclaration.IsBelligerent(defenderClan.StringId)
                   || Kingdom.All.Any(other => other != kingdom && kingdom.IsAtWarWith(other));
        }

        private static float GetRelation(Clan attackerClan, Clan defenderClan)
        {
            return attackerClan.Leader is null || defenderClan.Leader is null
                ? 0f
                : attackerClan.Leader.GetRelation(defenderClan.Leader);
        }
    }
}
