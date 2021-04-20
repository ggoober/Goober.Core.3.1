using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goober.Core.Models;

namespace Goober.Core.Services.Implementation
{
    class CacheProvider : ICacheProvider
    {
        private class CacheResult<T>
        {
            public T TargetObject { get; set; }

            public DateTime? RefreshTime { get; set; }
        }

        #region fields

        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheProvider> _logger;
        private readonly ConcurrentDictionary<string, CachedEntryInfo> _cachedEntriesDict = new ConcurrentDictionary<string, CachedEntryInfo>();

        #endregion

        #region ctor

        public CacheProvider(IMemoryCache memoryCache,
            ILogger<CacheProvider> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        #endregion

        #region ICacheProvider

        public void Remove(string cacheKey)
        {
            _memoryCache.Remove(cacheKey);

            _cachedEntriesDict.TryRemove(key: cacheKey, out var removed);
        }

        public async Task<T> GetAsync<T>(string cacheKey, int? refreshTimeInMinutes, int? expirationTimeInMinutes, Func<Task<T>> func)
        {
            var cachedResult = _memoryCache.Get(cacheKey) as CacheResult<T>;

            var currentDateTime = DateTime.Now;

            if (cachedResult != null)
            {
                _cachedEntriesDict.TryGetValue(cacheKey, out var existedCachedEnty);

                if (refreshTimeInMinutes.HasValue == true
                    && cachedResult.RefreshTime <= currentDateTime)
                {
                    try
                    {
                        cachedResult.TargetObject = await func();
                        cachedResult.RefreshTime = currentDateTime.AddMinutes(refreshTimeInMinutes.Value);

                        if (existedCachedEnty != null)
                        {
                            existedCachedEnty.LastRefreshDateTime = currentDateTime;
                            existedCachedEnty.NextRefreshDateTime = cachedResult.RefreshTime;
                        }
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError(exception: exc, message: $"Error while refreshing cache");
                    }
                }

                if (existedCachedEnty != null)
                {
                    existedCachedEnty.LastAccessDateTime = currentDateTime;
                    existedCachedEnty.IsEmpty = cachedResult.TargetObject == null;
                }

                if (cachedResult.TargetObject == null)
                    return default(T);

                return cachedResult.TargetObject;
            }

            var expensiveObject = await func();
            var absoluteRefreshDateTime = refreshTimeInMinutes.HasValue == true ? currentDateTime.AddMinutes(refreshTimeInMinutes.Value) : (DateTime?)null;
            var newCachedResult = new CacheResult<T>
            {
                RefreshTime = absoluteRefreshDateTime,
                TargetObject = expensiveObject
            };

            var cachedEnty = _cachedEntriesDict.GetOrAdd(key: cacheKey, valueFactory: (key) =>
            {
                return new CachedEntryInfo
                {
                    RowCreatedDateTime = currentDateTime,
                    ExpirationTimeInMinutes = expirationTimeInMinutes,
                    ExpirationDateTime = expirationTimeInMinutes != null ? currentDateTime.AddMinutes(expirationTimeInMinutes.Value) : (DateTime?)null,
                    RefreshTimeInMinutes = refreshTimeInMinutes,
                    NextRefreshDateTime = absoluteRefreshDateTime,
                    LastAccessDateTime = currentDateTime,
                    IsEmpty = newCachedResult.TargetObject == null
                };
            });

            if (expirationTimeInMinutes.HasValue == true)
            {
                var absoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(expirationTimeInMinutes.Value));

                _memoryCache.Set(key: cacheKey, value: newCachedResult, absoluteExpiration: absoluteExpiration);
            }
            else
            {
                _memoryCache.Set(key: cacheKey, value: newCachedResult);
            }

            if (newCachedResult.TargetObject == null)
                return default(T);

            return newCachedResult.TargetObject;
        }

        public CachedEntryInfo GetCachedEnty(string key)
        {
            _cachedEntriesDict.TryGetValue(key: key, out var result);

            return result;
        }

        public Dictionary<string, CachedEntryInfo> GetCachedEntries()
        {
            var currentDateTime = DateTime.Now;

            var expiredEntries = _cachedEntriesDict.Where(x => x.Value?.ExpirationDateTime < currentDateTime).ToList();

            foreach (var iCachedEntyWithKey in expiredEntries)
            {
                _cachedEntriesDict.TryRemove(iCachedEntyWithKey.Key, out var removedCachedEnty);
            }

            return _cachedEntriesDict.ToDictionary(x => x.Key, x => x.Value);
        }

        #endregion
    }
}
