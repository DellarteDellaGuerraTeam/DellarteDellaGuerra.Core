using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    /// <summary>
    /// Describes the static de jure feudal hierarchy (barony, county, duchy, kingdom)
    /// as read from configuration.
    /// </summary>
    public interface IFeudalStructure
    {
        string? GetDeJureSuzerainTitleId(string titleId);
        IReadOnlyList<string> GetDeJureVassalTitleIds(string titleId);
        string? GetTitleIdBySeat(string settlementId);
        IReadOnlyList<string> GetAllTitleIds();
        TitleRank? GetRank(string titleId);
        string? GetTitleName(string titleId);
    }
}
