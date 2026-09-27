using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Donation;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Ledger;
using DellarteDellaGuerra.Domain.Church.Pilgrimage;
using DellarteDellaGuerra.Domain.Church.Port;

namespace DellarteDellaGuerra.Church.Contract.Tests;

public class ShippedChurchConfigurationTests
{
    [Fact]
    public void SettlementConfiguration_ReferencesRealCampaignVillagesExactlyOnce()
    {
        var document = ChurchConfigurationContract.LoadSettlements();
        var campaignSettlementsPath = ChurchConfigurationContract.GetCampaignSettlementsPath();
        var campaignSettlements = XDocument.Load(campaignSettlementsPath)
            .Descendants("Settlement")
            .Where(element => element.Attribute("id") is not null)
            .GroupBy(element => element.Attribute("id")!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var foundation in document.Dioceses.SelectMany(diocese => diocese.Settlements))
        {
            Assert.True(
                campaignSettlements.TryGetValue(foundation.Id, out var matches),
                $"Church foundation '{foundation.Id}' does not exist in '{campaignSettlementsPath}'.");
            Assert.True(
                matches!.Count == 1,
                $"Church foundation '{foundation.Id}' occurs {matches.Count} times in '{campaignSettlementsPath}'; expected exactly once.");
            Assert.NotNull(matches[0].Element("Components")?.Element("Village"));
        }
    }

    [Fact]
    public void SettlementConfiguration_MapsEveryDioceseAndFoundationIntoDomainHierarchy()
    {
        var document = ChurchConfigurationContract.LoadSettlements();
        var provider = ChurchConfigurationContract.CreateProvider(document);

        var map = new BuildChurchMapUseCase(provider).Execute();

        Assert.Equal(document.Dioceses.Count, map.Roots.Count);
        foreach (var configuredDiocese in document.Dioceses)
        {
            var root = Assert.Single(map.Roots, entry => entry.Id == configuredDiocese.Id);
            var configuredSeat = Assert.Single(
                configuredDiocese.Settlements,
                settlement => settlement.Kind == nameof(ChurchSettlementKind.Cathedral));
            var configuredMembers = configuredDiocese.Settlements
                .Where(settlement => settlement.Id != configuredSeat.Id)
                .ToDictionary(settlement => settlement.Id);

            Assert.Equal(configuredDiocese.Name, root.Name);
            Assert.Equal(ChurchNodeRank.See, root.Rank);
            Assert.Equal(configuredSeat.Id, root.SettlementId);
            Assert.Equal(configuredMembers.Keys.OrderBy(id => id), root.Children.Select(child => child.Id).OrderBy(id => id));
            Assert.All(root.Children, child =>
            {
                var configured = configuredMembers[child.Id];
                var expectedRank = configured.Kind == nameof(ChurchSettlementKind.Priory)
                    ? ChurchNodeRank.Priory
                    : ChurchNodeRank.Abbey;
                Assert.Equal(expectedRank, child.Rank);
                Assert.Equal(configured.Id, child.SettlementId);
            });
        }

        var configuredFoundationIds = document.Dioceses
            .SelectMany(diocese => diocese.Settlements)
            .Select(settlement => settlement.Id)
            .OrderBy(id => id);
        var mappedFoundationIds = map.Roots
            .SelectMany(root => new[] { root.SettlementId }.Concat(root.Children.Select(child => child.SettlementId)))
            .OrderBy(id => id);
        Assert.Equal(configuredFoundationIds, mappedFoundationIds);
    }

