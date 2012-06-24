using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ReadItLaterApi.Metro.Http;
using ReadItLaterApi.Metro.Types;

namespace ReadItLaterApi.Metro
{
    public class ReadItLaterApi
    {
        private const string API_BASE_URL = "https://readitlaterlist.com/v2";
        private const string TEXT_BASE_URL = "http://text.readitlaterlist.com/v2";

        private const string API_KEY = "0l7A0E49daK3do6d2cpf2bTN9eTbdd6b";

        private const string DIFFBOT_BASE_URL = "http://www.diffbot.com/api";

        private const string DIFFBOT_TOKEN = "6784376a66622c8ac4895dd87764c421";

        public string Username { get; set; }
        public string Password { get; set; }

        public ReadItLaterApi(string username, string password)
        {
            Username = username;
            Password = password;
        }

        private RestClient GenerateClient()
        {
            return new RestClient
            {
                BaseUrl = API_BASE_URL
            };
        }

        private RestClient GenerateDiffbotClient()
        {
            return new RestClient
            {
                BaseUrl = DIFFBOT_BASE_URL
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
            var client = GenerateDiffbotClient();

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

        public async Task<DiffbotArticle> GetText(ReadingListItem item)
        {
            var result = await GetText(item.Url);

            return result;
        }

        public async Task<DiffbotArticle> GetText(string url)
        {
            var queryString = string.Format("?token={0}&url={1}&html=true&tags=true", DIFFBOT_TOKEN, url);

            var request = new RestRequest("article" + queryString);

            var result = await ExecuteTextRequest(request, false);

            var content = await result.HttpResponseMessage.Content.ReadAsStringAsync();

            return new DiffbotArticle(content);
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