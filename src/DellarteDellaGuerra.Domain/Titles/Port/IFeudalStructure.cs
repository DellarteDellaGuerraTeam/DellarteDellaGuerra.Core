using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    /// <summary>
    /// Describes the de jure feudal hierarchy (barony, county, duchy, kingdom) as read from
    /// configuration, and any title since reattached to another realm.
    /// </summary>
    public interface IFeudalStructure
    {
        string? GetDeJureSuzerainTitleId(string titleId);
        IReadOnlyList<string> GetDeJureVassalTitleIds(string titleId);
        string? GetTitleIdBySeat(string settlementId);
        IReadOnlyList<string> GetAllTitleIds();
        TitleRank? GetRank(string titleId);
        string? GetTitleName(string titleId);

        /// <summary>
        /// Moves the title, and everything below it, under another suzerain title, or makes it a
        /// root when the suzerain is null.
        /// </summary>
        void Reattach(string titleId, string? suzerainTitleId);
    }
}
