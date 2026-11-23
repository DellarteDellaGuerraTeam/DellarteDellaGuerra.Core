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
     *  Strength decays down each generation from an anchor:
     *  <list type="bullet">
     *   <item>a holder's son takes a Strong claim, his daughter a Weak one;</item>
     *   <item>a Strong claimant's children take a Weak claim, son or daughter alike;</item>
     *   <item>a Weak claim passes to nobody.</item>
     *  </list>
     *  So a claim reaches at most two generations from an anchor, and a female link costs a
     *  generation without ever cutting the line outright.
     *
     *  The claimants on a title are the heroes ExecuteSuccessionUseCase could have chosen
     *  from — see <see cref="Anchors"/>. Everyone but the holder himself qualifies, his own
     *  kin included: a passed-over second son holding a Strong claim on his elder brother's
     *  dignity is what makes a house able to go to war with itself.
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

            var createdClaims = new List<Claim>();

            foreach (var title in titles)
            {
                if (title.HolderHeroId is null) continue;

                foreach (var descendant in Descendants(Anchors(title.HolderHeroId)))
                {
                    if (descendant.Key == title.HolderHeroId) continue;

                    var hero = _genealogy.GetHero(descendant.Key);
                    if (hero is null || !hero.IsAlive || hero.ClanId is null) continue;

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
         *  The two points a claim can descend from: the holder, whose children are next in
         *  line, and the holder's father, whose other children are the holder's brothers and
         *  sisters.
         * </summary>
         * <remarks>
         *  These are the two walks ExecuteSuccessionUseCase makes — HeirOfBody down from the
         *  holder, then Collateral up one level and down again — so every claimant is a hero
         *  succession could have picked. That is also why the walk stops at the father: the
         *  collateral rule goes up exactly one level, and anchoring the grandfather would
         *  mint Strong claims for uncles that no succession would ever honour.
         * </remarks>
         */
        private List<string> Anchors(string holderHeroId)
        {
            var anchors = new List<string> { holderHeroId };

            if (_genealogy.GetHero(holderHeroId)?.FatherId is { } fatherId) anchors.Add(fatherId);

            return anchors;
        }

        /**
         * <summary>
         *  Walks down from each anchor, keeping the strongest level every descendant reaches.
         *  An anchor is only returned when another anchor's walk reaches it — the holder is
         *  one of his own father's children — and the caller drops him there.
         * </summary>
         * <remarks>
         *  A cyclic bloodline cannot trap this walk: every hop decays the level and the walk
         *  stops below Weak, which bounds it to two generations whatever the data says. That
         *  is why the self-parent record that crashed the succession walk never touched claim
         *  derivation, and why no visited set is needed here.
         * </remarks>
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
