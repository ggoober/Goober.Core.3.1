﻿using Goober.Http.Services;
using Goober.Http.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Goober.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGooberHttpServices(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddScoped<IHttpJsonHelperService, HttpJsonHelperService>();
        }
    }
}
