using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReadItLaterApi.Metro.Http
{
    public class RestClient : IDisposable
    {
        private readonly HttpClient _client;

        public string BaseUrl { get; set; }
        public string UserAgent { get; set; }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool cleanupNativeAndManaged)
        {
            if (cleanupNativeAndManaged && _client != null)
            {
                _client.Dispose();
            }
        }

        public RestClient()
        {
            _client = new HttpClient();

            UserAgent = "C# .Net 4.5";
        }

        public RestClient(string baseUrl)
        {
            _client = new HttpClient();

            BaseUrl = baseUrl;

            UserAgent = "C# .Net 4.5";
        }

        public async Task<RestResponse> Execute(RestRequest request, bool errorOnNonSuccess = true)
        {
            var address = new Uri(BaseUrl + "/" + request.Url);

            var message = new HttpRequestMessage(request.Method, address);
            
            message.Headers.Add("User-Agent", UserAgent);

            // Add the request headers
            foreach (var header in request.Headers) {
                message.Headers.Add(header.Key, header.Value);
            }

            // Add the URL-encoded data
            message.Content = new FormUrlEncodedContent(request.Parameters);

            var response = await _client.SendAsync(message);

            if (errorOnNonSuccess)
            {
                response.EnsureSuccessStatusCode();
            }

            return new RestResponse(response);
        }
    }
}