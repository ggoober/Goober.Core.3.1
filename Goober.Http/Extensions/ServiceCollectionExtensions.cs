using Goober.Http.Abstractions;
using Goober.Http.Services;
using Goober.Http.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Goober.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGooberHttpHelper(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddScoped<IHttpHelperService, HttpHelperService>();
        }

        public static void AddGooberUdpHelper(this IServiceCollection services)
        {
            services.AddSingleton<IUdpHelperService, UdpHelperService>();
        }
    }
}
