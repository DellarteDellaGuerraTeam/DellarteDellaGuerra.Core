using System.Reflection;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Serves <see cref="DadgContentSource.Embedded"/>: the snapshot embedded in this test assembly,
/// so the tests touch no file outside the build output and run on a machine with no game
/// installed. The title structure is linked from this repository's own config at build time; the
/// genealogy is a checked-in projection of the map module's heroes, lords and clans, carrying
/// only the fields the title system reads.
/// </summary>
internal sealed class EmbeddedContentProvider : IDadgContentProvider
{
    public Stream OpenTitles() => Open("titles.config.xml");

    public Stream OpenHeroes() => Open("dadg_heroes.xml");

    public Stream OpenCharacters() => Open("dadg_lords.xml");

    public Stream OpenClans() => Open("dadg_clans.xml");

    private static Stream Open(string fileName)
    {
        Assembly assembly = typeof(EmbeddedContentProvider).Assembly;

        return assembly.GetManifestResourceStream($"DadgContent.{fileName}")
               ?? throw new InvalidOperationException(
                   $"'{fileName}' is not embedded in {assembly.GetName().Name}.");
    }
}
