using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

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
            this.HttpResponseMessage = response;
        }
    }
}