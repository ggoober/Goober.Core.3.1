using System;
using System.Threading.Tasks;

namespace Goober.Http.Caching
{
    public interface IHttpCacheProvider
    {
        void Remove(string cacheKey);

        T Get<T>(string cacheKey, int cacheTimeInMinutes, Func<T> func);

        Task<T> GetAsync<T>(string cacheKey, int cacheTimeInMinutes, Func<Task<T>> func);

        Task<T> GetWithSecondsAsync<T>(string cacheKey, int cacheTimeInSeconds, Func<Task<T>> func);
    }
}
