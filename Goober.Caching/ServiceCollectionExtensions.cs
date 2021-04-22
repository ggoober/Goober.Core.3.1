using Goober.Caching.Services;
using Goober.Caching.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Goober.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGooberCaching(this IServiceCollection services, long? memoryCacheSizeLimitInBytes)
        {
            services.AddMemoryCache((options) => { options.SizeLimit = memoryCacheSizeLimitInBytes; });
            services.AddSingleton<ICacheProvider, CacheProvider>();
        }
    }
}
