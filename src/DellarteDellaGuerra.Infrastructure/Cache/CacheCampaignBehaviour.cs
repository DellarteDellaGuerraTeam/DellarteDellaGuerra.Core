using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Infrastructure.Cache
{
    public class CacheCampaignBehaviour : CampaignBehaviorBase, ICacheProvider
    {
        private readonly Dictionary<string, Func<object>> _onGameLoadFinishedCallbacks = new();
        private readonly Dictionary<string, Func<object>> _onNewGameCreatedCallbacks = new();

        private readonly Dictionary<string, object> _cache = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, _ => OnNewGameCreated());
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public string CacheObjectOnGameLoadFinished(Func<object> cacheObject)
        {
            var id = GenerateCachedObjectId();
            _onGameLoadFinishedCallbacks.Add(id, cacheObject);
            return id;
        }

        public string CacheObjectOnNewGameCreated(Func<object> cacheObject)
        {
            var id = GenerateCachedObjectId();
            _onNewGameCreatedCallbacks.Add(id, cacheObject);
            return id;
        }

        public T? GetCachedObject<T>(string id)
        {
            if (id is null || !_cache.ContainsKey(id)) return default;
            return (T)_cache[id];
        }

        private void ResetCache()
        {
            _cache.Clear();
        }
        
        private void OnGameLoadFinished()
        {
            ResetCache();
            foreach (var callback in _onGameLoadFinishedCallbacks) _cache.Add(callback.Key, callback.Value());
        }

        private void OnNewGameCreated()
        {
            ResetCache();
            foreach (var callback in _onNewGameCreatedCallbacks) _cache.Add(callback.Key, callback.Value());
        }

        private string GenerateCachedObjectId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}