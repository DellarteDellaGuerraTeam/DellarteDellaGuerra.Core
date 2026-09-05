using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Derives every claim that descends by blood, for the whole realm, from the current
     *  bloodlines. Claims are a pure function of the genealogy and the title holders, so
     *  this rebuilds them wholesale rather than patching them incrementally: run it when a
     *  campaign starts, when a save loads and whenever a death or a dispossession moves a
     *  title.
     * </summary>
     * <remarks>
     *  Strength decays down each generation from the title's holder:
     *  <list type="bullet">
     *   <item>a holder's son takes a Strong claim, his daughter a Weak one;</item>
     *   <item>a Strong claimant's children take a Weak claim, son or daughter alike;</item>
     *   <item>a Weak claim passes to nobody.</item>
     *  </list>
     *  So a claim reaches at most two generations from the last holder, and a female link
     *  costs a generation without ever cutting the line outright.
     *
     *  Only claims against another clan's title are recorded. A claim on a title one's own
     *  clan already holds is not actionable and every consumer discards it.
     * </remarks>
     */
    public class GenerateBloodClaimsUseCase : IGenerateBloodClaimsUseCase
    {
        private const int HolderLevel = 3;
        private const int StrongLevel = 2;
        private const int WeakLevel = 1;

        private readonly ITitleRepository _titleRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IGenealogy _genealogy;

        public GenerateBloodClaimsUseCase(
            ITitleRepository titleRepository,
            IClaimRepository claimRepository,
            IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _claimRepository = claimRepository;
            _genealogy = genealogy;
        }

        public IReadOnlyList<Claim> Execute()
        {
            var titles = _titleRepository.GetAllTitles();
            RemovePreviouslyDerivedClaims(titles);

            var holderClanByTitleId = titles.ToDictionary(
                title => title.Id, title => _genealogy.GetHolderClanOf(title));
            var principalTitleByClan = PrincipalTitleByClan(titles, holderClanByTitleId);
            var createdClaims = new List<Claim>();

            foreach (var title in titles)
            {
                string? holderClanId = holderClanByTitleId[title.Id];
                if (holderClanId is null) continue;

                bool isPrincipalTitle = principalTitleByClan.TryGetValue(holderClanId, out string? principalTitleId)
                                        && principalTitleId == title.Id;

                foreach (var descendant in Descendants(Anchors(holderClanId, isPrincipalTitle)))
                {
                    var hero = _genealogy.GetHero(descendant.Key);
                    if (hero is null || !hero.IsAlive || hero.ClanId is null) continue;
                    if (hero.ClanId == holderClanId) continue;

                    var claim = new Claim(
                        $"{title.Id}:{hero.Id}:blood",
                        hero.ClanId,
                        title.Id,
                        descendant.Value >= StrongLevel ? ClaimStrength.Strong : ClaimStrength.Weak,
                        ClaimOrigin.Inheritance,
                        hero.Id);

                    _claimRepository.AddClaim(claim);
                    createdClaims.Add(claim);
                }
            }

            return createdClaims;
        }

        private void RemovePreviouslyDerivedClaims(IReadOnlyList<Title> titles)
        {
            foreach (string claimId in titles
                         .SelectMany(title => _claimRepository.GetClaimsOn(title.Id))
                         .Where(claim => claim.Origin == ClaimOrigin.Inheritance)
                         .Select(claim => claim.Id)
                         .ToList())
            {
                _claimRepository.RemoveClaim(claimId);
            }
        }

        /**
         * <summary>
         *  The highest-ranking title each clan holds. The campaign data records no per-hero
         *  title history, so a clan's dead are anchored to this dignity alone; anchoring them
         *  to every title the clan holds would let one long-dead ancestor scatter claims
         *  across a whole portfolio.
         * </summary>
         */
        private static Dictionary<string, string> PrincipalTitleByClan(
            IReadOnlyList<Title> titles, IReadOnlyDictionary<string, string?> holderClanByTitleId)
        {
            return titles
                .Where(title => holderClanByTitleId[title.Id] is not null)
                .GroupBy(title => holderClanByTitleId[title.Id]!)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(title => title.Rank).First().Id);
        }

        private List<string> Anchors(string holderClanId, bool includeDeceased)
        {
            var anchors = new List<string>();

            string? leaderId = _genealogy.GetClanLeaderId(holderClanId);
            if (leaderId is not null) anchors.Add(leaderId);

            if (includeDeceased) anchors.AddRange(_genealogy.GetDeceasedClanMemberIds(holderClanId));

            return anchors;
        }

        /**
         * <summary>
         *  Walks down from each anchor, keeping the strongest level every descendant reaches.
         *  The anchors themselves are holders, not claimants, so they are never returned.
         * </summary>
         */
        private Dictionary<string, int> Descendants(IReadOnlyList<string> anchors)
        {
            var levelByHeroId = new Dictionary<string, int>();
            var pending = new Queue<KeyValuePair<string, int>>();

            foreach (string anchorId in anchors)
            {
                pending.Enqueue(new KeyValuePair<string, int>(anchorId, HolderLevel));
            }

            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                var hero = _genealogy.GetHero(current.Key);
                if (hero is null) continue;

                foreach (string childId in hero.ChildIds)
                {
                    var child = _genealogy.GetHero(childId);
                    if (child is null) continue;

                    int level = child.IsFemale
                        ? System.Math.Min(current.Value - 1, WeakLevel)
                        : current.Value - 1;
                    if (level < WeakLevel) continue;

                    if (levelByHeroId.TryGetValue(childId, out int known) && known >= level) continue;

                    levelByHeroId[childId] = level;
                    pending.Enqueue(new KeyValuePair<string, int>(childId, level));
                }
            }

            return levelByHeroId;
        }
    }
}
