using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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
        }

        public async Task<T> GetAsync<T>(string cacheKey, int? refreshTimeInMinutes, int? expirationTimeInMinutes, Func<Task<T>> func)
        {
            var cachedResult = _memoryCache.Get(cacheKey) as CacheResult<T>;

            var currentDateTime = DateTime.Now;

            if (cachedResult != null)
            {
                if (refreshTimeInMinutes.HasValue == true
                    && cachedResult.RefreshTime <= currentDateTime)
                {
                    try
                    {
                        cachedResult.TargetObject = await func();
                        cachedResult.RefreshTime = currentDateTime.AddMinutes(refreshTimeInMinutes.Value);
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError(exception: exc, message: $"Error while refreshing cache");
                    }
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

        #endregion
    }
}
