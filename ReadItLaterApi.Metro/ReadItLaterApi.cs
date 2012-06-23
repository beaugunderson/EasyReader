using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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

        public async Task<RestResponse> ExecuteTextRequest(RestRequest request, bool errorOnNonSuccess = true)
        {
            var client = GenerateClient(text: true);

            AddDefaultParameters(ref request);

            var response = await client.Execute(request, errorOnNonSuccess);

            return response;
        }

        public async Task<RestResponse> Execute(RestRequest request, bool errorOnNonSuccess = true)
        {
            var client = GenerateClient();

            AddDefaultParameters(ref request);

            var response = await client.Execute(request, errorOnNonSuccess);

            return response;
        }

        #region API calls
        public async Task<ReadingList> GetReadingList()
        {
            var request = new RestRequest("get");

            var response = await Execute(request);

            var json = await response.HttpResponseMessage.Content.ReadAsStringAsync();

            return new ReadingList(json);
        }

        public async Task<string> GetText(ReadingListItem item)
        {
            var result = await GetText(item.Url);

            return result;
        }

        public async Task<string> GetText(string url)
        {
            var request = new RestRequest("text");

            request.Parameters["url" ] = url;
            request.Parameters["images"] = "1";

            var result = await ExecuteTextRequest(request, false);

            var content = await result.HttpResponseMessage.Content.ReadAsStringAsync();
            
            return content;
        }

        public async Task<bool> VerifyCredentials()
        {
            var request = new RestRequest("auth");

            request.Parameters["username"] = Username;
            request.Parameters["password"] = Password;

            var response = await Execute(request);
            
            return response.HttpResponseMessage.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> CreateAccount()
        {
            var request = new RestRequest("signup");

            request.Parameters["username"] = Username;
            request.Parameters["password"] = Password;

            //if (response.Result.StatusCode == HttpStatusCode.Forbidden && 
            //    response.Result.Headers.Contains("X-Error"))
            //{
            //    // Alert if the username was already taken
            //}

            var response = await Execute(request, false);
            
            return response.HttpResponseMessage.StatusCode == HttpStatusCode.OK;
        }
        #endregion
    }
}