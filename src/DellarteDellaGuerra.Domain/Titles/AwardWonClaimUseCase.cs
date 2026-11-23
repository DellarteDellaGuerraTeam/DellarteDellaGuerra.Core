using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  What a won claim war gives: the title fought for and the inheritance the loser holds
     *  under it, the titles answering to it by allegiance and in turn theirs, all go to the
     *  winner's leader. A title another house holds under it stays with that house, and the
     *  loser keeps whatever lies outside the fought-for inheritance.
     * </summary>
     * <remarks>
     *  Returns the seats of the titles it moved, so the caller can hand over the settlements
     *  that go with them. The loser is given no claim on what it lost.
     *
     *  A lord cannot be the vassal of two kings, so a title won across the border leaves its
     *  realm with everything below it. It goes under the winner's primary title when it ranks
     *  lower, and otherwise directly under the winner's king. The clans whose primary title
     *  went with it now serve in the winner's realm, and are returned so the caller can move
     *  them there. A won kingdom stays a realm of its own.
     *
     *  A seat the winner occupies stops being contested, since the winner now holds it. A seat
     *  some other house occupies stays contested against the new holder.
     * </remarks>
     */
    public class AwardWonClaimUseCase : IAwardWonClaimUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly IGenealogy _genealogy;
        private readonly SuzeraintyPolicy _suzeraintyPolicy;

        public AwardWonClaimUseCase(
            ITitleRepository titleRepository,
            IFeudalStructure feudalStructure,
            IGenealogy genealogy,
            SuzeraintyPolicy suzeraintyPolicy)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _genealogy = genealogy;
            _suzeraintyPolicy = suzeraintyPolicy;
        }

        public WonClaimAward Execute(string titleId, string winnerClanId, string loserClanId, float currentDay)
        {
            var movedSeats = new List<string>();

            var joiningClans = new List<string>();

            string? winnerLeaderId = _genealogy.GetClanLeaderId(winnerClanId);
            if (winnerLeaderId is null) return new WonClaimAward(false, movedSeats, joiningClans);

            // Looked up before the award, so a won title of the same rank does not displace it.
            Title? winnerPrimaryTitle = _suzeraintyPolicy.GetPrimaryTitle(winnerClanId);

            // The title fought for may have passed to an heir of another house while the war went
            // on. Then it is not the loser's to give up, and it stays in its realm with its clans.
            bool titleWon = _genealogy.GetHolderClanOf(_titleRepository.GetTitle(titleId)) == loserClanId;

            var movedTitleIds = new List<string>();
            Award(titleId, winnerClanId, winnerLeaderId, loserClanId, currentDay, movedSeats, movedTitleIds);

            if (titleWon && winnerPrimaryTitle is not null && MoveIntoWinnersRealm(titleId, winnerPrimaryTitle))
            {
                // The rest of the inheritance follows the title it answers to across the border.
                string winnerRealm = GetRealm(titleId);
                foreach (string movedTitleId in movedTitleIds.Where(id => id != titleId && GetRealm(id) != winnerRealm))
                {
                    _feudalStructure.Reattach(movedTitleId, titleId);
                }

                joiningClans.AddRange(ClansWhosePrimaryTitleIsUnder(titleId, winnerClanId));
            }

            return new WonClaimAward(titleWon, movedSeats, joiningClans);
        }

        private bool MoveIntoWinnersRealm(string titleId, Title winnerPrimaryTitle)
        {
            TitleRank? rank = _feudalStructure.GetRank(titleId);
            if (rank is null || rank >= TitleRank.King) return false;

            string winnerRealm = GetRealm(winnerPrimaryTitle.Id);
            if (GetRealm(titleId) == winnerRealm) return false;

            // Territory still nests by map tier when the house's dignity uses a different rank.
            string? suzerainTitleId = rank < _feudalStructure.GetRank(winnerPrimaryTitle.Id) ? winnerPrimaryTitle.Id
                : _feudalStructure.GetRank(winnerRealm) > rank ? winnerRealm
                : null;
            _feudalStructure.Reattach(titleId, suzerainTitleId);
            return true;
        }

        private string GetRealm(string titleId)
        {
            string realm = titleId;
            while (_feudalStructure.GetDeJureSuzerainTitleId(realm) is { } suzerainTitleId)
            {
                realm = suzerainTitleId;
            }

            return realm;
        }

        private IEnumerable<string> ClansWhosePrimaryTitleIsUnder(string titleId, string winnerClanId)
        {
            var subtree = new List<string>();
            CollectSubtree(titleId, subtree);

            return subtree
                .Select(id => _genealogy.GetHolderClanOf(_titleRepository.GetTitle(id)))
                .OfType<string>()
                .Where(clanId => clanId != winnerClanId)
                .Distinct()
                .Where(clanId => _suzeraintyPolicy.GetPrimaryTitle(clanId) is { } primary
                                 && subtree.Contains(primary.Id))
                .ToList();
        }

        private void CollectSubtree(string titleId, List<string> subtree)
        {
            subtree.Add(titleId);
            foreach (string vassalTitleId in _feudalStructure.GetDeJureVassalTitleIds(titleId))
            {
                CollectSubtree(vassalTitleId, subtree);
            }
        }

        private void Award(
            string titleId,
            string winnerClanId,
            string winnerLeaderId,
            string loserClanId,
            float currentDay,
            List<string> movedSeats,
            List<string> movedTitleIds)
        {
            // Collected before anything moves, since moving a title changes who holds it.
            List<string> inheritance = new[] { titleId }
                .Concat(TitleInheritance.HeldUnder(titleId, loserClanId, _feudalStructure, _titleRepository, _genealogy))
                .ToList();

            foreach (string inheritedTitleId in inheritance)
            {
                Title? title = _titleRepository.GetTitle(inheritedTitleId);
                if (title is null || _genealogy.GetHolderClanOf(title) != loserClanId) continue;

                _titleRepository.SaveTitle(title.AwardTo(winnerLeaderId, winnerClanId, currentDay));
                if (!string.IsNullOrEmpty(title.SeatSettlementId)) movedSeats.Add(title.SeatSettlementId);
                movedTitleIds.Add(inheritedTitleId);
            }
        }
    }
}
