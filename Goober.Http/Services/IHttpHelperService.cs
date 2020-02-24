using Goober.Http.Enums;
using Goober.Http.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goober.Http.Services
{
    public interface IHttpHelperService
    {
        string BuildUrl(string schemeAndHost, string urlPath, bool joinUrlAsIs = false);
        Task<byte[]> ExecuteGetAsByteArrayAsync(string urlWithourQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int? timeoutInMilliseconds = null, Credentials credentials = null);
        Task<string> ExecuteGetAsStringAsync(string urlWithourQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int? timeoutInMilliseconds = null, Credentials credentials = null);
        Task<T> ExecuteGetAsync<T>(string urlWithourQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int? timeoutInMilliseconds = null, Credentials credentials = null, JsonSerializerSettings serializerSettings = null) where T : class;
        Task<string> ExecutePostAsStringAsync<TRequest>(string url, TRequest data, ContentTypeEnum contentType = ContentTypeEnum.ApplicationJson, int? timeoutInMilliseconds = null, Credentials credentials = null);
        Task<T> ExecutePostAsync<T, U>(string url, U bodyContent, ContentTypeEnum contentType = ContentTypeEnum.ApplicationJson, Credentials credentials = null, int? timeoutInMilliseconds = null, JsonSerializerSettings serializerSettings = null);
        Task<byte[]> ExecutePostWithByteArrayResponseAsync<TRequest>(string url, TRequest parameters, int? timeoutInMilliseconds = null);
    }
}