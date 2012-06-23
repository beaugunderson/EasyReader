using System.Collections.Generic;
using System.Net.Http;

namespace ReadItLaterApi.Metro.Http
{
    public class RestRequest
    {
        public string Url { get; set; }

        public HttpMethod Method { get; set; }

        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public RestRequest()
        {
            Method = HttpMethod.Get;
        }

        public RestRequest(string url)
        {
            Url = url;

            Method = HttpMethod.Get;
        }
    }
}