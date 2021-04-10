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
using Goober.Http.Models.Internal;

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

            var url = HttpUtils.BuildUrlWithQueryParameters(urlWithoutQueryParameters, queryParameters);

            return Deserialize<TResponse>(value: ret,
                serializerSettings: jsonSerializerSettings,
                loggingUrl: url,
                loggingAuthenticationHeaderValue: authenticationHeaderValue,
                loggingHeaderValues: headerValues);
        }

        public async Task<string> ExecuteGetStringAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            int timeoutInMilliseconds = 120000,
            long maxContentLength = 300 * 1024)
        {
            
        }

        private async Task<(string Result, HttpRequestContextModel<HttpRequestNoContentModel> HttpRequestLoggingContext)> ExecuteGetStringInternalAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters,
            AuthenticationHeaderValue authenticationHeaderValue,
            List<KeyValuePair<string, string>> headerValues,
            int timeoutInMilliseconds,
            long maxContentLength)
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

                var ret = await GetResponseStringAndProcessResponseStatusCodeAsync(
                    httpResponse: httpResponse,
                    maxContentLength: maxContentLength,
                    loggingUrl: url,
                    loggingStrJsonContent: null,
                    loggingAuthenticationHeaderValue: authenticationHeaderValue,
                    loggingHeaderValues: headerValues
                    );

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

            var strJsonContent = Serialize(request, jsonSerializerSettings);

            return Deserialize<TResponse>(value: ret, 
                serializerSettings: jsonSerializerSettings,
                loggingUrl: url,
                loggingStrJsonContent: strJsonContent,
                loggingAuthenticationHeaderValue: authenticationHeaderValue,
                loggingHeaderValues: headerValues);
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

                var strJsonContent = Serialize(request, jsonSerializerSettings);

                httpRequest.Content = new StringContent(content: strJsonContent, Encoding.UTF8, mediaType: ApplicationJsonContentTypeValue);

                var httpResponse = await httpClient.SendAsync(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                var ret = await GetResponseStringAndProcessResponseStatusCodeAsync(httpResponse: httpResponse,
                    maxContentLength: maxContentLength,
                    loggingUrl: url,
                    loggingStrJsonContent: strJsonContent,
                    loggingAuthenticationHeaderValue: authenticationHeaderValue,
                    loggingHeaderValues: headerValues);

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

        private T Deserialize<T>(string value,
            JsonSerializerSettings serializerSettings,
            string loggingUrl,
            string loggingStrJsonContent,
            AuthenticationHeaderValue loggingAuthenticationHeaderValue,
            List<KeyValuePair<string, string>> loggingHeaderValues)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(value, serializerSettings ?? _jsonSerializerSettings);
            }
            catch (Exception exc)
            {
                var strRequest = GetRequestLoggingString(
                    url: loggingUrl,
                    strJsonContent: loggingStrJsonContent,
                    authenticationHeaderValue: loggingAuthenticationHeaderValue,
                    headerValues: loggingHeaderValues);

                throw new WebException(
                    message: $"Can't deserialize to type = \"{typeof(T).Name}\", message = \"{exc.Message}\" from value = \"{value}\", request: {{ {strRequest} }}",
                    innerException: exc);
            }
        }

        private async Task<string> GetResponseStringAndProcessResponseStatusCodeAsync(HttpResponseMessage httpResponse,
            long maxContentLength,
            string loggingUrl,
            string loggingStrJsonContent,
            AuthenticationHeaderValue loggingAuthenticationHeaderValue,
            List<KeyValuePair<string, string>> loggingHeaderValues)
        {
            string loggingStrContent;

            var responseStringResult = await ReadContentWithMaxSizeRetrictionAsync(httpResponse.Content,
            encoding: Encoding.UTF8,
            maxSize: maxContentLength);

            if (httpResponse.StatusCode == HttpStatusCode.OK
                || httpResponse.StatusCode == HttpStatusCode.Accepted
                || httpResponse.StatusCode == HttpStatusCode.Created)
            {
                if (responseStringResult.IsReadToTheEnd == true)
                {
                    return responseStringResult.StringResult.ToString();
                }

                loggingStrContent = GetRequestLoggingString(
                        url: loggingUrl, 
                        strJsonContent: loggingStrJsonContent,
                        authenticationHeaderValue: loggingAuthenticationHeaderValue,
                        headerValues: loggingHeaderValues);

                throw new WebException($"Response content length is grater then {maxContentLength}, request: {{ {loggingStrContent} }} ");
            }

            loggingStrContent = GetRequestLoggingString(url: loggingUrl,
                authenticationHeaderValue: loggingAuthenticationHeaderValue,
                headerValues: loggingHeaderValues);

            var exception = new WebException($"Request fault with statusCode = {httpResponse.StatusCode}, request: {{ {loggingStrContent} }}");

            if (responseStringResult.IsReadToTheEnd == false)
            {
                responseStringResult.StringResult.AppendLine();
                responseStringResult.StringResult.Append($"<<< NOT END, response size is greter than {maxContentLength}");
            }

            exception.Data["response"] = responseStringResult.StringResult.ToString();

            throw exception;
        }

        private async Task<(bool IsReadToTheEnd, StringBuilder StringResult)> ReadContentWithMaxSizeRetrictionAsync(HttpContent httpContent,
                    Encoding encoding,
                    long maxSize,
                    int bufferSize = 1024)
        {
            using (var stream = await httpContent.ReadAsStreamAsync())
            {
                return await ReadStreamWithMaxSizeRetrictionAsync(stream, encoding, maxSize, bufferSize);
            }
        }

        private async Task<(bool IsReadToTheEnd, StringBuilder StringResult)> ReadStreamWithMaxSizeRetrictionAsync(Stream stream,
            Encoding encoding,
            long maxSize,
            int bufferSize = 1024)
        {
            if (stream.CanRead == false)
            {
                throw new InvalidOperationException("stream is not ready to ready");
            }

            var totalBytesRead = 0;

            var sbResult = new StringBuilder();

            byte[] buffer = new byte[bufferSize];
            var bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);

            while (bytesRead > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead > maxSize)
                {
                    return (false, sbResult);
                }

                sbResult.Append(encoding.GetString(bytes: buffer, index: 0, count: bytesRead));

                bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);
            }

            return (true, sbResult);
        }

        private string GetRequestLoggingString(
            string url,
            string strJsonContent,
            AuthenticationHeaderValue authenticationHeaderValue,
            List<KeyValuePair<string, string>> headerValues)
        {
            var ret = new List<string>();

            ret.Add($"url: \"{url}\"");

            if (authenticationHeaderValue != null)
            {
                ret.Add($"authenticationHeader: {{ scheme: \"{authenticationHeaderValue.Scheme}\", parameter: \"{authenticationHeaderValue.Parameter}\" }}");
            }

            if (string.IsNullOrEmpty(strJsonContent) == false)
            {
                ret.Add($"content: {strJsonContent}");
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
