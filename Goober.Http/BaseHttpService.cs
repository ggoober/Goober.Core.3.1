using Goober.Http.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goober.Http
{
    public abstract class BaseHttpService
    {
        #region fields

        protected abstract string ApiSchemeAndHostConfigKey { get; set; }
        protected readonly IHttpHelperService HttpClientService;
        protected readonly IConfiguration Configuration;

        #endregion

        #region ctor

        protected BaseHttpService(IConfiguration configuration, IHttpHelperService httpClientService)
        {
            Configuration = configuration;
            HttpClientService = httpClientService;
        }

        #endregion

        protected async Task<TResponse> ExecuteGetAsync<TResponse>(string path,
            List<KeyValuePair<string, string>> queryParameters,
            int timeoutMiliseconds = 15000)
        {
            var urlWithoutQueryParameters = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecuteGetAsync<TResponse>(urlWithoutQueryParameters: urlWithoutQueryParameters, 
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutMiliseconds);

            return result;
        }

        protected async Task<string> ExecuteGetAsStringAsync<TResponse>(string path,
            List<KeyValuePair<string, string>> queryParameters,
            int timeoutMiliseconds = 15000)
        {
            var urlWithoutQueryParameters = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecuteGetAsStringAsync(urlWithoutQueryParameters: urlWithoutQueryParameters, 
                queryParameters: queryParameters, 
                timeoutInMilliseconds: timeoutMiliseconds);

            return result;
        }

        protected async Task<byte[]> ExecuteGetAsByteArrayAsync<TResponse>(string path,
            List<KeyValuePair<string, string>> queryParameters,
            int timeoutMiliseconds = 15000)
        {
            var urlWithoutQueryParameters = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecuteGetAsByteArrayAsync(urlWithoutQueryParameters: urlWithoutQueryParameters,
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutMiliseconds);

            return result;
        }

        protected async Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string path, TRequest request, int timeoutMiliseconds = 15000)
        {
            var url = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecutePostAsync<TResponse, TRequest>(url: url, request: request, timeoutInMilliseconds: timeoutMiliseconds);

            return result;
        }

        protected async Task<string> ExecutePostAsStringAsync<TRequest>(string path, TRequest request, int timeoutMiliseconds = 15000)
        {
            var url = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecutePostAsStringAsync(url: url, request: request, timeoutInMilliseconds: timeoutMiliseconds);

            return result;
        }

        protected async Task<byte[]> ExecutePostAsByteArrayAsync<TRequest>(string path, TRequest request, int timeoutMiliseconds = 15000)
        {
            var url = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecutePostAsByteArrayAsync(url: url, request: request, timeoutInMilliseconds: timeoutMiliseconds);

            return result;
        }

        protected string BuildUrlBySchemeAndHostAndPath(string path)
        {
            if (string.IsNullOrEmpty(ApiSchemeAndHostConfigKey) == true)
                throw new InvalidOperationException("ApiSchemeAndHostConfigKey is empty");

            var schemeAndHost = Configuration[ApiSchemeAndHostConfigKey];

            if (string.IsNullOrEmpty(schemeAndHost))
                throw new InvalidOperationException($"schemeAndHost is empty by key = {ApiSchemeAndHostConfigKey}");

            var url = HttpClientService.BuildUrl(schemeAndHost: schemeAndHost, urlPath: path);
            return url;
        }
    }
}
