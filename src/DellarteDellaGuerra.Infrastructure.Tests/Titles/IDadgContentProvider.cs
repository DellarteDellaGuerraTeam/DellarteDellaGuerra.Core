namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Supplies the four documents the content-integration tests read. One implementation per
/// <see cref="DadgContentSource"/>.
/// </summary>
internal interface IDadgContentProvider
{
    Stream OpenTitles();

    Stream OpenHeroes();

    Stream OpenCharacters();

    Stream OpenClans();
}
