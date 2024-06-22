using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Infrastructure.Cache
{
    public class CacheCampaignBehaviour : CampaignBehaviorBase, ICacheProvider
    {
        private readonly Dictionary<CachedEvent, Stack<(string, Func<object>)>> _cacheRequestsByEvent = new();

        private readonly Dictionary<CachedEvent, Stack<Action>> _invalidationRequestsByEvent =
            new();
        private readonly Dictionary<string, object> _cache = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => OnSessionLaunched());
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, _ => OnAfterSessionLaunched());
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public string CacheObject(object cacheObject)
        {
            var id = GenerateCachedObjectId();
            _cache.Add(id, cacheObject);
            return id;
        }

        public string CacheObject(Func<object> cacheRequest, CachedEvent cachedEvent)
        {
            if (!_cacheRequestsByEvent.ContainsKey(cachedEvent))
                _cacheRequestsByEvent[cachedEvent] = new Stack<(string, Func<object>)>();

            var id = GenerateCachedObjectId();
            _cacheRequestsByEvent[cachedEvent].Push((id, cacheRequest));

            return id;
        }

        public T? GetCachedObject<T>(string id)
        {
            if (id is null || !_cache.ContainsKey(id)) return default;
            return (T)_cache[id];
        }

        public void InvalidateCache(string id)
        {
            if (id is null) return;
            _cache.Remove(id);
        }

        public void InvalidateCache(string id, CachedEvent cachedEvent)
        {
            if (id is null) return;
            if (!_invalidationRequestsByEvent.ContainsKey(cachedEvent))
                _invalidationRequestsByEvent[cachedEvent] = new Stack<Action>();

            _invalidationRequestsByEvent[cachedEvent].Push(() => InvalidateCache(id));
        }

        public void InvalidateCache()
        {
            _invalidationRequestsByEvent.Clear();
            _cacheRequestsByEvent.Clear();
            _cache.Clear();
        }

        private void ExecuteCacheRequests(CachedEvent cachedEvent)
        {
            if (!_cacheRequestsByEvent.ContainsKey(cachedEvent)) return;

            while (_cacheRequestsByEvent[cachedEvent].Count > 0)
            {
                var cacheRequest = _cacheRequestsByEvent[cachedEvent].Pop();
                _cache.Add(cacheRequest.Item1, cacheRequest.Item2());
            }
        }

        private void ExecuteInvalidationRequests(CachedEvent cachedEvent)
        {
            if (!_invalidationRequestsByEvent.ContainsKey(cachedEvent)) return;

            while (_invalidationRequestsByEvent[cachedEvent].Count > 0)
                _invalidationRequestsByEvent[cachedEvent].Pop()();
        }

        private void OnSessionLaunched()
        {
            ExecuteInvalidationRequests(CachedEvent.OnSessionLaunched);
            ExecuteCacheRequests(CachedEvent.OnSessionLaunched);
        }

        private void OnAfterSessionLaunched()
        {
            ExecuteInvalidationRequests(CachedEvent.OnAfterSessionLaunched);
            ExecuteCacheRequests(CachedEvent.OnAfterSessionLaunched);
        }

        private string GenerateCachedObjectId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}