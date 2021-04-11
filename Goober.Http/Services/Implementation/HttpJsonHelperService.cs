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
using Goober.Http.Models.Internal;
using Microsoft.AspNetCore.Http;

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

        private async Task<string> ExecuteGetReturnStringInternalAsync(
            HttpRequestContextModel<HttpRequestNoContentModel> requestContext,
            int timeoutInMilliseconds,
            long maxResponseContentLength)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var url = HttpUtils.BuildUrlWithQueryParameters(requestContext.Url, requestContext.QueryParameters);

                var httpRequest = HttpUtils.GenerateHttpRequestMessage(
                    requestUrl: url,
                    httpMethodType: HttpMethod.Get,
                    authenticationHeaderValue: requestContext.AuthenticationHeaderValue,
                    headerValues: requestContext.HeaderValues,
                    responseMediaTypes: new List<string> { ApplicationJsonContentTypeValue });

                var httpResponse = await httpClient.SendAsync(httpRequest);
                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                var ret = await GetResponseStringAndProcessResponseStatusCodeAsync(
                    httpResponse: httpResponse,
                    loggingRequestContext: requestContext,
                    maxResponseContentLength: maxResponseContentLength);

                return ret;
            }
        }

        public async Task<string> ExecuteGetReturnStringAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxResponseContentLength = 300 * 1024)
        {
            var requestContext = new HttpRequestContextModel<HttpRequestNoContentModel>
            {
                Url = urlWithoutQueryParameters,
                HttpMethod = HttpMethod.Get,
                QueryParameters = queryParameters,
                AuthenticationHeaderValue = authenticationHeaderValue,
                HeaderValues = headerValues,
                JsonSerializerSettings = jsonSerializerSettings ?? _jsonSerializerSettings
            };

            var ret = await ExecuteGetReturnStringInternalAsync(requestContext: requestContext,
                timeoutInMilliseconds: timeoutInMilliseconds,
                maxResponseContentLength: maxResponseContentLength);

            return ret;
        }

        public async Task<TResponse> ExecuteGetAsync<TResponse>(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxResponseContentLength = 300 * 1024)
        {
            var requestContext = new HttpRequestContextModel<HttpRequestNoContentModel>
            {
                Url = urlWithoutQueryParameters,
                HttpMethod = HttpMethod.Get,
                QueryParameters = queryParameters,
                AuthenticationHeaderValue = authenticationHeaderValue,
                HeaderValues = headerValues,
                JsonSerializerSettings = jsonSerializerSettings ?? _jsonSerializerSettings
            };

            var strRet = await ExecuteGetReturnStringInternalAsync(requestContext: requestContext,
                timeoutInMilliseconds: timeoutInMilliseconds,
                maxResponseContentLength: maxResponseContentLength);

            if (string.IsNullOrEmpty(strRet) == true)
                return default;

            var ret = Deserialize<TResponse, HttpRequestNoContentModel>(value: strRet,
                jsonSerializerSettings: requestContext.JsonSerializerSettings,
                loggingRequestContext: requestContext);

            return ret;
        }


        private async Task<string> ExecutePostReturnStringInternalAsync<TRequest>(
           HttpRequestContextModel<TRequest> requestContext,
           int timeoutInMilliseconds,
           long maxResponseContentLength)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                var httpRequest = HttpUtils.GenerateHttpRequestMessage(
                    requestUrl: requestContext.Url,
                    httpMethodType: HttpMethod.Post,
                    authenticationHeaderValue: requestContext.AuthenticationHeaderValue,
                    headerValues: requestContext.HeaderValues,
                    responseMediaTypes: new List<string> { ApplicationJsonContentTypeValue });

                var strJsonContent = Serialize(requestContext.RequestContent, requestContext.JsonSerializerSettings);

                httpRequest.Content = new StringContent(content: strJsonContent, Encoding.UTF8, mediaType: ApplicationJsonContentTypeValue);

                var httpResponse = await httpClient.SendAsync(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                var ret = await GetResponseStringAndProcessResponseStatusCodeAsync(
                    httpResponse: httpResponse,
                    loggingRequestContext: requestContext,
                    maxResponseContentLength: maxResponseContentLength);

                return ret;
            }
        }

        public async Task<string> ExecutePostReturnStringAsync<TRequest>(string url,
           TRequest request,
           AuthenticationHeaderValue authenticationHeaderValue = null,
           List<KeyValuePair<string, string>> headerValues = null,
           JsonSerializerSettings jsonSerializerSettings = null,
           int timeoutInMilliseconds = 120000,
           long maxResponseContentLength = 300 * 1024)
        {
            var requestContext = new HttpRequestContextModel<TRequest>
            {
                Url = url,
                HttpMethod = HttpMethod.Post,
                QueryParameters = null,
                AuthenticationHeaderValue = authenticationHeaderValue,
                HeaderValues = headerValues,
                JsonSerializerSettings = jsonSerializerSettings ?? _jsonSerializerSettings,
                RequestContent = request
            };

            var ret = await ExecutePostReturnStringInternalAsync(
                requestContext: requestContext,
                timeoutInMilliseconds: timeoutInMilliseconds,
                maxResponseContentLength: maxResponseContentLength);

            return ret;
        }

        public async Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string url,
            TRequest request,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxResponseContentLength = 300 * 1024)
        {
            var requestContext = new HttpRequestContextModel<TRequest>
            {
                Url = url,
                HttpMethod = HttpMethod.Post,
                QueryParameters = null,
                AuthenticationHeaderValue = authenticationHeaderValue,
                HeaderValues = headerValues,
                JsonSerializerSettings = jsonSerializerSettings ?? _jsonSerializerSettings,
                RequestContent = request
            };

            var strRet = await ExecutePostReturnStringInternalAsync(
               requestContext: requestContext,
               timeoutInMilliseconds: timeoutInMilliseconds,
               maxResponseContentLength: maxResponseContentLength);

            if (string.IsNullOrEmpty(strRet) == true)
                return default;

            return Deserialize<TResponse, TRequest>(
                value: strRet,
                jsonSerializerSettings: requestContext.JsonSerializerSettings,
                loggingRequestContext: requestContext);
        }

        public async Task<TResponse> UploadFileAsync<TResponse>(string url,
            IFormFile file,
            string formDataFileParameterName = "file",
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long responseMaxContentLength = 300 * 1024)
        {
            var strRet = await UploadFileReturnStringAsync(url: url,
                file: file,
                formDataFileParameterName: formDataFileParameterName,
                authenticationHeaderValue: authenticationHeaderValue,
                headerValues: headerValues,
                jsonSerializerSettings: jsonSerializerSettings,
                timeoutInMilliseconds: timeoutInMilliseconds,
                responseMaxContentLength: responseMaxContentLength);

            if (string.IsNullOrEmpty(strRet) == true)
                return default;

            var loggingRequestContext = new HttpRequestContextModel<HttpRequestNoContentModel>
            {
                Url = url,
                HttpMethod = HttpMethod.Post,
                AuthenticationHeaderValue = authenticationHeaderValue,
                HeaderValues = headerValues,
                JsonSerializerSettings = jsonSerializerSettings ?? _jsonSerializerSettings,
                Files = new List<string> { $"{formDataFileParameterName}:{file.FileName};contentType:{file.ContentType};fileLength:{file.Length}" }
            };

            return Deserialize<TResponse, HttpRequestNoContentModel>(
                value: strRet,
                jsonSerializerSettings: loggingRequestContext.JsonSerializerSettings,
                loggingRequestContext: loggingRequestContext);
        }

        public async Task<string> UploadFileReturnStringAsync(string url,
            IFormFile file,
            string formDataFileParameterName = "file",
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long responseMaxContentLength = 300 * 1024)
        {
            using (var fileStream = file.OpenReadStream())
            {
                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                    var httpRequest = HttpUtils.GenerateHttpRequestMessage(
                        requestUrl: url,
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

                    var loggingRequestContext = new HttpRequestContextModel<HttpRequestNoContentModel>
                    {
                        Url = url,
                        HttpMethod = HttpMethod.Post,
                        AuthenticationHeaderValue = authenticationHeaderValue,
                        HeaderValues = headerValues,
                        JsonSerializerSettings = jsonSerializerSettings ?? _jsonSerializerSettings,
                        Files = new List<string> { $"{formDataFileParameterName}:{file.FileName};contentType:{file.ContentType};fileLength:{file.Length}" }
                    };

                    var ret = await GetResponseStringAndProcessResponseStatusCodeAsync(
                        httpResponse: httpResponse,
                        loggingRequestContext: loggingRequestContext,
                        maxResponseContentLength: responseMaxContentLength);

                    return ret;
                }
            }
        }

        #region private methods

        private static string Serialize(object value, JsonSerializerSettings serializerSettings = null)
        {
            return JsonConvert.SerializeObject(value, serializerSettings ?? _jsonSerializerSettings);
        }

        private TTarget Deserialize<TTarget, TRequest>(string value,
            JsonSerializerSettings jsonSerializerSettings,
            HttpRequestContextModel<TRequest> loggingRequestContext)
        {
            try
            {
                return JsonConvert.DeserializeObject<TTarget>(value, jsonSerializerSettings ?? _jsonSerializerSettings);
            }
            catch (Exception exc)
            {
                throw new WebException(
                    message: $"Can't deserialize to type = \"{typeof(TTarget).Name}\", message = \"{exc.Message}\" from value = \"{value}\", request: {Serialize(loggingRequestContext, loggingRequestContext.JsonSerializerSettings)}",
                    innerException: exc);
            }
        }

        private async Task<string> GetResponseStringAndProcessResponseStatusCodeAsync<TRequest>(HttpResponseMessage httpResponse,
            HttpRequestContextModel<TRequest> loggingRequestContext,
            long maxResponseContentLength)
        {
            var responseStringResult = await ReadContentWithMaxSizeRetrictionAsync(httpResponse.Content,
                encoding: Encoding.UTF8,
                maxSize: maxResponseContentLength);

            if (httpResponse.StatusCode == HttpStatusCode.OK
                || httpResponse.StatusCode == HttpStatusCode.Accepted
                || httpResponse.StatusCode == HttpStatusCode.Created)
            {
                if (responseStringResult.IsReadToTheEnd == true)
                {
                    return responseStringResult.StringResult.ToString();
                }

                throw new WebException($"Response content length is grater then {maxResponseContentLength}, request: {Serialize(loggingRequestContext, loggingRequestContext.JsonSerializerSettings)} ");
            }
            
            var exception = new WebException($"Request fault with statusCode = {httpResponse.StatusCode}, response: {responseStringResult.StringResult}, request: {Serialize(loggingRequestContext, loggingRequestContext.JsonSerializerSettings)}");

            if (responseStringResult.IsReadToTheEnd == false)
            {
                responseStringResult.StringResult.AppendLine();
                responseStringResult.StringResult.Append($"<<< NOT END, response size is greter than {maxResponseContentLength}");
            }

            throw exception;
        }

        private async Task<(bool IsReadToTheEnd, StringBuilder StringResult)> ReadContentWithMaxSizeRetrictionAsync(HttpContent httpContent,
                    Encoding encoding,
                    long maxSize,
                    int bufferSize = 1024)
        {
            using (var stream = await httpContent.ReadAsStreamAsync())
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
        }

        #endregion
    }
}
