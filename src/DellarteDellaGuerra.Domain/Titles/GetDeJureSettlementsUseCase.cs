using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /// <summary>
    /// Flattens a title to the settlements it covers de jure: its own seat plus the seat of
    /// every title below it in the hierarchy. A war over a duchy is fought for the duchy's
    /// counties and baronies too, so this is the set the main goal is picked from.
    /// </summary>
    public class GetDeJureSettlementsUseCase : IGetDeJureSettlementsUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;

        public GetDeJureSettlementsUseCase(ITitleRepository titleRepository, IFeudalStructure feudalStructure)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
        }

        public IReadOnlyList<string> Execute(string titleId)
        {
            var settlementIds = new List<string>();
            Collect(titleId, settlementIds);
            return settlementIds;
        }

        private void Collect(string titleId, List<string> settlementIds)
        {
            // A title configured without a seat, or absent from the repository entirely, still
            // has vassals worth walking.
            string? seatSettlementId = _titleRepository.GetTitle(titleId)?.SeatSettlementId;
            if (!string.IsNullOrEmpty(seatSettlementId)) settlementIds.Add(seatSettlementId!);

            foreach (string vassalTitleId in _feudalStructure.GetDeJureVassalTitleIds(titleId))
            {
                Collect(vassalTitleId, settlementIds);
            }
        }
    }
}
