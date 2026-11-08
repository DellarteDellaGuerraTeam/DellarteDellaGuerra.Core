using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /// <summary>
    /// Flattens a title to the settlements of the inheritance it carries: its own seat plus the
    /// seat of every title its holder's house holds under it by allegiance. A war over the Duchy
    /// of York is fought for the York inheritance, so this is the set the main goal is picked
    /// from. A claim on a lesser title disputes only that estate and what its holder keeps under it.
    /// </summary>
    public class GetInheritanceSettlementsUseCase : IGetInheritanceSettlementsUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly IGenealogy _genealogy;

        public GetInheritanceSettlementsUseCase(
            ITitleRepository titleRepository,
            IFeudalStructure feudalStructure,
            IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _genealogy = genealogy;
        }

        public IReadOnlyList<string> Execute(string titleId)
        {
            var settlementIds = new List<string>();
            Title? title = _titleRepository.GetTitle(titleId);
            if (title is null) return settlementIds;
            if (!string.IsNullOrEmpty(title.SeatSettlementId)) settlementIds.Add(title.SeatSettlementId);

            string? holderClanId = _genealogy.GetHolderClanOf(title);
            if (holderClanId is null) return settlementIds;

            foreach (string heldTitleId in TitleInheritance.HeldUnder(
                         titleId, holderClanId, _feudalStructure, _titleRepository, _genealogy))
            {
                string? seatSettlementId = _titleRepository.GetTitle(heldTitleId)?.SeatSettlementId;
                if (!string.IsNullOrEmpty(seatSettlementId)) settlementIds.Add(seatSettlementId!);
            }

            return settlementIds;
        }
    }
}
