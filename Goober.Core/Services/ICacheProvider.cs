using System;
using System.Threading.Tasks;

namespace Goober.Core.Services
{
    public interface ICacheProvider
    {
        void Remove(string cacheKey);

        Task<T> GetAsync<T>(string cacheKey, int? refreshTimeInMinutes, int? expirationTimeInMinutes, Func<Task<T>> func);
    }
}
