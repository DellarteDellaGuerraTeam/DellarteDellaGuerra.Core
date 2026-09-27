using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    public interface ITitleRepository
    {
        Title? GetTitle(string titleId);
        Title? GetTitleBySeat(string settlementId);
        IReadOnlyList<Title> GetTitlesByClan(string clanId);
        IReadOnlyList<Title> GetAllTitles();
        void SaveTitle(Title title);

        /// <summary>The title the clan was last found to serve under, if it was ever looked up.</summary>
        string? GetPrimaryTitleId(string clanId);
        void SavePrimaryTitleId(string clanId, string titleId);
    }
}
