using Goober.Http.Caching;
using Goober.Http.Enums;
using Goober.Http.Models;
using Goober.Http.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goober.Http
{
    public abstract class BaseHttpService
    {
        #region fields

        protected virtual int _defaultCacheTime { get; set; } = 5;

        protected abstract string _apiSchemeAndHostConfigKey { get; set; }

        protected readonly IConfiguration Configuration;
        protected readonly IHttpHelperService HttpHelperService;
        protected readonly IHttpCacheProvider HttpCacheProvider;

        #endregion

        #region ctor

        public BaseHttpService(
            IConfiguration configuration,
            IHttpHelperService httpHelperService,
            IHttpCacheProvider httpCacheProvider)
        {
            Configuration = configuration;
            HttpHelperService = httpHelperService;
            HttpCacheProvider = httpCacheProvider;
        }

        #endregion

        protected async Task<TResponse> ExecuteGetCachedAsync<TResponse>(string path,
            List<KeyValuePair<string, string>> queryParameters,
            int cacheTimeInSeconds = 300,
            int? timeoutMiliseconds = null,
            Credentials credentials = null,
            JsonSerializerSettings serializerSettings = null) where TResponse : class
        {
            var url = GetUrl(path);
            var strQueryParameters = string.Join(";", queryParameters.Select(x => $"{x.Key}={x.Value}"));

            var cacheKey = $"{this.GetType().Name}.ExecuteGetCachedAsync({url},{strQueryParameters})";

            return await HttpCacheProvider.GetAsync(cacheKey: cacheKey,
                cacheTimeInMinutes: cacheTimeInSeconds,
                func: async () => await ExecuteGetAsync<TResponse>(path, queryParameters, timeoutMiliseconds, credentials, serializerSettings));
        }

        protected async Task<TResponse> ExecuteGetAsync<TResponse>(string path,
            List<KeyValuePair<string, string>> queryParameters,
            int? timeoutMiliseconds = null,
            Credentials credentials = null,
            JsonSerializerSettings serializerSettings = null) where TResponse : class
        {
            var url = GetUrl(path);

            var result = await HttpHelperService.ExecuteGetAsync<TResponse>(urlWithourQueryParameters: url,
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutMiliseconds,
                credentials: credentials,
                serializerSettings: serializerSettings);

            return result;
        }

        protected async Task<TResponse> ExecutePostCachedAsync<TResponse, TRequest>(string path,
            TRequest request,
            int cacheTimeInSeconds = 300,
            int? timeoutMiliseconds = null,
            ContentTypeEnum contentType = ContentTypeEnum.ApplicationJson,
            Credentials credentials = null,
            JsonSerializerSettings serializerSettings = null)
            where TRequest : class
        {
            var url = GetUrl(path);

            var cacheKey = $"{this.GetType().Name}.ExecutePostCachedAsync({url},{JsonConvert.SerializeObject(request)})";

            return await HttpCacheProvider.GetAsync(cacheKey: cacheKey, cacheTimeInMinutes: cacheTimeInSeconds, func: async () => await ExecutePostAsync<TResponse, TRequest>(path, request, timeoutMiliseconds, contentType, credentials, serializerSettings));
        }

        protected async Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string path,
        TRequest request,
        int? timeoutMiliseconds = null,
        ContentTypeEnum contentType = ContentTypeEnum.ApplicationJson,
        Credentials credentials = null,
        JsonSerializerSettings serializerSettings = null)
        where TRequest : class
        {
            var url = GetUrl(path);

            var result = await HttpHelperService.ExecutePostAsync<TResponse, TRequest>(url: url,
                bodyContent: request,
                contentType: contentType,
                credentials: credentials,
                timeoutInMilliseconds: timeoutMiliseconds,
                serializerSettings: serializerSettings);

            return result;
        }

        protected async Task<string> ExecutePostAsStringAsync<TRequest>(string path, TRequest request,
            int? timeoutMiliseconds = null,
            ContentTypeEnum contentType = ContentTypeEnum.ApplicationJson,
            Credentials credentials = null)
        {
            var url = GetUrl(path);

            var result = await HttpHelperService.ExecutePostAsStringAsync(url: url,
                data: request,
                timeoutInMilliseconds: timeoutMiliseconds,
                contentType: contentType,
                credentials: credentials);

            return result;
        }

        protected string GetUrl(string path)
        {
            if (string.IsNullOrEmpty(_apiSchemeAndHostConfigKey) == true)
                throw new InvalidOperationException("ApiSchemeAndHostConfigKey is empty");

            var schemeAndHost = Configuration[_apiSchemeAndHostConfigKey];

            if (string.IsNullOrEmpty(schemeAndHost))
                throw new InvalidOperationException($"schemeAndHost is empty by key = {_apiSchemeAndHostConfigKey}");

            var url = HttpHelperService.BuildUrl(schemeAndHost: schemeAndHost, urlPath: path);

            return url;
        }
    }
}
