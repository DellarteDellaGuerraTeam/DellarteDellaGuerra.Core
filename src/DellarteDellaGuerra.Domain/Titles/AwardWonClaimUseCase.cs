using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  What a won claim war gives: the title fought for, and every title below it in the de
     *  jure hierarchy that the loser holds, all go to the winner's leader. A title below it
     *  that another house holds stays with that house, and the loser keeps whatever lies
     *  outside the fought-for title.
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

        public WonClaimAward Execute(string titleId, string winnerClanId, string loserClanId)
        {
            var movedSeats = new List<string>();

            var joiningClans = new List<string>();

            string? winnerLeaderId = _genealogy.GetClanLeaderId(winnerClanId);
            if (winnerLeaderId is null) return new WonClaimAward(movedSeats, joiningClans);

            // Looked up before the award, so a won title of the same rank does not displace it.
            Title? winnerPrimaryTitle = _suzeraintyPolicy.GetPrimaryTitle(winnerClanId);

            Award(titleId, winnerClanId, winnerLeaderId, loserClanId, movedSeats);

            if (winnerPrimaryTitle is not null && MoveIntoWinnersRealm(titleId, winnerPrimaryTitle))
            {
                joiningClans.AddRange(ClansWhosePrimaryTitleIsUnder(titleId, winnerClanId));
            }

            return new WonClaimAward(movedSeats, joiningClans);
        }

        private bool MoveIntoWinnersRealm(string titleId, Title winnerPrimaryTitle)
        {
            TitleRank? rank = _feudalStructure.GetRank(titleId);
            if (rank is null || rank >= TitleRank.King) return false;

            string winnerRealm = GetRealm(winnerPrimaryTitle.Id);
            if (GetRealm(titleId) == winnerRealm) return false;

            string? suzerainTitleId = rank < winnerPrimaryTitle.Rank ? winnerPrimaryTitle.Id
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
            List<string> movedSeats)
        {
            Title? title = _titleRepository.GetTitle(titleId);
            if (title is not null && _genealogy.GetHolderClanOf(title) == loserClanId)
            {
                Title awarded = title.WithHolder(winnerLeaderId);
                if (awarded.OccupantClanId == winnerClanId) awarded = awarded.WithOccupant(null, null);

                _titleRepository.SaveTitle(awarded);
                if (!string.IsNullOrEmpty(title.SeatSettlementId)) movedSeats.Add(title.SeatSettlementId);
            }

            foreach (string vassalTitleId in _feudalStructure.GetDeJureVassalTitleIds(titleId))
            {
                Award(vassalTitleId, winnerClanId, winnerLeaderId, loserClanId, movedSeats);
            }
        }
    }
}
