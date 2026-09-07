namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Serves <see cref="DadgContentSource.Module"/>: the genealogy read straight out of the sibling
/// DellarteDellaGuerraMap module, which is not part of this repository. Running the same
/// assertions against the live files is what proves the embedded snapshot still matches the
/// shipped content. If this provider fails where <see cref="EmbeddedContentProvider"/> passes,
/// the two have drifted apart and the snapshot needs regenerating.
/// </summary>
/// <remarks>
/// The title structure is not in that module. It lives in this repository, and the embedded copy
/// is linked from it at build time, so it is the same document either way and is taken from the
/// snapshot here rather than resolved a second way.
/// </remarks>
internal sealed class MapModuleContentProvider : IDadgContentProvider
{
    public const string ModulesRootVariable = "DADG_MODULES_ROOT";

    private readonly EmbeddedContentProvider _titles = new();

    public MapModuleContentProvider()
    {
        string? configuredModulesRoot = Environment.GetEnvironmentVariable(ModulesRootVariable);
        DirectoryInfo modulesRoot = string.IsNullOrWhiteSpace(configuredModulesRoot)
            ? FindAncestorNamed(new DirectoryInfo(AppContext.BaseDirectory), "Modules")
            : new DirectoryInfo(configuredModulesRoot);

        MapDataRoot = Path.Combine(modulesRoot.FullName, "DellarteDellaGuerraMap", "ModuleData");
    }

    /// <summary>The module folder these tests ended up reading, which the run logs.</summary>
    public string MapDataRoot { get; }

    public Stream OpenTitles() => _titles.OpenTitles();

    public Stream OpenHeroes() => Open("heroes", "dadg_heroes.xml");

    public Stream OpenCharacters() => Open("characters", "dadg_lords.xml");

    public Stream OpenClans() => Open("clans", "dadg_clans.xml");

    private Stream Open(string folder, string fileName)
    {
        string path = Path.Combine(MapDataRoot, folder, fileName);

        return File.Exists(path)
            ? File.OpenRead(path)
            : throw new FileNotFoundException(
                "The sibling DellarteDellaGuerraMap module is not where this test expected it. "
                + $"Point {ModulesRootVariable} at the Bannerlord Modules directory, or set "
                + $"{DadgContent.SourceVariable}={DadgContentSource.Embedded} to run against the "
                + "embedded snapshot instead.",
                path);
    }

    private static DirectoryInfo FindAncestorNamed(DirectoryInfo start, string name)
    {
        for (DirectoryInfo? directory = start; directory is not null; directory = directory.Parent)
        {
            if (string.Equals(directory.Name, name, StringComparison.OrdinalIgnoreCase)) return directory;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate a '{name}' directory above '{start.FullName}'. "
            + $"Set {ModulesRootVariable} to the Bannerlord Modules directory instead.");
    }
}
