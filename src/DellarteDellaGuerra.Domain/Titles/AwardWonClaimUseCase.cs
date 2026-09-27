using System.Collections.Generic;
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
     *  A seat the winner occupies stops being contested, since the winner now holds it. A seat
     *  some other house occupies stays contested against the new holder.
     * </remarks>
     */
    public class AwardWonClaimUseCase : IAwardWonClaimUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly IGenealogy _genealogy;

        public AwardWonClaimUseCase(
            ITitleRepository titleRepository,
            IFeudalStructure feudalStructure,
            IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _genealogy = genealogy;
        }

        public IReadOnlyList<string> Execute(string titleId, string winnerClanId, string loserClanId)
        {
            var movedSeats = new List<string>();

            string? winnerLeaderId = _genealogy.GetClanLeaderId(winnerClanId);
            if (winnerLeaderId is null) return movedSeats;

            Award(titleId, winnerClanId, winnerLeaderId, loserClanId, movedSeats);
            return movedSeats;
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
