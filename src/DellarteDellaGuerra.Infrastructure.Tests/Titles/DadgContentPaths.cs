namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Locates the shipped 1471 content the content-integration tests read: the title structure
/// from this repository, and the heroes, characters and clans from the sibling
/// DellarteDellaGuerraMap module.
/// </summary>
internal sealed record DadgContentPaths(
    string Titles,
    string Heroes,
    string Characters,
    string Clans)
{
    public static DadgContentPaths Find()
    {
        DirectoryInfo repositoryRoot = FindAncestorContaining(
            AppContext.BaseDirectory,
            Path.Combine("config", "titles.config.xml"));
        string? configuredModulesRoot = Environment.GetEnvironmentVariable("DADG_MODULES_ROOT");
        DirectoryInfo modulesRoot = configuredModulesRoot is null
            ? FindAncestorNamed(repositoryRoot, "Modules")
            : new DirectoryInfo(configuredModulesRoot);
        string mapDataRoot = Path.Combine(modulesRoot.FullName, "DellarteDellaGuerraMap", "ModuleData");

        return new DadgContentPaths(
            RequireFile(Path.Combine(repositoryRoot.FullName, "config", "titles.config.xml")),
            RequireFile(Path.Combine(mapDataRoot, "heroes", "dadg_heroes.xml")),
            RequireFile(Path.Combine(mapDataRoot, "characters", "dadg_lords.xml")),
            RequireFile(Path.Combine(mapDataRoot, "clans", "dadg_clans.xml")));
    }

    private static DirectoryInfo FindAncestorContaining(string startPath, string relativePath)
    {
        for (DirectoryInfo? directory = new DirectoryInfo(startPath); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, relativePath))) return directory;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate a repository containing '{relativePath}' above '{startPath}'.");
    }

    private static DirectoryInfo FindAncestorNamed(DirectoryInfo start, string name)
    {
        for (DirectoryInfo? directory = start; directory is not null; directory = directory.Parent)
        {
            if (string.Equals(directory.Name, name, StringComparison.OrdinalIgnoreCase)) return directory;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the '{name}' directory above '{start.FullName}'.");
    }

    private static string RequireFile(string path) =>
        File.Exists(path)
            ? path
            : throw new FileNotFoundException(
                "The DADG content integration test requires the sibling DellarteDellaGuerraMap module.",
                path);
}
