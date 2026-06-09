using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IGetSuzerainUseCase
    {
        string? Execute(string clanId);
        TitleRank? GetHighestRank(string clanId);
    }
}
