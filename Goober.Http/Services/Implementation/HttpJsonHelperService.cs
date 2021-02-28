using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Goober.Http.Utils;

namespace Goober.Http.Services.Implementation
{
    class HttpJsonHelperService : IHttpJsonHelperService
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(
                    namingStrategy: new CamelCaseNamingStrategy(processDictionaryKeys: true, overrideSpecifiedNames: false, processExtensionDataNames: true),
                    allowIntegerValues: true)
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTime
        };

        private const string ApplicationJsonContentTypeValue = "application/json";

        private IHttpClientFactory _httpClientFactory;

        public HttpJsonHelperService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TResponse> ExecuteGetAsync<TResponse>(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            long maxContentLength = 300 * 1024)
        {

            var ret = await ExecuteGetStringAsync(urlWithoutQueryParameters: urlWithoutQueryParameters,
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutInMilliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: headerValues,
                maxContentLength: maxContentLength);

            return Deserialize<TResponse>(ret, jsonSerializerSettings);
        }

        public async Task<string> ExecuteGetStringAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            long maxContentLength = 300 * 1024)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
                var url = HttpUtils.BuildUrlWithQueryParameters(urlWithoutQueryParameters, queryParameters);

                var httpRequest = HttpUtils.GenerateHttpRequestMessage(
                    requestUri: url,
                    httpMethodType: HttpMethod.Get,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: headerValues,
                    responseMediaTypes: new List<string> { ApplicationJsonContentTypeValue });

                var httpResponse = await httpClient.SendAsync(httpRequest);
                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                await ThrowExceptionIfResponseIsNotValidAsync(
                    httpResponse: httpResponse,
                    maxContentLength: maxContentLength,
                    loggingUrl: url,
                    loggingStrContent: string.Empty
                    );

                var ret = await httpResponse.Content.ReadAsStringAsync();

                return ret;
            }
        }

        public async Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string url,
            TRequest request,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            long maxContentLength = 300 * 1024)
        {
            var ret = await ExecutePostStringAsync(url: url,
                request: request,
                timeoutInMilliseconds: timeoutInMilliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: headerValues,
                jsonSerializerSettings: jsonSerializerSettings,
                maxContentLength: maxContentLength);

            return Deserialize<TResponse>(ret, jsonSerializerSettings);
        }

        public async Task<string> ExecutePostStringAsync<TRequest>(string url,
            TRequest request,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            long maxContentLength = 300 * 1024)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var httpRequest = HttpUtils.GenerateHttpRequestMessage(
                    requestUri: url,
                    httpMethodType: HttpMethod.Post,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: headerValues,
                    responseMediaTypes: new List<string> { ApplicationJsonContentTypeValue });

                var strContent = Serialize(request, jsonSerializerSettings);

                httpRequest.Content = new StringContent(content: strContent, Encoding.UTF8, mediaType: ApplicationJsonContentTypeValue);

                var httpResponse = await httpClient.SendAsync(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                await ThrowExceptionIfResponseIsNotValidAsync(httpResponse: httpResponse,
                    maxContentLength: maxContentLength,
                    loggingUrl: url,
                    loggingStrContent: strContent);

                var ret = await httpResponse.Content.ReadAsStringAsync();

                return ret;
            }
        }

        #region private methods

        private static string Serialize(object value, JsonSerializerSettings serializerSettings = null)
        {
            return JsonConvert.SerializeObject(value, serializerSettings ?? _jsonSerializerSettings);
        }

        private static T Deserialize<T>(string value, JsonSerializerSettings serializerSettings = null)
        {
            return JsonConvert.DeserializeObject<T>(value, serializerSettings ?? _jsonSerializerSettings);
        }

        private static async Task ThrowExceptionIfResponseIsNotValidAsync(HttpResponseMessage httpResponse, long maxContentLength, string loggingUrl, string loggingStrContent)
        {
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                if (httpResponse.Content.Headers.ContentLength > maxContentLength)
                {
                    throw new WebException($"Response ({loggingUrl}) content-length ({maxContentLength}) exceeded, content: {loggingStrContent}");
                }

                return;
            }

            var exception = new WebException($"Request({loggingUrl}) fault with code = {httpResponse.StatusCode}, data: {loggingStrContent}");

            if (httpResponse.Content.Headers.ContentLength <= maxContentLength)
            {
                var errorString = await httpResponse.Content.ReadAsStringAsync();
                exception.Data["error"] = errorString;
            }

            throw exception;
        }

        #endregion
    }
}
