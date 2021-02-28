using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Goober.Http.Services
{
    public interface IHttpJsonHelperService
    {
        Task<TResponse> ExecuteGetAsync<TResponse>(string urlWithoutQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null, JsonSerializerSettings jsonSerializerSettings = null, long maxContentLength = 307200);
        Task<string> ExecuteGetStringAsync(string urlWithoutQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null, long maxContentLength = 307200);
        Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string url, TRequest request, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null, JsonSerializerSettings jsonSerializerSettings = null, long maxContentLength = 307200);
        Task<string> ExecutePostStringAsync<TRequest>(string url, TRequest request, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null, JsonSerializerSettings jsonSerializerSettings = null, long maxContentLength = 307200);
    }
}