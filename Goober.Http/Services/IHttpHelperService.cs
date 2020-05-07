using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Goober.Http.Services
{
    public interface IHttpHelperService
    {
        string BuildUrl(string schemeAndHost, string urlPath);
        Task<byte[]> ExecuteGetAsByteArrayAsync(string urlWithoutQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null);
        Task<string> ExecuteGetAsStringAsync(string urlWithoutQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null);
        Task<TResponse> ExecuteGetAsync<TResponse>(string urlWithoutQueryParameters, List<KeyValuePair<string, string>> queryParameters = null, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null);
        Task<byte[]> ExecutePostAsByteArrayAsync<TRequest>(string url, TRequest request, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null);
        Task<string> ExecutePostAsStringAsync<TRequest>(string url, TRequest request, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null);
        Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string url, TRequest request, int timeoutInMilliseconds = 120000, AuthenticationHeaderValue authenticationHeaderValue = null, List<KeyValuePair<string, string>> headerValues = null);
    }
}