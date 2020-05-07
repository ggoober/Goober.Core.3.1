using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Goober.Http.Services.Implementation
{
    class HttpHelperService : IHttpHelperService
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new StringEnumConverter() },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTime
        };

        private const string ApplicationJsonContentTypeValue = "application/json";

        private IHttpClientFactory _httpClientFactory;

        public HttpHelperService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TResponse> ExecuteGetAsync<TResponse>(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var httpRequest = GenerateHttpRequestMessageWithHeader(authenticationHeaderValue, headerValues);

                var url = ConcatUrlWithQueryParameters(urlWithoutQueryParameters, queryParameters);

                httpRequest.Method = HttpMethod.Get;
                httpRequest.RequestUri = new Uri(url);

                var httpResponse = await httpClient.SendAsync(httpRequest);
                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }
                await ThrowExceptionOnNotOKStatusCodeAsync(url, httpResponse, string.Empty);

                var ret = await httpResponse.Content.ReadAsStringAsync();
                return Deserialize<TResponse>(ret);
            }
        }

        public async Task<string> ExecuteGetAsStringAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var httpRequest = GenerateHttpRequestMessageWithHeader(authenticationHeaderValue, headerValues);

                var url = ConcatUrlWithQueryParameters(urlWithoutQueryParameters, queryParameters);

                httpRequest.Method = HttpMethod.Get;
                httpRequest.RequestUri = new Uri(url);

                var httpResponse = await httpClient.SendAsync(httpRequest);
                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return string.Empty;
                }
                await ThrowExceptionOnNotOKStatusCodeAsync(url, httpResponse, string.Empty);

                var ret = await httpResponse.Content.ReadAsStringAsync();
                return ret;
            }
        }

        public async Task<byte[]> ExecuteGetAsByteArrayAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var httpRequest = GenerateHttpRequestMessageWithHeader(authenticationHeaderValue, headerValues);

                var url = ConcatUrlWithQueryParameters(urlWithoutQueryParameters, queryParameters);

                httpRequest.Method = HttpMethod.Get;
                httpRequest.RequestUri = new Uri(url);

                var httpResponse = await httpClient.SendAsync(httpRequest);
                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return null;
                }
                await ThrowExceptionOnNotOKStatusCodeAsync(url, httpResponse, string.Empty);

                var ret = await httpResponse.Content.ReadAsByteArrayAsync();
                return ret;
            }
        }

        public async Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string url,
            TRequest request,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var strRequest = Serialize(request);

                var httpRequest = GeneratePostHttpRequestMessage(url: url,
                    strRequest: strRequest,
                    applicationJsonContentTypeValue: ApplicationJsonContentTypeValue,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: headerValues);

                var httpResponse = await httpClient.SendAsync(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }
                await ThrowExceptionOnNotOKStatusCodeAsync(url, httpResponse, strRequest);

                var ret = await httpResponse.Content.ReadAsStringAsync();
                return Deserialize<TResponse>(ret);
            }
        }

        public async Task<byte[]> ExecutePostAsByteArrayAsync<TRequest>(string url,
            TRequest request,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var strRequest = Serialize(request);

                var httpRequest = GeneratePostHttpRequestMessage(url: url,
                    strRequest: strRequest,
                    applicationJsonContentTypeValue: ApplicationJsonContentTypeValue,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: headerValues);

                var httpResponse = await httpClient.SendAsync(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return null;
                }
                await ThrowExceptionOnNotOKStatusCodeAsync(url, httpResponse, strRequest);

                var ret = await httpResponse.Content.ReadAsByteArrayAsync();
                return ret;
            }
        }

        public async Task<string> ExecutePostAsStringAsync<TRequest>(string url,
            TRequest request,
            int timeoutInMilliseconds = 120000,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var strRequest = Serialize(request);

                var httpRequest = GeneratePostHttpRequestMessage(url: url,
                    strRequest: strRequest,
                    applicationJsonContentTypeValue: ApplicationJsonContentTypeValue,
                    authenticationHeaderValue: authenticationHeaderValue,
                    headerValues: headerValues);

                var httpResponse = await httpClient.SendAsync(httpRequest);
                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return string.Empty;
                }

                await ThrowExceptionOnNotOKStatusCodeAsync(url, httpResponse, strRequest);

                var ret = await httpResponse.Content.ReadAsStringAsync();
                return ret;
            }
        }

        public string BuildUrl(string schemeAndHost, string urlPath)
        {
            var baseUri = new UriBuilder(new Uri(new Uri(schemeAndHost), urlPath));

            return new UriBuilder(scheme: baseUri.Scheme,
                                                host: baseUri.Host,
                                                port: baseUri.Port,
                                                path: baseUri.Path,
                                                extraValue: baseUri.Query).Uri.ToString();
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

        private static HttpRequestMessage GeneratePostHttpRequestMessage(string url,
            string strRequest,
            string applicationJsonContentTypeValue,
            AuthenticationHeaderValue authenticationHeaderValue,
            List<KeyValuePair<string, string>> headerValues)
        {
            var ret = GenerateHttpRequestMessageWithHeader(authenticationHeaderValue, headerValues);

            ret.Method = HttpMethod.Post;
            ret.RequestUri = new Uri(url);

            ret.Content = new StringContent(strRequest, Encoding.UTF8, applicationJsonContentTypeValue);

            return ret;
        }

        private static async Task ThrowExceptionOnNotOKStatusCodeAsync(string url, HttpResponseMessage httpResponse, string strRequest)
        {
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            var exception = new WebException($"Request({url}) fault with code = {httpResponse.StatusCode}, data: {strRequest}");
            if (httpResponse.StatusCode == HttpStatusCode.InternalServerError)
            {
                var jsonErrorString = await httpResponse.Content.ReadAsStringAsync();
                exception.Data["JsonError"] = jsonErrorString;
            }

            throw exception;
        }

        private static HttpRequestMessage GenerateHttpRequestMessageWithHeader(AuthenticationHeaderValue authenticationHeaderValue, List<KeyValuePair<string, string>> headerValues)
        {
            var httpRequest = new HttpRequestMessage();
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJsonContentTypeValue));

            if (authenticationHeaderValue != null)
            {
                httpRequest.Headers.Authorization = authenticationHeaderValue;
            }

            if (headerValues != null && headerValues.Any())
            {
                foreach (var item in headerValues)
                {
                    httpRequest.Headers.Add(item.Key, item.Value);
                }
            }

            return httpRequest;
        }

        private static string ConcatUrlWithQueryParameters(string urlWithoutQueryParameters, List<KeyValuePair<string, string>> queryParameters)
        {
            var url = urlWithoutQueryParameters;

            if (queryParameters != null && queryParameters.Any() == true)
            {
                return url + "?" + string.Join(separator: "&", values: queryParameters.Select(x => $"{x.Key}={x.Value}"));
            }

            return url;
        }

        #endregion
    }
}
