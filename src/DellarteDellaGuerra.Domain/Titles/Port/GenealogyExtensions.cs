using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    public static class GenealogyExtensions
    {
        public static string? GetHolderClanOf(this IGenealogy genealogy, Title? title) =>
            title?.HolderHeroId is { } heroId ? genealogy.GetClanOf(heroId) : null;
    }
}
