using System.Net.Http;

namespace ReadItLaterApi.Metro.Http
{
    public class RestResponse
    {
        public HttpResponseMessage HttpResponseMessage;

        public RestResponse()
        {
        }

        public RestResponse(HttpResponseMessage response)
        {
            HttpResponseMessage = response;
        }
    }
}