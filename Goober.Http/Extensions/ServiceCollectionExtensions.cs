using Goober.Http.Caching;
using Goober.Http.Services;
using Goober.Http.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Goober.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterRemoteCall(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IHttpCacheProvider, Caching.Implementation.HttpCacheProvider>();
            services.AddScoped<IHttpHelperService, HttpHelperService>();
        }
    }
}
