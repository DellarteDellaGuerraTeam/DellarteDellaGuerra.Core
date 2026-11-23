using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Port;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;
using Microsoft.Extensions.Logging.Abstractions;

namespace DellarteDellaGuerra.Integration.Tests.PoliticalMap;

public class PoliticalMapSessionTests
{
    [Fact]
    public async Task LoadingAnotherSaveKeepsItsGridAndSheetWhenTheOlderBuildFinishesLast()
    {
        var terrain = new OverlappingTerrainLoads();
        var behavior = new PoliticalMapGridCampaignBehavior(terrain, new LoggerFactory(NullLoggerFactory.Instance));
        behavior.StartSession(new[] { new PoliticalMapSeed("older", 5, 5) });
        var older = behavior.GridTask;
        try
        {
            await terrain.OlderStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            behavior.StartSession(new[] { new PoliticalMapSeed("current", 5, 5) });
            var current = await behavior.GridTask.WaitAsync(TimeSpan.FromSeconds(10));
            var surface = Assert.IsType<PoliticalMapSurface>(behavior.Surface);
            Assert.Same(current, surface.Grid);
            Assert.Equal("current", surface.Grid.SettlementAt(5, 5));
            Assert.Equal(30f, surface.Sheet.HeightAt(5, 5));

            terrain.FinishOlder.SetResult();
            Assert.Null(await older.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Same(surface, behavior.Surface);
        }
        finally
        {
            terrain.FinishOlder.TrySetResult();
        }
    }

    private sealed class OverlappingTerrainLoads : IPoliticalMapTerrain
    {
        private int _loads;
        public TaskCompletionSource OlderStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FinishOlder { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PoliticalMapHeightField Load()
        {
            bool older = Interlocked.Increment(ref _loads) == 1;
            if (older)
            {
                OlderStarted.SetResult();
                FinishOlder.Task.GetAwaiter().GetResult();
            }
            float height = older ? 20 : 30;
            return new PoliticalMapHeightField(2, new ushort[4], height, height, 12);
        }
    }
}
