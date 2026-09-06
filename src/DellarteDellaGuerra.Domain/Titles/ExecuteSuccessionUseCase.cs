using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Passes a dead holder's dignities to his heir by male-preference primogeniture: sons by
     *  seniority, then daughters, then the deceased's own brothers and sisters, and finally
     *  the head of his clan so that no title is orphaned.
     * </summary>
     * <remarks>
     *  Heirs are resolved from the bloodlines alone. Vanilla's ChangeClanLeaderAction runs on
     *  the same death and the order of the two is not guaranteed, so reading Clan.Leader to
     *  find an heir would be a race; it is consulted only as the last-resort backstop, where a
     *  stale answer still beats an orphan.
     *
     *  A predeceased child does not end the line: his own descendants stand in his place
     *  (representation). That is what lets a dignity descend through a female link — the
     *  Yorkist claim in this setting is exactly such a descent.
     *
     *  <para>
     *  The descent walk carries a visited set because the bloodlines come from authored
     *  content, not from the engine's own invariants: a single mistyped parent id makes a
     *  hero his own ancestor, and an unguarded walk then recurses until the process dies.
     *  A StackOverflowException cannot be caught in .NET, so this is a crash rather than a
     *  bad succession. Claim derivation never needed the guard — its strength counter bounds
     *  it to two generations — which is why the defect stayed invisible until now.
     *  </para>
     * </remarks>
     */
    public class ExecuteSuccessionUseCase : IExecuteSuccessionUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IGenealogy _genealogy;
        private readonly ILogger _logger;

        public ExecuteSuccessionUseCase(
            ITitleRepository titleRepository,
            IGenealogy genealogy,
            ILoggerFactory loggerFactory)
        {
            _titleRepository = titleRepository;
            _genealogy = genealogy;
            _logger = loggerFactory.CreateLogger<ExecuteSuccessionUseCase>();
        }

        public IReadOnlyList<SuccessionResult> Execute(string deceasedHeroId)
        {
            var vacatedTitles = _titleRepository.GetAllTitles()
                .Where(title => title.HolderHeroId == deceasedHeroId)
                .ToList();
            if (vacatedTitles.Count == 0) return new List<SuccessionResult>();

            string? heirId = ResolveHeir(deceasedHeroId);

            var results = new List<SuccessionResult>(vacatedTitles.Count);
            foreach (var title in vacatedTitles)
            {
                _titleRepository.SaveTitle(title.WithHolder(heirId));
                results.Add(new SuccessionResult(title.Id, deceasedHeroId, heirId));
            }

            _logger.Info(heirId is null
                ? $"'{deceasedHeroId}' died leaving no heir; {vacatedTitles.Count} title(s) left vacant"
                : $"'{deceasedHeroId}' died; {vacatedTitles.Count} title(s) pass to '{heirId}'");

            return results;
        }

        private string? ResolveHeir(string deceasedHeroId)
        {
            var deceased = _genealogy.GetHero(deceasedHeroId);
            if (deceased is null) return null;

            // Seeded with the deceased so that he can neither inherit from himself nor be
            // walked into a second time. See VisitedHeroIds on why the set exists at all.
            var visited = new HashSet<string> { deceasedHeroId };

            return HeirOfBody(deceased, visited)
                   ?? Collateral(deceased, visited)
                   ?? ClanLeaderBackstop(deceased);
        }

        /**
         * <summary>
         *  The deceased's own descendants: the whole male line first — a predeceased son
         *  represented by his children — and only then daughters. Male preference means a
         *  daughter inherits when no son's line survives at all, not merely when the eldest
         *  son is dead.
         * </summary>
         */
        private string? HeirOfBody(HeroNode ancestor, ISet<string> visited)
        {
            foreach (var child in BySeniority(ancestor))
            {
                if (!visited.Add(child.Id)) continue;
                if (child.IsAlive) return child.Id;
                if (HeirOfBody(child, visited) is { } heirId) return heirId;
            }

            return null;
        }

        /**
         * <summary>
         *  The deceased's brothers, then his sisters, eldest first — reached through his
         *  father, since a sibling is one of a father's other children. A predeceased sibling
         *  is represented by his own line, exactly as a child is.
         * </summary>
         */
        private string? Collateral(HeroNode deceased, ISet<string> visited)
        {
            var father = deceased.FatherId is null ? null : _genealogy.GetHero(deceased.FatherId);
            if (father is null) return null;

            // The deceased is already in the visited set, so iterating his father's children
            // skips him without a special case.
            foreach (var sibling in BySeniority(father))
            {
                if (!visited.Add(sibling.Id)) continue;
                if (sibling.IsAlive) return sibling.Id;
                if (HeirOfBody(sibling, visited) is { } heirId) return heirId;
            }

            return null;
        }

        /**
         * <summary>
         *  Never orphan a title: it falls to the head of the clan the deceased belonged to.
         *  A dead leader is refused — the death that triggered this succession may not have
         *  been processed by the engine's own leadership handover yet — leaving the title
         *  vacant, which the next grant or conquest re-fills.
         * </summary>
         */
        private string? ClanLeaderBackstop(HeroNode deceased)
        {
            string? leaderId = deceased.ClanId is null ? null : _genealogy.GetClanLeaderId(deceased.ClanId);
            if (leaderId is null) return null;

            return _genealogy.GetHero(leaderId) is { IsAlive: true } ? leaderId : null;
        }

        /// <summary>Sons eldest-first, then daughters eldest-first.</summary>
        private IEnumerable<HeroNode> BySeniority(HeroNode parent)
        {
            var children = parent.ChildIds
                .Select(childId => _genealogy.GetHero(childId))
                .Where(child => child is not null)
                .Select(child => child!)
                .ToList();

            return children.Where(child => !child.IsFemale).OrderByDescending(child => child.Age)
                .Concat(children.Where(child => child.IsFemale).OrderByDescending(child => child.Age));
        }
    }
}
