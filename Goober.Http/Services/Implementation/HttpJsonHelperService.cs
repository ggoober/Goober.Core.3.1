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
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

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
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxContentLength = 300 * 1024)
        {

            var ret = await ExecuteGetStringAsync(urlWithoutQueryParameters: urlWithoutQueryParameters,
                queryParameters: queryParameters,
                timeoutInMilliseconds: timeoutInMilliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: headerValues,
                maxContentLength: maxContentLength);

            if (string.IsNullOrEmpty(ret) == true)
                return default;

            return Deserialize<TResponse>(ret, jsonSerializerSettings);
        }

        public async Task<string> ExecuteGetStringAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutInMilliseconds = 120000,
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
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxContentLength = 300 * 1024)
        {
            var ret = await ExecutePostStringAsync(url: url,
                request: request,
                timeoutInMilliseconds: timeoutInMilliseconds,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: headerValues,
                jsonSerializerSettings: jsonSerializerSettings,
                maxContentLength: maxContentLength);

            if (string.IsNullOrEmpty(ret) == true)
                return default;

            return Deserialize<TResponse>(ret, jsonSerializerSettings);
        }

        public async Task<string> ExecutePostStringAsync<TRequest>(string url,
            TRequest request,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
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

        public async Task<TResponse> UploadFileAsync<TResponse>(string url,
            IFormFile file,
            string formDataFileParameterName = "file",
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long responseMaxContentLength = 30 * 1024)
        {
            var contentType = file.ContentType;

            using (var fileStream = file.OpenReadStream())
            {
                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                    var httpRequest = HttpUtils.GenerateHttpRequestMessage(
                        requestUri: url,
                        httpMethodType: HttpMethod.Post,
                        authenticationHeaderValue: authenticationHeaderValue,
                        headerValues: null,
                        responseMediaTypes: new List<string> { ApplicationJsonContentTypeValue });

                    var fileStreamContent = new StreamContent(fileStream);
                    fileStreamContent.Headers.Add("Content-Type", file.ContentType);

                    var formData = new MultipartFormDataContent();
                    formData.Add(fileStreamContent, formDataFileParameterName, file.FileName);

                    httpRequest.Content = formData;

                    foreach (var iCustomHeader in headerValues)
                    {
                        httpRequest.Content.Headers.Add(iCustomHeader.Key, iCustomHeader.Value);
                    }

                    var httpResponse = await httpClient.SendAsync(httpRequest);

                    if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                    {
                        return default;
                    }

                    var strContent = $"content: {file.FileName}, headers: {Serialize(headerValues)}";

                    await ThrowExceptionIfResponseIsNotValidAsync(httpResponse: httpResponse,
                        maxContentLength: responseMaxContentLength,
                        loggingUrl: url,
                        loggingStrContent: strContent);

                    var responseStream = await httpResponse.Content.ReadAsStreamAsync();
                    //ReadResponseWithMaxSizeRetrictionAsync(responseStream, Encoding.UTF8, url, )
                    var ret = await httpResponse.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(ret) == true)
                    {
                        return default;
                    }

                    return Deserialize<TResponse>(ret, jsonSerializerSettings);
                }
            }
        }

        #region private methods

        private static string Serialize(object value, JsonSerializerSettings serializerSettings = null)
        {
            return JsonConvert.SerializeObject(value, serializerSettings ?? _jsonSerializerSettings);
        }

        private static T Deserialize<T>(string value, 
            JsonSerializerSettings serializerSettings,
            string loggingUrl,
            AuthenticationHeaderValue authenticationHeaderValue,
            List<KeyValuePair<string, string>> headerValues)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(value, serializerSettings ?? _jsonSerializerSettings);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException(
                    message: $"Can't deserialize to type = {typeof(T).Name} from value = {value}, url = {loggingUrl}, content = {loggingStrContent}",
                    innerException: exc);
            }
        }

        private static async Task ThrowExceptionIfResponseIsNotValidAsync(HttpResponseMessage httpResponse, long maxContentLength, string loggingUrl, string loggingStrContent)
        {
            if (httpResponse.StatusCode == HttpStatusCode.OK 
                || httpResponse.StatusCode == HttpStatusCode.Accepted
                || httpResponse.StatusCode == HttpStatusCode.Created)
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

        private async Task<string> ReadResponseWithMaxSizeRetrictionAsync(Stream stream, 
            Encoding encoding,
            string loggingUrl,
            string loggingStrContent,
            int maxSize,
            int bufferSize = 1024)
        {
            if (stream.CanRead == false)
            {
                throw new InvalidOperationException("stream is not ready to ready");
            }

            var totalBytesRead = 0;

            var ret = new StringBuilder();

            byte[] buffer = new byte[bufferSize];
            var bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);

            while (bytesRead > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead > maxSize)
                {
                    throw new InvalidOperationException($"stream response size is greter than {maxSize}, url = {loggingUrl}, content = {loggingStrContent}");
                }

                ret.Append(encoding.GetString(bytes: buffer, index: 0, count: bytesRead));

                bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);

            }

            return ret.ToString();
        }

        private string GetStringContentForLoggingFromHeaders(AuthenticationHeaderValue authenticationHeaderValue,
            List<KeyValuePair<string, string>> headerValues)
        {
            var ret = new List<string>();

            if (authenticationHeaderValue != null)
            {
                ret.Add($"authenticationHeader: {{ scheme: \"{authenticationHeaderValue.Scheme}\", parameter: \"{authenticationHeaderValue.Parameter}\" }}");
            }

            if (headerValues != null && headerValues.Any() == true)
            {
                var retHeaders = new List<string>();

                foreach (var iHeaderValue in headerValues)
                {
                    retHeaders.Add($"{{ key: \"{iHeaderValue.Key}\", value: \"{iHeaderValue.Value}\" }}");
                }

                ret.Add($"headers: [{string.Join(", ", retHeaders)}]");
            }

            return string.Join(", ", ret);
        }

        #endregion
    }
}
