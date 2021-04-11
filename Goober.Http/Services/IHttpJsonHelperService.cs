using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Goober.Http.Services
{
    public interface IHttpJsonHelperService
    {
        Task<string> ExecuteGetReturnStringAsync(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxResponseContentLength = 300 * 1024);

        Task<TResponse> ExecuteGetAsync<TResponse>(string urlWithoutQueryParameters,
            List<KeyValuePair<string, string>> queryParameters = null,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxResponseContentLength = 300 * 1024);

        Task<string> ExecutePostReturnStringAsync<TRequest>(string url,
           TRequest request,
           AuthenticationHeaderValue authenticationHeaderValue = null,
           List<KeyValuePair<string, string>> headerValues = null,
           JsonSerializerSettings jsonSerializerSettings = null,
           int timeoutInMilliseconds = 120000,
           long maxResponseContentLength = 300 * 1024);

        Task<TResponse> ExecutePostAsync<TResponse, TRequest>(string url,
            TRequest request,
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long maxResponseContentLength = 300 * 1024);

        Task<string> UploadFileReturnStringAsync(string url,
            IFormFile file,
            string formDataFileParameterName = "file",
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long responseMaxContentLength = 300 * 1024);

        Task<TResponse> UploadFileAsync<TResponse>(string url,
            IFormFile file,
            string formDataFileParameterName = "file",
            AuthenticationHeaderValue authenticationHeaderValue = null,
            List<KeyValuePair<string, string>> headerValues = null,
            JsonSerializerSettings jsonSerializerSettings = null,
            int timeoutInMilliseconds = 120000,
            long responseMaxContentLength = 300 * 1024);
    }
}