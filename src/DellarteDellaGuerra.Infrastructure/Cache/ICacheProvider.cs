using System;

namespace DellarteDellaGuerra.Infrastructure.Cache
{
    public interface ICacheProvider
    {
        string CacheObjectOnGameLoadFinished(Func<object> cacheObject);
        string CacheObjectOnNewGameCreated(Func<object> cacheObject);
        T? GetCachedObject<T>(string id);
    }
}