using Goober.Caching.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goober.Caching.Services
{
    public interface ICacheProvider
    {
        void Remove(string cacheKey);

        Task<T> GetAsync<T>(string cacheKey, int? refreshTimeInMinutes, int? expirationTimeInMinutes, Func<Task<T>> func);

        CachedEntryInfo GetCachedEnty(string key);

        Dictionary<string, CachedEntryInfo> GetCachedEntries();
    }
}
