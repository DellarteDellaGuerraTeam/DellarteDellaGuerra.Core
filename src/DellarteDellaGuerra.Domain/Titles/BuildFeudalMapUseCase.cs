using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /// <summary>
    /// Builds the feudal map by walking the de jure structure and overlaying the current
    /// holders from the title repository. Titles missing from the repository are rendered
    /// as vacant with an empty seat.
    /// </summary>
    public class BuildFeudalMapUseCase : IBuildFeudalMapUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;

        public BuildFeudalMapUseCase(ITitleRepository titleRepository, IFeudalStructure feudalStructure)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
        }

        public FeudalMap Execute()
        {
            var realms = _feudalStructure
                .GetAllTitleIds()
                .Where(titleId => _feudalStructure.GetDeJureSuzerainTitleId(titleId) is null)
                .Select(BuildEntry)
                .ToList();

            return new FeudalMap(realms);
        }

        private FeudalMapEntry BuildEntry(string titleId)
        {
            Title? title = _titleRepository.GetTitle(titleId);
            var vassals = _feudalStructure
                .GetDeJureVassalTitleIds(titleId)
                .Select(BuildEntry)
                .ToList();

            return new FeudalMapEntry(
                titleId,
                _feudalStructure.GetTitleName(titleId) ?? titleId,
                _feudalStructure.GetRank(titleId) ?? TitleRank.Baron,
                title?.SeatSettlementId ?? string.Empty,
                title?.HolderClanId,
                vassals,
                title is { IsContested: true } ? title.OccupantClanId : null);
        }
    }
}
