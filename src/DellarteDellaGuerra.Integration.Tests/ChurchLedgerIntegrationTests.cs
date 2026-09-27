using System;
using DellarteDellaGuerra.Church;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Favour;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Ledger;
using DellarteDellaGuerra.Domain.Church.Port;
using DellarteDellaGuerra.Integration.DI;
using Microsoft.Extensions.DependencyInjection;

namespace DellarteDellaGuerra.Integration.Tests;

public class ChurchLedgerIntegrationTests
{
    [Fact]
    public void Container_ResolvesChurchLedgerBoundaryAsSingletons()
    {
        var provider = new DadgServiceContainer().Build();
        try
        {
            var world = provider.GetRequiredService<IChurchLedgerWorld>();
            var useCase = provider.GetRequiredService<IBuildChurchLedgerUseCase>();

            Assert.IsType<BannerlordChurchLedgerWorld>(world);
            Assert.IsType<BuildChurchLedgerUseCase>(useCase);
            Assert.Same(world, provider.GetRequiredService<IChurchLedgerWorld>());
            Assert.Same(useCase, provider.GetRequiredService<IBuildChurchLedgerUseCase>());
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public void Ledger_WithoutCampaign_PreservesConfiguredHierarchyAndMarksLiveFactsUnavailable()
    {
        var settlements = new StubChurchSettlementsProvider(
            new ChurchDioceseData(
                "see_ely",
                "See of Ely",
                new List<ChurchSettlementData>
                {
                    new("village_Ely_Cathedral", ChurchSettlementKind.Cathedral),
                    new("village_Battle_Abbey", ChurchSettlementKind.Abbey)
                }));
        var world = new BannerlordChurchLedgerWorld(new ChurchSettlements(settlements));
        var useCase = new BuildChurchLedgerUseCase(new BuildChurchMapUseCase(settlements), world);

        var ledger = useCase.Execute();

        var see = Assert.Single(ledger.Roots);
        Assert.Equal("See of Ely", see.Name);
        Assert.False(see.IsAvailable);
        Assert.False(Assert.Single(see.Children).IsAvailable);
        Assert.Equal(0f, ledger.Favour.AverageRelation);
        Assert.Equal(ChurchFavourRank.Indifferent, ledger.Favour.Rank);
    }

    private class StubChurchSettlementsProvider : IChurchSettlementsProvider
    {
        private readonly IReadOnlyCollection<ChurchDioceseData> _dioceses;

        public StubChurchSettlementsProvider(params ChurchDioceseData[] dioceses)
        {
            _dioceses = dioceses;
        }

        public IReadOnlyCollection<ChurchSettlementData> GetChurchSettlements() =>
            _dioceses.SelectMany(diocese => diocese.Members).ToList();

        public IReadOnlyCollection<ChurchDioceseData> GetDioceses() => _dioceses;
    }
}
