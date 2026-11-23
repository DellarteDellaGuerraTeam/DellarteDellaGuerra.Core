using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap
{
    /// <summary>One mesh chunk of a territory to rebuild this frame.</summary>
    public record PoliticalMapChunk(string TerritoryId, int ChunkIndex, int ChunkCount);

    /// <summary>
    /// Spreads the political map's rebuilds across frames. Campaign events only mark territories
    /// dirty; the overlay calls <see cref="Tick"/> once a frame and rebuilds the chunks it is handed.
    /// </summary>
    /// <remarks>
    /// Building a mesh runs synchronously on the main thread at about 9 µs a triangle, and a chunk
    /// holds up to 512 triangles, so a chunk costs up to about 4.6 ms. A county on the 1 m grid
    /// spans a dozen or more chunks, which is why the budget is counted in chunks rather than in
    /// territories: one county a frame would be a visible hitch. A territory is restarted at most
    /// once per <c>minSecondsBetweenRebuilds</c> of real time, so a burst of events (a death passing
    /// a dozen titles, a siege moving a castle and its title together) repaints it once. Marks are
    /// deduplicated and served in the order they first arrived.
    /// </remarks>
    public class PoliticalMapRefreshScheduler
    {
        public const int DefaultChunksPerFrame = 1;
        public const double DefaultMinSecondsBetweenRebuilds = 1.0;

        private readonly int _chunksPerFrame;
        private readonly double _minSecondsBetweenRebuilds;
        private readonly List<string> _pending = new();
        private readonly HashSet<string> _pendingSet = new();
        private readonly Dictionary<string, double> _lastStartedAt = new();

        private string? _activeTerritoryId;
        private int _activeNextChunk;
        private int _activeChunkCount;

        public PoliticalMapRefreshScheduler(
            int chunksPerFrame = DefaultChunksPerFrame,
            double minSecondsBetweenRebuilds = DefaultMinSecondsBetweenRebuilds)
        {
            if (chunksPerFrame < 1) throw new ArgumentOutOfRangeException(nameof(chunksPerFrame));
            _chunksPerFrame = chunksPerFrame;
            _minSecondsBetweenRebuilds = minSecondsBetweenRebuilds;
        }

        /// <summary>True while a territory is pending or part way through its rebuild.</summary>
        public bool HasWork => _activeTerritoryId is not null || _pending.Count > 0;

        /// <summary>The territories waiting to be rebuilt, oldest mark first.</summary>
        public IReadOnlyList<string> Pending => _pending;

        /// <summary>
        /// Queues the territory. One already pending keeps its place; one part way through its
        /// rebuild is queued again, because its finished chunks were painted before the change.
        /// </summary>
        public void MarkDirty(string territoryId)
        {
            if (_pendingSet.Add(territoryId)) _pending.Add(territoryId);
        }

        public void MarkDirty(IEnumerable<string> territoryIds)
        {
            foreach (string territoryId in territoryIds) MarkDirty(territoryId);
        }

        /// <summary>Drops all queued work, for when the overlay has just been built whole.</summary>
        public void Clear()
        {
            _pending.Clear();
            _pendingSet.Clear();
            _activeTerritoryId = null;
        }

        /// <summary>
        /// The chunks to rebuild this frame, at most the per-frame budget. The current territory is
        /// finished before the next pending one starts. <paramref name="chunkCountOf"/> is asked once,
        /// when a territory starts; a territory with no chunks is passed over without using budget.
        /// </summary>
        public IReadOnlyList<PoliticalMapChunk> Tick(double nowSeconds, Func<string, int> chunkCountOf)
        {
            var chunks = new List<PoliticalMapChunk>();
            while (chunks.Count < _chunksPerFrame)
            {
                if (_activeTerritoryId is null && !StartNext(nowSeconds, chunkCountOf)) break;

                chunks.Add(new PoliticalMapChunk(_activeTerritoryId!, _activeNextChunk, _activeChunkCount));
                if (++_activeNextChunk >= _activeChunkCount) _activeTerritoryId = null;
            }

            return chunks;
        }

        private bool StartNext(double nowSeconds, Func<string, int> chunkCountOf)
        {
            for (int index = 0; index < _pending.Count; index++)
            {
                string territoryId = _pending[index];
                if (_lastStartedAt.TryGetValue(territoryId, out double startedAt)
                    && nowSeconds - startedAt < _minSecondsBetweenRebuilds)
                {
                    continue;
                }

                _pending.RemoveAt(index);
                _pendingSet.Remove(territoryId);
                _lastStartedAt[territoryId] = nowSeconds;

                int chunkCount = chunkCountOf(territoryId);
                if (chunkCount <= 0)
                {
                    index--;
                    continue;
                }

                _activeTerritoryId = territoryId;
                _activeNextChunk = 0;
                _activeChunkCount = chunkCount;
                return true;
            }

            return false;
        }
    }
}
