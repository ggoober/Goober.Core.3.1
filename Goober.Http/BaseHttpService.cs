using Goober.Http.Services;
using Goober.Http.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Goober.Http
{
    public abstract class BaseHttpService
    {
        #region fields

        protected abstract string ApiSchemeAndHostConfigKey { get; set; }

        private const string CallSequenceKey = "g-callsec";

        private const string CallSequenceIdKey = "g-callsec-id";

        protected readonly IHttpJsonHelperService HttpJsonHelperService;
        protected readonly IHttpContextAccessor HttpContextAccessor;
        private readonly IHostEnvironment _hostEnvironment;
        protected readonly IConfiguration Configuration;
        protected readonly string AssemblyName;

        #endregion

        #region ctor

        protected BaseHttpService(IConfiguration configuration, IHttpJsonHelperService httpJsonHelperService,
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HttpJsonHelperService = httpJsonHelperService;
            HttpContextAccessor = httpContextAccessor;
            _hostEnvironment = hostEnvironment;
            AssemblyName = _hostEnvironment.ApplicationName;
        }

        #endregion

        protected async Task<TResponse> ExecuteGetAsync<TResponse>(string path,
            List<KeyValuePair<string, string>> queryParameters,
            string callerMethodName,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutMiliseconds = 12000)
        {
            var newHeaderValues = GetHeaderValuesWithCallSequence(headerValues, callerMethodName);

            var urlWithoutQueryParameters = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpJsonHelperService.ExecuteGetAsync<TResponse>(
                urlWithoutQueryParameters: urlWithoutQueryParameters, 
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutMiliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: newHeaderValues);

            return result;
        }

        protected async Task<string> ExecuteGetStringAsync(string path,
            List<KeyValuePair<string, string>> queryParameters,
            string callerMethodName,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutMiliseconds = 12000)
        {
            var newHeaderValues = GetHeaderValuesWithCallSequence(headerValues, callerMethodName);

            var urlWithoutQueryParameters = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpJsonHelperService.ExecuteGetStringAsync(
                urlWithoutQueryParameters: urlWithoutQueryParameters,
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutMiliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: newHeaderValues);

            return result;
        }

        protected async Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string path,
            TRequest request,
            string callerMethodName,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutInMilliseconds = 120000)
        {
            var newHeaderValues = GetHeaderValuesWithCallSequence(headerValues, callerMethodName);

            var url = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpJsonHelperService.ExecutePostAsync<TResponse, TRequest>(
                    url: url,
                    request: request,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: newHeaderValues,
                    timeoutInMilliseconds: timeoutInMilliseconds
                );

            return result;
        }

        protected async Task<string> ExecutePostStringAsync<TRequest>(string path,
            TRequest request,
            string callerMethodName,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutInMilliseconds = 120000)
        {
            var newHeaderValues = GetHeaderValuesWithCallSequence(headerValues, callerMethodName);

            var url = BuildUrlBySchemeAndHostAndPath(path);

            var result = await HttpJsonHelperService.ExecutePostStringAsync<TRequest>(
                    url: url,
                    request: request,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: newHeaderValues,
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

        private List<KeyValuePair<string, string>> GetHeaderValuesWithCallSequence(List<KeyValuePair<string, string>> headerValues, string methodName)
        {
            var ret = headerValues?.ToList() ?? new List<KeyValuePair<string, string>>();

            var callSequnce = GetCallSequence();

            var actionName = HttpContextAccessor.HttpContext?.Request?.Path ?? methodName;

            callSequnce.Add(new CallSequenceHeaderModel { Application = AssemblyName, Action = actionName });

            var strCallSequence = JsonUtils.Serialize(callSequnce);

            ret.Add(new KeyValuePair<string, string>(CallSequenceKey, strCallSequence));

            var callSequenceId = GetCallSequenceIdFromContextItemsOrGenerateNew();
            if (string.IsNullOrEmpty(callSequenceId) == false)
            {
                ret.Add(new KeyValuePair<string, string>(CallSequenceIdKey, callSequenceId));
            }

            return ret;
        }

        private List<CallSequenceHeaderModel> GetCallSequence()
        {
            var ret = new List<CallSequenceHeaderModel>();

            var isCallSequenceExists = HttpContextAccessor.HttpContext?.Request.Headers.TryGetValue(CallSequenceKey, out var callSequence);
            if (isCallSequenceExists == true)
            {
                var iCallSequenceValues = callSequence.ToList();

                foreach (var iCallSequenceValue in iCallSequenceValues)
                {
                    var methods = JsonUtils.Deserialize<List<CallSequenceHeaderModel>>(iCallSequenceValue);

                    if (methods.Any() == false)
                    {
                        continue;
                    }

                    ret.AddRange(methods);
                }
            }

            return ret;
        }

        private string GetCallSequenceIdFromContextItemsOrGenerateNew()
        {
            var contextItems = HttpContextAccessor.HttpContext.Items;

            if (contextItems == null || contextItems.ContainsKey(CallSequenceIdKey) == false)
            {
                return Guid.NewGuid().ToString();
            }

            var ret = contextItems[CallSequenceIdKey];
            if (ret == null)
                return Guid.NewGuid().ToString();

            return ret.ToString();
        }

        class CallSequenceHeaderModel
        {
            public string Application { get; set; }

            public string Action { get; set; }
        }
    }
}
