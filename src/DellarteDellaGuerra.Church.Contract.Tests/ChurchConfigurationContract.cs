using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Port;

namespace DellarteDellaGuerra.Church.Contract.Tests;

internal static class ChurchConfigurationContract
{
    public static ChurchSettlementsDocument LoadSettlements() =>
        Deserialize<ChurchSettlementsDocument>("dadg.church_settlements.xml");

    public static DadgConfigurationDocument LoadSettings() =>
        Deserialize<DadgConfigurationDocument>("dadg.config.xml");

    public static string GetCampaignSettlementsPath()
    {
        var attemptedPaths = new List<string>();
        foreach (var modulesDirectory in GetCandidateModulesDirectories().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var candidate = Path.Combine(
                modulesDirectory,
                "DellarteDellaGuerraMap",
                "ModuleData",
                "settlements.xml");
            attemptedPaths.Add(candidate);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException(
            "Could not locate DellarteDellaGuerraMap/ModuleData/settlements.xml. " +
            "Set DADG_MODULES_DIR or BANNERLORD_GAME_DIR. Paths tried:" +
            Environment.NewLine + string.Join(Environment.NewLine, attemptedPaths.Select(path => $"- {path}")));
    }

    public static IChurchSettlementsProvider CreateProvider(ChurchSettlementsDocument document)
    {
        var dioceses = document.Dioceses.Select(diocese => new ChurchDioceseData(
                diocese.Id,
                diocese.Name,
                diocese.Settlements.Select(settlement => new ChurchSettlementData(
                        settlement.Id,
                        ParseKind(settlement),
                        settlement.Shrine))
                    .ToList()))
            .ToList();

        return new ContractChurchSettlementsProvider(dioceses);
    }

    public static ChurchSettings CreateSettings(DadgConfigurationDocument document)
    {
        var settings = document.Church;
        return new ChurchSettings(
            settings.DonationCost,
            settings.DonationRelation,
            settings.MassRelation,
            settings.MassMorale,
            settings.SacrilegeRelationLocal,
            settings.SacrilegeRelationOthers,
            settings.WeeklyTithePower,
            settings.DonationPower,
            settings.BlessingMorale,
            settings.MaxPilgrimParties,
            settings.PilgrimProtectionRelation);
    }

    private static T Deserialize<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Shipped church configuration was not copied to '{path}'.", path);

        var serialiser = new XmlSerializer(typeof(T));
        using var stream = File.OpenRead(path);
        return serialiser.Deserialize(stream) is T document
            ? document
            : throw new InvalidDataException($"Could not deserialize shipped church configuration '{path}'.");
    }

    private static ChurchSettlementKind ParseKind(ChurchSettlementElement settlement) =>
        Enum.TryParse(settlement.Kind, out ChurchSettlementKind kind)
            ? kind
            : throw new InvalidDataException(
                $"Unknown church settlement kind '{settlement.Kind}' for '{settlement.Id}'.");

    private static IEnumerable<string> GetCandidateModulesDirectories()
    {
        var modulesDirectory = Environment.GetEnvironmentVariable("DADG_MODULES_DIR");
        if (!string.IsNullOrWhiteSpace(modulesDirectory)) yield return Path.GetFullPath(modulesDirectory);

        var gameDirectory = Environment.GetEnvironmentVariable("BANNERLORD_GAME_DIR");
        if (!string.IsNullOrWhiteSpace(gameDirectory))
            yield return Path.GetFullPath(Path.Combine(gameDirectory, "Modules"));

        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var directory = new DirectoryInfo(startPath);
                 directory is not null;
                 directory = directory.Parent)
            {
                if (directory.Name.Equals("Modules", StringComparison.OrdinalIgnoreCase))
                    yield return directory.FullName;

                if (directory.Name.Equals("DellarteDellaGuerra.Core", StringComparison.OrdinalIgnoreCase) &&
                    directory.Parent is not null)
                    yield return directory.Parent.FullName;

                var gitFile = Path.Combine(directory.FullName, ".git");
                if (!File.Exists(gitFile)) continue;

                var gitDirectory = ReadGitDirectory(gitFile);
                var gitMetadata = new DirectoryInfo(gitDirectory);
                while (gitMetadata is not null &&
                       !gitMetadata.Name.Equals(".git", StringComparison.OrdinalIgnoreCase))
                    gitMetadata = gitMetadata.Parent;

                var installedModulesDirectory = FindModulesDirectory(gitMetadata?.Parent);
                if (installedModulesDirectory is not null)
                    yield return installedModulesDirectory.FullName;
            }
        }
    }

    private static DirectoryInfo? FindModulesDirectory(DirectoryInfo? start)
    {
        for (var directory = start; directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "DellarteDellaGuerraMap")))
                return directory;
        }

        return null;
    }

    private static string ReadGitDirectory(string gitFile)
    {
        const string prefix = "gitdir:";
        var marker = File.ReadAllText(gitFile).Trim();
        if (!marker.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Worktree marker '{gitFile}' does not start with '{prefix}'.");

        var path = marker.Substring(prefix.Length).Trim();
        return Path.GetFullPath(path, Path.GetDirectoryName(gitFile)!);
    }

    private sealed class ContractChurchSettlementsProvider : IChurchSettlementsProvider
    {
        private readonly IReadOnlyCollection<ChurchDioceseData> _dioceses;

        public ContractChurchSettlementsProvider(IReadOnlyCollection<ChurchDioceseData> dioceses)
        {
            _dioceses = dioceses;
        }

        public IReadOnlyCollection<ChurchSettlementData> GetChurchSettlements() =>
            _dioceses.SelectMany(diocese => diocese.Members).ToList();

        public IReadOnlyCollection<ChurchDioceseData> GetDioceses() => _dioceses;
    }
}

[XmlRoot("ChurchSettlements")]
public sealed class ChurchSettlementsDocument
{
    [XmlElement("Diocese")]
    public List<ChurchDioceseElement> Dioceses { get; set; } = new();
}

public sealed class ChurchDioceseElement
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("ChurchSettlement")]
    public List<ChurchSettlementElement> Settlements { get; set; } = new();
}

public sealed class ChurchSettlementElement
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("kind")]
    public string Kind { get; set; } = string.Empty;

    [XmlAttribute("shrine")]
    public bool Shrine { get; set; }
}

[XmlRoot("DadgConfiguration")]
public sealed class DadgConfigurationDocument
{
    [XmlElement("ChurchConfig")]
    public ChurchSettingsElement Church { get; set; } = new();
}

public sealed class ChurchSettingsElement
{
    public int DonationCost { get; set; }
    public int DonationRelation { get; set; }
    public int MassRelation { get; set; }
    public int MassMorale { get; set; }
    public int SacrilegeRelationLocal { get; set; }
    public int SacrilegeRelationOthers { get; set; }
    public int WeeklyTithePower { get; set; }
    public int DonationPower { get; set; }
    public int BlessingMorale { get; set; }
    public int MaxPilgrimParties { get; set; }
    public int PilgrimProtectionRelation { get; set; }
}
