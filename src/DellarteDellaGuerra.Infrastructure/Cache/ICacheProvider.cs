using System;

namespace DellarteDellaGuerra.Infrastructure.Cache
{
    public interface ICacheProvider
    {
        string CacheObject(object cacheObject);
        string CacheObject(Func<object> cacheRequest, CachedEvent cachedEvent);
        T? GetCachedObject<T>(string id);
        void InvalidateCache();
        void InvalidateCache(string id);
        void InvalidateCache(string id, CachedEvent cachedEvent);
    }
}
