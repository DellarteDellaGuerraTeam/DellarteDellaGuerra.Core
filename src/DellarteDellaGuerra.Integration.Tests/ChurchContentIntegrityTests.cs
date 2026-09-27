using System.Xml.Linq;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;

namespace DellarteDellaGuerra.Integration.Tests;

public class ChurchContentIntegrityTests
{
    private const string ChurchConfigurationFileName = "dadg.church_settlements.xml";
    private const string MapModuleId = "DellarteDellaGuerraMap";

    [Fact]
    public void ChurchConfiguration_HasValidDioceseStructure()
    {
        var configuration = LoadChurchConfiguration();
        var foundations = configuration.Dioceses.SelectMany(diocese => diocese.Settlements).ToList();

        Assert.Equal(
            configuration.Dioceses.Count,
            configuration.Dioceses.Select(diocese => diocese.Id).Distinct().Count());
        Assert.Equal(
            foundations.Count,
            foundations.Select(foundation => foundation.Id).Distinct().Count());
        Assert.All(configuration.Dioceses, diocese =>
            Assert.Single(diocese.Settlements.Where(foundation => foundation.Kind == "Cathedral")));
        Assert.All(foundations, foundation =>
            Assert.Contains(foundation.Kind, new[] { "Cathedral", "Abbey", "Priory" }));
        Assert.InRange(foundations.Count(foundation => foundation.Shrine), 0, 1);
    }

    [Fact]
    public void ChurchConfiguration_OnlyReferencesRealVillageSettlements()
    {
        var foundations = LoadChurchConfiguration().Dioceses
            .SelectMany(diocese => diocese.Settlements)
            .ToList();
        var campaignSettlements = XDocument.Load(GetCampaignSettlementsPath())
            .Descendants("Settlement")
            .Where(element => element.Attribute("id") is not null)
            .ToDictionary(element => element.Attribute("id")!.Value);

        foreach (var foundation in foundations)
        {
            Assert.True(
                campaignSettlements.TryGetValue(foundation.Id, out var campaignSettlement),
                $"Church settlement '{foundation.Id}' does not exist in {MapModuleId}/ModuleData/settlements.xml.");
            Assert.NotNull(campaignSettlement!.Element("Components")?.Element("Village"));
        }
    }

    [Fact]
    public void ChurchConfiguration_BuildsCompleteHierarchy()
    {
        var provider = LoadChurchSettlementsProvider();
        var map = new BuildChurchMapUseCase(provider).Execute();
        var configuredIds = provider.GetChurchSettlements()
            .Select(foundation => foundation.SettlementId)
            .OrderBy(id => id)
            .ToList();
        var mappedIds = map.Roots
            .SelectMany(root => new[] { root.SettlementId }
                .Concat(root.Children.Select(child => child.SettlementId)))
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(configuredIds, mappedIds);
        Assert.All(map.Roots, root => Assert.Equal(ChurchNodeRank.See, root.Rank));
        Assert.Equal(provider.GetDioceses().Count, map.Roots.Count);
    }

    [Fact]
    public void ChurchConfiguration_MatchesIntendedReleaseContent()
    {
        var configuration = LoadChurchConfiguration();
        var foundations = configuration.Dioceses.SelectMany(diocese => diocese.Settlements).ToList();

        Assert.Equal(3, configuration.Dioceses.Count);
        Assert.Equal(16, foundations.Count);
        Assert.Equal("village_Walsingham_Abbey", Assert.Single(foundations.Where(foundation => foundation.Shrine)).Id);
    }

    private static ChurchSettlementsConfig LoadChurchConfiguration()
    {
        var serialiser = new XmlSerializer(typeof(ChurchSettlementsConfig));
        using var stream = File.OpenRead(Path.Combine(GetCoreRoot(), "config", ChurchConfigurationFileName));
        return Assert.IsType<ChurchSettlementsConfig>(serialiser.Deserialize(stream));
    }

    private static IChurchSettlementsProvider LoadChurchSettlementsProvider()
    {
        var configuration = LoadChurchConfiguration();
        var dioceses = configuration.Dioceses.Select(diocese => new ChurchDioceseData(
            diocese.Id,
            diocese.Name,
            diocese.Settlements.Select(foundation => new ChurchSettlementData(
                    foundation.Id,
                    ParseKind(foundation.Kind),
                    foundation.Shrine))
                .ToList()))
            .ToList();
        return new ChurchSettlementsProvider(dioceses);
    }

    private static ChurchSettlementKind ParseKind(string value)
    {
        Assert.True(Enum.TryParse(value, out ChurchSettlementKind kind), $"Unknown church settlement kind '{value}'.");
        return kind;
    }

    private static string GetCampaignSettlementsPath()
    {
        var modulesDirectory = Environment.GetEnvironmentVariable("DADG_MODULES_DIR");
        if (string.IsNullOrWhiteSpace(modulesDirectory))
        {
            var gameDirectory = Environment.GetEnvironmentVariable("BANNERLORD_GAME_DIR");
            if (!string.IsNullOrWhiteSpace(gameDirectory))
                modulesDirectory = Path.Combine(gameDirectory, "Modules");
        }

        if (string.IsNullOrWhiteSpace(modulesDirectory))
        {
            var siblingModulesDirectory = Directory.GetParent(GetCoreRoot())?.FullName;
            if (siblingModulesDirectory is not null &&
                Directory.Exists(Path.Combine(siblingModulesDirectory, MapModuleId)))
                modulesDirectory = siblingModulesDirectory;
        }

        Assert.False(
            string.IsNullOrWhiteSpace(modulesDirectory),
            "Could not locate the Bannerlord Modules directory. Set DADG_MODULES_DIR or BANNERLORD_GAME_DIR.");

        var path = Path.Combine(modulesDirectory!, MapModuleId, "ModuleData", "settlements.xml");
        Assert.True(File.Exists(path), $"DADG campaign settlements file was not found at '{path}'.");
        return path;
    }

    private static string GetCoreRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "config", ChurchConfigurationFileName)))
                return directory.FullName;
        }

        throw new InvalidOperationException(
            $"Could not find {ChurchConfigurationFileName} above test output '{AppContext.BaseDirectory}'.");
    }

    private sealed class ChurchSettlementsProvider : IChurchSettlementsProvider
    {
        private readonly IReadOnlyCollection<ChurchDioceseData> _dioceses;

        public ChurchSettlementsProvider(IReadOnlyCollection<ChurchDioceseData> dioceses)
        {
            _dioceses = dioceses;
        }

        public IReadOnlyCollection<ChurchSettlementData> GetChurchSettlements() =>
            _dioceses.SelectMany(diocese => diocese.Members).ToList();

        public IReadOnlyCollection<ChurchDioceseData> GetDioceses() => _dioceses;
    }
}
