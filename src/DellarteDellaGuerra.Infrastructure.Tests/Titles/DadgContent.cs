namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Opens the 1471 content the content-integration tests read, from whichever
/// <see cref="DadgContentSource"/> the environment selects.
/// </summary>
/// <remarks>
/// Both sources are expected to produce the same numbers, because the embedded snapshot carries
/// every field the title system reads off the live files and nothing else. The day the two
/// disagree, the snapshot has fallen behind the shipped content.
/// </remarks>
internal static class DadgContent
{
    public const string SourceVariable = "DADG_CONTENT_SOURCE";

    private static readonly IDadgContentProvider Provider = CreateProvider(SelectedSource());

    public static Stream OpenTitles() => Provider.OpenTitles();

    public static Stream OpenHeroes() => Provider.OpenHeroes();

    public static Stream OpenCharacters() => Provider.OpenCharacters();

    public static Stream OpenClans() => Provider.OpenClans();

    private static DadgContentSource SelectedSource()
    {
        string? requested = Environment.GetEnvironmentVariable(SourceVariable);
        if (string.IsNullOrWhiteSpace(requested)) return DadgContentSource.Module;

        // Only the declared names are accepted. TryParse would also take "1" for Embedded, and a
        // number that lands on a source by accident is a typo the run should stop for, not honour.
        foreach (DadgContentSource source in Enum.GetValues<DadgContentSource>())
        {
            if (string.Equals(source.ToString(), requested.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }
        }

        throw new InvalidOperationException(
            $"{SourceVariable} must be one of "
            + $"{string.Join(", ", Enum.GetNames<DadgContentSource>())}, not '{requested}'.");
    }

    // Which content a run read decides what its numbers are worth, and the variable that picks it
    // is easy to leave set in a shell without noticing, so every run says out loud where it read
    // from and how to ask for the other source.
    private static IDadgContentProvider CreateProvider(DadgContentSource source)
    {
        if (source == DadgContentSource.Embedded)
        {
            Console.WriteLine(
                $"[DADG content] {SourceVariable}={DadgContentSource.Embedded}: reading the 1471 content from "
                + $"the snapshot embedded in this test assembly. Set {SourceVariable}="
                + $"{DadgContentSource.Module} to read the live files from the DellarteDellaGuerraMap "
                + "module instead.");

            return new EmbeddedContentProvider();
        }

        var provider = new MapModuleContentProvider();

        Console.WriteLine(
            $"[DADG content] {SourceVariable}={DadgContentSource.Module} (the default): reading the 1471 "
            + $"genealogy from the DellarteDellaGuerraMap module at '{provider.MapDataRoot}'. Set "
            + $"{SourceVariable}={DadgContentSource.Embedded} to read the snapshot embedded in this test "
            + "assembly instead, which needs no game installation.");

        return provider;
    }
}
