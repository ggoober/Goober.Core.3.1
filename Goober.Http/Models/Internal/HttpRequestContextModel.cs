using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Goober.Http.Models.Internal
{
    internal class HttpRequestContextModel<TRequest>
    {
        public string Url { get; set; }

        public List<KeyValuePair<string, string>> QueryParameters { get; set; }

        public AuthenticationHeaderValue AuthenticationHeaderValue { get; set; }

        public List<KeyValuePair<string, string>> HeaderValues { get; set; }

        public TRequest RequestContent { get; set; }

        public List<KeyValuePair<string, string>> FormContent { get; set; }
    }
}
