
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Goober.Http.Utils
{
    public static class HttpUtils
    {
        public static string BuildUrl(string schemeAndHost, string urlPath)
        {
            var baseUri = new UriBuilder(new Uri(new Uri(schemeAndHost), urlPath));

            return new UriBuilder(scheme: baseUri.Scheme,
                                                host: baseUri.Host,
                                                port: baseUri.Port,
                                                path: baseUri.Path,
                                                extraValue: baseUri.Query).Uri.ToString();
        }

        public static HttpRequestMessage GenerateHttpRequestMessage(
            string requestUri,
            HttpMethod httpMethodType,
            AuthenticationHeaderValue authenticationHeaderValue = null, 
            List<KeyValuePair<string, string>> headerValues = null,
            List<string> responseMediaTypes = null)
        {
            var ret = new HttpRequestMessage();

            if (responseMediaTypes != null && responseMediaTypes.Any())
            {
                foreach (var iResponseMediaType in responseMediaTypes)
                {
                    ret.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(iResponseMediaType));
                }
            }

            if (authenticationHeaderValue != null)
            {
                ret.Headers.Authorization = authenticationHeaderValue;
            }

            if (headerValues != null && headerValues.Any())
            {
                foreach (var item in headerValues)
                {
                    ret.Headers.Add(item.Key, item.Value);
                }
            }

            ret.Method = httpMethodType;
            ret.RequestUri = new Uri(requestUri);

            return ret;
        }

        public static string BuildUrlWithQueryParameters(string urlWithoutQueryParameters, List<KeyValuePair<string, string>> queryParameters)
        {
            var url = urlWithoutQueryParameters;

            if (queryParameters != null && queryParameters.Any() == true)
            {
                return url + "?" + string.Join(separator: "&", values: queryParameters.Select(x => $"{x.Key}={x.Value}"));
            }

            return url;
        }
    }
}
