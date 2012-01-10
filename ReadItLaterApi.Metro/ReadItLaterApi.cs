using System.Collections.Generic;
using System.Net;
using System.Net.Http;

using Windows.Data.Json;

using ReadItLaterApi;
using ReadItLaterApi.Metro.Http;

namespace ReadItLaterApi.Metro
{
    public class ReadItLaterApi
    {
        private const string API_BASE_URL = "https://readitlaterlist.com/v2";
        private const string TEXT_BASE_URL = "http://text.readitlaterlist.com/v2";

        private const string API_KEY = "0l7A0E49daK3do6d2cpf2bTN9eTbdd6b";

        public string Username { get; set; }
        public string Password { get; set; }

        public ReadItLaterApi(string username, string password)
        {
            Username = username;
            Password = password;
        }

        private RestClient GenerateClient(bool text = false)
        {
            return new RestClient
            {
                BaseUrl = text ? TEXT_BASE_URL : API_BASE_URL
            };
        }

        private void AddDefaultParameters(ref RestRequest request)
        {
            request.Method = HttpMethod.Post;

            request.Parameters["username"] = Username;
            request.Parameters["password"] = Password;

            request.Parameters["apikey"] =  API_KEY;

            request.Headers["Accept"] = "*/*";
        }

        public RestResponse ExecuteTextRequest(RestRequest request)
        {
            var client = GenerateClient(text: true);

            AddDefaultParameters(ref request);

            var response = client.Execute(request);

            return response;
        }

        public RestResponse Execute(RestRequest request)
        {
            var client = GenerateClient();

            AddDefaultParameters(ref request);

            var response = client.Execute(request);

            return response;
        }

        #region API calls
        public ReadingList GetReadingList()
        {
            var request = new RestRequest("get");

            var response = Execute(request);

            var json = response.HttpResponseMessage.Content.ReadAsString();

            return new ReadingList(json);
        }

        public string GetText(ReadingListItem item)
        {
            return GetText(item.Url);
        }

        public string GetText(string url)
        {
            var request = new RestRequest("text");

            request.Parameters["url" ] = url;
            request.Parameters["images"] = "1";

            return ExecuteTextRequest(request).HttpResponseMessage.Content.ReadAsString();
        }

        public bool VerifyCredentials()
        {
            var request = new RestRequest("auth");

            request.Parameters["username"] = Username;
            request.Parameters["password"] = Password;

            return Execute(request).HttpResponseMessage.StatusCode == HttpStatusCode.OK;
        }

        public bool CreateAccount()
        {
            var request = new RestRequest("signup");

            request.Parameters["username"] = Username;
            request.Parameters["password"] = Password;

            return Execute(request).HttpResponseMessage.StatusCode == HttpStatusCode.OK;
        }
        #endregion
    }
}