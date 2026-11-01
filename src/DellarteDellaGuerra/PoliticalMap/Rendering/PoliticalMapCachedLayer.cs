using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.PoliticalMap.Rendering
{
    /// <summary>One retained map level. Only geometry planning runs on a worker; engine calls stay on the map thread.</summary>
    internal sealed class PoliticalMapCachedLayer
    {
        private readonly PoliticalMapMesher _mesher;
        private readonly PoliticalMapMeshPlan _plan;
        private readonly PoliticalMapRefreshScheduler _scheduler = new();
        private readonly PoliticalMapRenderer _renderer;
        private readonly ILogger _logger;
        private readonly string _name;
        private Task<(Dictionary<string, IReadOnlyList<PoliticalMapMeshChunk>> Meshes, double Milliseconds)>? _planning;
        private PoliticalMapMeshRequest? _planningRequest;
        private PoliticalMapMeshRequest? _uploadRequest;
        private Dictionary<string, IReadOnlyList<PoliticalMapMeshChunk>>? _meshes;
        private int _failedRevision = -1;
        private Stopwatch? _rebuild;
        private int _frames, _chunks;
        private double _planningMs, _uploadMs, _slowestFrameMs;

        public PoliticalMapCachedLayer(TaleWorlds.Engine.Scene scene, PoliticalMapGrid grid, PoliticalMapSheet sheet,
            string[] territories, IReadOnlyList<(int A, int B)> neighbours, ILogger logger, string name)
        {
            _mesher = new PoliticalMapMesher(grid, territories);
            _plan = new PoliticalMapMeshPlan(territories, neighbours);
            _renderer = new PoliticalMapRenderer(scene, grid, sheet);
            _renderer.SetVisibility(PoliticalMapZoomBand.Mid, false);
            _logger = logger;
            _name = name;
        }

        public bool IsReady => _plan.IsReady;
        public void SetVisibility(PoliticalMapZoomBand band, bool visible) => _renderer.SetVisibility(band, visible);

        public void Repaint(PoliticalMapSettlementPaint[] paints)
        {
            if (!_plan.Update(paints)) return;
            if (_uploadRequest is not null)
                foreach (string territory in _uploadRequest.Territories) _renderer.Discard(territory);
            _scheduler.Clear();
            _meshes = null;
            _uploadRequest = null;
            _rebuild = null;
            _frames = _chunks = 0;
            _planningMs = _uploadMs = _slowestFrameMs = 0;
        }

        public void Drain(double clock, double budgetMs)
        {
            if (IsReady) return;
            var frame = Stopwatch.StartNew();
            _frames++;
            if (_planning is not null)
            {
                if (!_planning.IsCompleted) return;
                var completed = _planning;
                PoliticalMapMeshRequest request = _planningRequest!;
                _planning = null;
                _planningRequest = null;
                if (completed.IsFaulted)
                {
                    _failedRevision = request.Revision;
                    _logger.Error($"Political map geometry planning failed for {_name}; the last completed map is kept.", completed.Exception!);
                    return;
                }
                if (_plan.IsCurrent(request))
                {
                    _meshes = completed.Result.Meshes;
                    _planningMs = completed.Result.Milliseconds;
                    _uploadRequest = request;
                    _scheduler.MarkDirty(request.Territories);
                }
            }

            if (_meshes is null)
            {
                PoliticalMapMeshRequest request = _plan.Capture();
                if (request.Revision == _failedRevision || request.Revision == 0) return;
                _rebuild ??= Stopwatch.StartNew();
                _planningRequest = request;
                // One worker per layer: its mesher never crosses threads concurrently.
                _planning = Task.Run(() =>
                {
                    var watch = Stopwatch.StartNew();
                    var meshes = request.Territories.ToDictionary(territory => territory, territory => _mesher.Build(territory, request.Paints));
                    return (meshes, watch.Elapsed.TotalMilliseconds);
                });
                return;
            }

            while (frame.Elapsed.TotalMilliseconds < budgetMs)
            {
                IReadOnlyList<PoliticalMapChunk> chunks = _scheduler.Tick(clock, territory => _meshes[territory].Count);
                if (chunks.Count == 0) break;
                foreach (PoliticalMapChunk chunk in chunks)
                {
                    double before = frame.Elapsed.TotalMilliseconds;
                    _renderer.AddChunk(chunk.TerritoryId, chunk.ChunkIndex, _meshes[chunk.TerritoryId][chunk.ChunkIndex]);
                    _uploadMs += frame.Elapsed.TotalMilliseconds - before;
                    _chunks++;
                }
            }

            if (!_scheduler.HasWork)
            {
                // All neighbours swap together; an empty territory loses its old mesh too.
                foreach (string territory in _uploadRequest!.Territories) _renderer.Commit(territory);
                _plan.Complete(_uploadRequest);
                _meshes = null;
                _uploadRequest = null;
                _slowestFrameMs = Math.Max(_slowestFrameMs, frame.Elapsed.TotalMilliseconds);
                _logger.Info($"Political map cache ready ({_name}): {_chunks} chunks in {_rebuild?.ElapsedMilliseconds ?? 0} ms over {_frames} frames; "
                    + $"worker planning {_planningMs:F1} ms, mesh uploads {_uploadMs:F1} ms, slowest map frame {_slowestFrameMs:F1} ms.");
                _rebuild = null;
                _frames = _chunks = 0;
                _planningMs = _uploadMs = _slowestFrameMs = 0;
            }
            else _slowestFrameMs = Math.Max(_slowestFrameMs, frame.Elapsed.TotalMilliseconds);
        }
    }
}
