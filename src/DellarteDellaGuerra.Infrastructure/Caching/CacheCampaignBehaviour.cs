using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Infrastructure.Caching
{
    public class CacheCampaignBehaviour : CampaignBehaviorBase, ICacheProvider
    {
        private readonly Dictionary<CampaignEvent, Stack<(string, Func<object>)>> _cacheRequestsByEvent = new();

        private readonly Dictionary<CampaignEvent, Stack<Action>> _invalidationRequestsByEvent =
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

        public string CacheObject(Func<object> cacheRequest, CampaignEvent campaignEvent)
        {
            if (!_cacheRequestsByEvent.ContainsKey(campaignEvent))
                _cacheRequestsByEvent[campaignEvent] = new Stack<(string, Func<object>)>();

            var id = GenerateCachedObjectId();
            _cacheRequestsByEvent[campaignEvent].Push((id, cacheRequest));

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

        public void InvalidateCache(string id, CampaignEvent campaignEvent)
        {
            if (id is null) return;
            if (!_invalidationRequestsByEvent.ContainsKey(campaignEvent))
                _invalidationRequestsByEvent[campaignEvent] = new Stack<Action>();

            _invalidationRequestsByEvent[campaignEvent].Push(() => InvalidateCache(id));
        }

        public void InvalidateCache()
        {
            _invalidationRequestsByEvent.Clear();
            _cacheRequestsByEvent.Clear();
            _cache.Clear();
        }

        private void ExecuteCacheRequests(CampaignEvent campaignEvent)
        {
            if (!_cacheRequestsByEvent.ContainsKey(campaignEvent)) return;

            while (_cacheRequestsByEvent[campaignEvent].Count > 0)
            {
                var cacheRequest = _cacheRequestsByEvent[campaignEvent].Pop();
                _cache.Add(cacheRequest.Item1, cacheRequest.Item2());
            }
        }

        private void ExecuteInvalidationRequests(CampaignEvent campaignEvent)
        {
            if (!_invalidationRequestsByEvent.ContainsKey(campaignEvent)) return;

            while (_invalidationRequestsByEvent[campaignEvent].Count > 0)
                _invalidationRequestsByEvent[campaignEvent].Pop()();
        }

        private void OnSessionLaunched()
        {
            ExecuteInvalidationRequests(CampaignEvent.OnSessionLaunched);
            ExecuteCacheRequests(CampaignEvent.OnSessionLaunched);
        }

        private void OnAfterSessionLaunched()
        {
            ExecuteInvalidationRequests(CampaignEvent.OnAfterSessionLaunched);
            ExecuteCacheRequests(CampaignEvent.OnAfterSessionLaunched);
        }

        private string GenerateCachedObjectId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}