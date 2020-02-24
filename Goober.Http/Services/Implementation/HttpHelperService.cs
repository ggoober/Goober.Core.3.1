using Goober.Http.Enums;
using Goober.Http.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Goober.Http.Services.Implementation
{
    internal class HttpHelperService : IHttpHelperService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpHelperService(
            IHttpClientFactory httpClientFactory
            )
        {
            _httpClientFactory = httpClientFactory;
        }

        private readonly JsonSerializerSettings _defaultJsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new StringEnumConverter() },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTime
        };


        public async Task<T> ExecuteGetAsync<T>(string urlWithourQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int? timeoutInMilliseconds = null,
            Credentials credentials = null,
            JsonSerializerSettings serializerSettings = null) where T : class
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                SetAuthorization(httpClient, credentials);

                SetTimout(httpClient, timeoutInMilliseconds);

                string url = GenerateGetUrl(urlWithourQueryParameters, queryParameters);

                var response = await httpClient.GetAsync(new Uri(url));

                var strContent = await response.Content.ReadAsStringAsync();

                return await DeserializeWebResponseAsync<T>(strContent, serializerSettings ?? _defaultJsonSerializerSettings);
            }
        }

        public async Task<string> ExecuteGetAsStringAsync(string urlWithourQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int? timeoutInMilliseconds = null,
            Credentials credentials = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                SetAuthorization(httpClient, credentials);

                SetTimout(httpClient, timeoutInMilliseconds);

                string url = GenerateGetUrl(urlWithourQueryParameters, queryParameters);

                var response = await httpClient.GetAsync(new Uri(url));

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<byte[]> ExecuteGetAsByteArrayAsync(string urlWithourQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            int? timeoutInMilliseconds = null,
            Credentials credentials = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                SetAuthorization(httpClient, credentials);

                SetTimout(httpClient, timeoutInMilliseconds);

                string url = GenerateGetUrl(urlWithourQueryParameters, queryParameters);

                var resultString = await httpClient.GetAsync(new Uri(url));

                return await resultString.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task<T> ExecutePostAsync<T, U>(string url,
            U bodyContent,
            ContentTypeEnum contentType = ContentTypeEnum.ApplicationJson,
            Credentials credentials = null,
            int? timeoutInMilliseconds = null,
            JsonSerializerSettings serializerSettings = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                SetAuthorization(httpClient, credentials);

                SetTimout(httpClient, timeoutInMilliseconds);

                var mediaType = GetMediaType(contentType);

                var strBody = JsonConvert.SerializeObject(bodyContent, serializerSettings ?? _defaultJsonSerializerSettings);

                var content = new StringContent(strBody, Encoding.UTF8, mediaType);

                var responseMessage = await httpClient.PostAsync(url, content);

                return await ProcessResponseMessageAsync<T>(responseMessage, url, strBody, contentType, serializerSettings);
            }
        }

        public async Task<string> ExecutePostAsStringAsync<TRequest>(string url, TRequest data,
            ContentTypeEnum contentType = ContentTypeEnum.ApplicationJson, int? timeoutInMilliseconds = null, Credentials credentials = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                SetAuthorization(httpClient, credentials);

                SetTimout(httpClient, timeoutInMilliseconds);

                var jsonString = JsonConvert.SerializeObject(data);

                var mediaType = GetMediaType(contentType);

                var content = new StringContent(jsonString, Encoding.UTF8, mediaType);

                var responseMessage = await httpClient.PostAsync(url, content);

                var res = await ProcessResponseMessageAsStringAsync(responseMessage, url, jsonString);

                return res;
            }
        }

        public async Task<byte[]> ExecutePostWithByteArrayResponseAsync<TRequest>(string url, TRequest parameters, int? timeoutInMilliseconds = null)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                SetTimout(httpClient, timeoutInMilliseconds);

                var jsonSerialized = JsonConvert.SerializeObject(parameters);

                var content = new StringContent(jsonSerialized, Encoding.UTF8, "application/json");

                var httpResponse = httpClient.PostAsync(url, content).Result;

                return await ProcessByteArrayResponseMessageAsync(httpResponse, url, jsonSerialized);
            }
        }

        public string BuildUrl(string schemeAndHost, string urlPath, bool joinUrlAsIs = false)
        {
            var baseUri = new UriBuilder(new Uri(new Uri(schemeAndHost), urlPath));

            return new UriBuilder(scheme: baseUri.Scheme,
                                                host: baseUri.Host,
                                                port: baseUri.Port,
                                                path: baseUri.Path,
                                                extraValue: baseUri.Query).Uri.ToString();
        }

        #region private methods

        private async Task<T> DeserializeWebResponseAsync<T>(string strContent, JsonSerializerSettings serializerSettings)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(strContent, serializerSettings ?? _defaultJsonSerializerSettings);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException($"Can't deserialize to Type = {typeof(T).Name} value: {strContent}, exc.message = {exc.Message}");
            }
        }

        private string GenerateCredentials(string login, string password)
            => Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login}:{password}"));

        private void SetAuthorization(HttpClient client, Credentials credentials)
        {
            if (credentials == null) return;

            if ((string.IsNullOrWhiteSpace(credentials.Login) || string.IsNullOrWhiteSpace(credentials.Password))
                 && string.IsNullOrWhiteSpace(credentials.Token))
                return;

            var token = string.IsNullOrEmpty(credentials.Token)
                ? GenerateCredentials(credentials.Login, credentials.Password)
                : credentials.Token;

            client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue($"{credentials.Type}", token);
        }

        private void SetTimout(HttpClient client, int? timeoutInMilliseconds)
        {
            if (timeoutInMilliseconds.HasValue)
            {
                client.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds.Value);
            }
        }

        private async Task<byte[]> ProcessByteArrayResponseMessageAsync(HttpResponseMessage httpResponseMessage, string url, string requestBody)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
            {
                return new byte[0];
            }

            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                var exception = new WebException($"Request({url}) fault with code = {httpResponseMessage.StatusCode}, parameters = {requestBody}");
                if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError)
                {
                    var byteError = await httpResponseMessage.Content.ReadAsByteArrayAsync();
                    exception.Data["ErrorMessage"] = byteError.ToString();
                }

                throw exception;
            }

            return await httpResponseMessage.Content.ReadAsByteArrayAsync();
        }

        private async Task<T> ProcessResponseMessageAsync<T>(HttpResponseMessage responseMessage,
            string url,
            string strBody,
            ContentTypeEnum contentType,
            JsonSerializerSettings serializerSettings)
        {
            if (responseMessage.StatusCode == HttpStatusCode.NoContent)
            {
                return default(T);
            }

            T result;
            var responseString = await responseMessage.Content?.ReadAsStringAsync();

            if (responseMessage.StatusCode != HttpStatusCode.OK
                && responseMessage.StatusCode != HttpStatusCode.Created
                && responseMessage.StatusCode != HttpStatusCode.Accepted)
            {
                var exception = new HermesWebException($"Request({url}) fault with code = {responseMessage.StatusCode}, data: {strBody}", responseMessage.StatusCode, responseString);
                if (responseMessage.StatusCode == HttpStatusCode.InternalServerError)
                {
                    exception.Data["ErrorMessage"] = responseString;
                }

                throw exception;
            }

            if (contentType == ContentTypeEnum.ApplicationJson)
            {
                result = await DeserializeWebResponseAsync<T>(responseString, serializerSettings);
            }
            else
            {
                using (var stream = new MemoryStream(StringToUTF8ByteArray(responseString)))
                {
                    result = (T)new XmlSerializer(typeof(T)).Deserialize(stream);
                }

            }

            return result;
        }

        private async Task<string> ProcessResponseMessageAsStringAsync(HttpResponseMessage responseMessage,
            string url,
            string strContent)
        {
            if (responseMessage.StatusCode == HttpStatusCode.NoContent)
            {
                return default(string);
            }

            var responseString = await responseMessage.Content.ReadAsStringAsync();

            if (responseMessage.StatusCode != HttpStatusCode.OK
                && responseMessage.StatusCode != HttpStatusCode.Created
                && responseMessage.StatusCode != HttpStatusCode.Accepted)
            {
                var exception = new HermesWebException($"Request({url}) fault with code = {responseMessage.StatusCode}, data: {strContent}", responseMessage.StatusCode, responseString);
                if (responseMessage.StatusCode == HttpStatusCode.InternalServerError)
                {
                    exception.Data["ErrorMessage"] = responseString;
                }
                throw exception;
            }

            return responseString;
        }

        private string GetMediaType(ContentTypeEnum contentType)
        {
            switch (contentType)
            {
                case ContentTypeEnum.ApplicationJson:
                    return "application/json";
                case ContentTypeEnum.ApplicationXml:
                    return "application/xml";
                case ContentTypeEnum.TextJson:
                    return "text/json";
                case ContentTypeEnum.TextXml:
                    return "text/xml";
                default:
                    throw new NotSupportedException($"{contentType} is not supported");
            }
        }

        private byte[] StringToUTF8ByteArray(string pXmlString)
        {
            var encoding = new UTF8Encoding();
            byte[] byteArray = encoding.GetBytes(pXmlString);
            return byteArray;
        }

        private static string GenerateGetUrl(string urlWithourQueryParameters, List<KeyValuePair<string, string>> queryParameters)
        {
            var url = urlWithourQueryParameters;

            if (queryParameters != null && queryParameters.Any())
            {
                url += "?" + string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"));
            }

            return url;
        }

        #endregion
    }
}
