using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.PoliticalMap.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.PoliticalMap.Api.Campaign
{
    public sealed record PoliticalMapSurface(PoliticalMapGrid Grid, PoliticalMapSheet Sheet);

    /**
     * <summary>
     *  Builds the political map's settlement grid when a campaign session launches: every land
     *  cell of the map goes to its nearest town, castle or village over land. The settlements are
     *  read here on the main thread; the terrain is read and the grid built on a worker thread, so
     *  the map loads without waiting for it (about half a second over the 1471 map).
     * </summary>
     * <remarks>
     *  Nothing is shipped or cached: the grid always follows the map and the settlements the
     *  player actually has. Until it is ready, and if it fails, <see cref="Grid"/> is null and the
     *  overlay has nothing to paint.
     * </remarks>
     */
    public class PoliticalMapGridCampaignBehavior : CampaignBehaviorBase
    {
        private readonly IPoliticalMapTerrain _terrain;
        private readonly ILogger _logger;

        private readonly object _sessionLock = new();
        private volatile PoliticalMapSurface? _surface;
        private int _session;

        public PoliticalMapGridCampaignBehavior(IPoliticalMapTerrain terrain, ILoggerFactory loggerFactory)
        {
            _terrain = terrain;
            _logger = loggerFactory.CreateLogger<PoliticalMapGridCampaignBehavior>();
        }

        /// <summary>The grid of the running campaign, or null while it is still being built.</summary>
        public PoliticalMapGrid? Grid => _surface?.Grid;

        /// <summary>The surface the overlay is drawn on, ready with <see cref="Grid"/>.</summary>
        public PoliticalMapSheet? Sheet => _surface?.Sheet;

        public PoliticalMapSurface? Surface => _surface;

        /// <summary>The build in flight or done, for a caller that would rather wait than poll.</summary>
        public Task<PoliticalMapGrid?> GridTask { get; private set; } = Task.FromResult<PoliticalMapGrid?>(null);

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter) =>
            StartSession(Settlement.All
                .Where(settlement => settlement.IsTown || settlement.IsCastle || settlement.IsVillage)
                .Select(settlement =>
                {
                    Vec2 position = settlement.GetPosition2D;
                    return new PoliticalMapSeed(settlement.StringId, position.x, position.y);
                }));

        internal void StartSession(IEnumerable<PoliticalMapSeed> settlements)
        {
            int session;
            lock (_sessionLock)
            {
                session = ++_session;
                _surface = null;
            }
            var seeds = settlements.ToList();
            GridTask = Task.Run(() => Build(seeds, session));
        }

        private PoliticalMapGrid? Build(IReadOnlyList<PoliticalMapSeed> seeds, int session)
        {
            try
            {
                var clock = Stopwatch.StartNew();
                PoliticalMapHeightField terrain = _terrain.Load();
                long terrainMs = clock.ElapsedMilliseconds;
                PoliticalMapGrid grid = PoliticalMapGridBuilder.Build(terrain, seeds);
                long gridMs = clock.ElapsedMilliseconds - terrainMs;
                var sheet = new PoliticalMapSheet(terrain, grid);
                lock (_sessionLock)
                {
                    if (session != _session) return null;
                    _surface = new PoliticalMapSurface(grid, sheet);
                }
                _logger.Info(
                    $"Political map grid built: {grid.Width}x{grid.Height} cells over {seeds.Count} settlements, "
                    + $"terrain read in {terrainMs} ms, grid in {gridMs} ms, "
                    + $"sheet in {clock.ElapsedMilliseconds - terrainMs - gridMs} ms.");
                return grid;
            }
            catch (Exception exception)
            {
                _logger.Error("Could not build the political map grid; the political map stays blank.", exception);
                return null;
            }
        }
    }
}
