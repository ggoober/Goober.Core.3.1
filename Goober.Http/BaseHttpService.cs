using Goober.Http.Services;
using Goober.Http.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Goober.Http
{
    public abstract class BaseHttpService
    {
        #region fields

        protected abstract string ApiSchemeAndHostConfigKey { get; set; }
        protected readonly IHttpJsonHelperService HttpClientService;
        protected readonly IConfiguration Configuration;

        #endregion

        #region ctor

        protected BaseHttpService(IConfiguration configuration, IHttpJsonHelperService httpClientService)
        {
            Configuration = configuration;
            HttpClientService = httpClientService;
        }

        #endregion

        protected async Task<TResponse> ExecuteGetAsync<TResponse>(string path,
            List<KeyValuePair<string, string>> queryParameters,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutMiliseconds = 12000)
        {
            var urlWithoutQueryParameters = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecuteGetAsync<TResponse>(
                urlWithoutQueryParameters: urlWithoutQueryParameters, 
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutMiliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: headerValues);

            return result;
        }

        protected async Task<string> ExecuteGetStringAsync(string path,
            List<KeyValuePair<string, string>> queryParameters,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutMiliseconds = 12000)
        {
            var urlWithoutQueryParameters = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecuteGetStringAsync(
                urlWithoutQueryParameters: urlWithoutQueryParameters,
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutMiliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: headerValues);

            return result;
        }

        protected async Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string path,
            TRequest request,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutInMilliseconds = 120000)
        {
            var url = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecutePostAsync<TResponse, TRequest>(
                    url: url,
                    request: request,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: headerValues,
                    timeoutInMilliseconds: timeoutInMilliseconds
                );

            return result;
        }

        protected async Task<string> ExecutePostStringAsync<TRequest>(string path,
            TRequest request,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutInMilliseconds = 120000)
        {
            var url = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpClientService.ExecutePostStringAsync<TRequest>(
                    url: url,
                    request: request,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: headerValues,
                    timeoutInMilliseconds: timeoutInMilliseconds
                );

            return result;
        }

        protected string BuildUrlBySchemeAndHostAndPath(string path)
        {
            if (string.IsNullOrEmpty(ApiSchemeAndHostConfigKey) == true)
                throw new InvalidOperationException("ApiSchemeAndHostConfigKey is empty");

            var schemeAndHost = Configuration[ApiSchemeAndHostConfigKey];

            if (string.IsNullOrEmpty(schemeAndHost))
                throw new InvalidOperationException($"schemeAndHost is empty by key = {ApiSchemeAndHostConfigKey}");

            var url = HttpUtils.BuildUrl(schemeAndHost: schemeAndHost, urlPath: path);
            return url;
        }
    }
}