    [Fact]
    public void SettlementConfiguration_EnrichesEveryFoundationThroughLedgerPort()
    {
        var document = ChurchConfigurationContract.LoadSettlements();
        var provider = ChurchConfigurationContract.CreateProvider(document);
        var world = new RecordingLedgerWorld(provider.GetChurchSettlements());
        var useCase = new BuildChurchLedgerUseCase(new BuildChurchMapUseCase(provider), world);

        var ledger = useCase.Execute();

        var entries = Flatten(ledger.Roots).ToList();
        var configuredFoundations = provider.GetChurchSettlements().ToDictionary(foundation => foundation.SettlementId);
        Assert.Equal(configuredFoundations.Keys.OrderBy(id => id), world.RequestedSettlementIds.OrderBy(id => id));
        Assert.Equal(configuredFoundations.Count, entries.Count);
        Assert.All(entries, entry => Assert.True(entry.IsAvailable));
        Assert.All(entries, entry => Assert.Equal($"Foundation {entry.SettlementId}", entry.SettlementName));

        var shrine = Assert.Single(
            entries,
            entry => entry.Status.HasFlag(ChurchFoundationStatus.PilgrimageShrine));
        Assert.True(configuredFoundations[shrine.SettlementId].IsShrine);

        Assert.All(ledger.Roots, root => Assert.Equal(ClergyOffice.Bishop, root.ClergyOffice));
        Assert.All(
            entries.Where(entry => entry.Rank == ChurchNodeRank.Abbey),
            abbey => Assert.Equal(ClergyOffice.Abbot, abbey.ClergyOffice));
        Assert.All(
            entries.Where(entry => entry.Rank == ChurchNodeRank.Priory),
            priory => Assert.Equal(ClergyOffice.Prioress, priory.ClergyOffice));
        Assert.Equal(2f, ledger.Favour.AverageRelation);
    }

    [Fact]
    public void SettingsConfiguration_DeserializesIntoDomainSettingsAndDrivesPolicyBoundaries()
    {
        var settings = ChurchConfigurationContract.CreateSettings(ChurchConfigurationContract.LoadSettings());

        Assert.Equal(500, settings.DonationCost);
        Assert.Equal(2, settings.DonationRelation);
        Assert.Equal(1, settings.MassRelation);
        Assert.Equal(4, settings.MassMorale);
        Assert.Equal(-15, settings.SacrilegeRelationLocal);
        Assert.Equal(-5, settings.SacrilegeRelationOthers);
        Assert.Equal(2, settings.WeeklyTithePower);
        Assert.Equal(5, settings.DonationPower);
        Assert.Equal(5, settings.BlessingMorale);
        Assert.Equal(3, settings.MaxPilgrimParties);
        Assert.Equal(2, settings.PilgrimProtectionRelation);

        Assert.Equal(
            DonationOutcome.InsufficientGold,
            DonationPolicy.Evaluate(settings.DonationCost - 1, null, settings.DonationCost));
        Assert.Equal(
            DonationOutcome.Allowed,
            DonationPolicy.Evaluate(settings.DonationCost, null, settings.DonationCost));
        Assert.True(PilgrimagePolicy.ShouldSpawn(settings.MaxPilgrimParties - 1, settings.MaxPilgrimParties, 0f));
        Assert.False(PilgrimagePolicy.ShouldSpawn(settings.MaxPilgrimParties, settings.MaxPilgrimParties, 0f));
    }

    private static IEnumerable<ChurchLedgerEntry> Flatten(IEnumerable<ChurchLedgerEntry> entries)
    {
        foreach (var entry in entries)
        {
            yield return entry;
            foreach (var child in Flatten(entry.Children))
                yield return child;
        }
    }

    private sealed class RecordingLedgerWorld : IChurchLedgerWorld
    {
        private readonly IReadOnlyDictionary<string, ChurchSettlementData> _foundations;

        public RecordingLedgerWorld(IEnumerable<ChurchSettlementData> foundations)
        {
            _foundations = foundations.ToDictionary(foundation => foundation.SettlementId);
        }

        public List<string> RequestedSettlementIds { get; } = new();

        public ChurchFoundationFacts? GetFoundation(string settlementId)
        {
            RequestedSettlementIds.Add(settlementId);
            var foundation = _foundations[settlementId];
            return new ChurchFoundationFacts(
                $"Foundation {settlementId}",
                $"Clergy {settlementId}",
                foundation.Kind == ChurchSettlementKind.Priory,
                2,
                10f,
                "Owner",
                "Faction",
                foundation.IsShrine,
                false);
        }

        public IReadOnlyCollection<int> GetLivingClergyRelations() => new[] { 0, 2, 4 };
    }
}
