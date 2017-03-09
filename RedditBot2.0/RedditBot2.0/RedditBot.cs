using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedditBot2._0
{
    class RedditBot
    {
        private string _name;
        private string _description;
        private string _username, _password, _clientId, _secret;
        private Dictionary<string,string> _formData;


        public RedditBot(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public void Authenticate(string username, string password, string clientId, string secret)
        {
            _username = username;
            _password = password;
            _clientId = clientId;
            _secret = secret;
            _formData = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                    { "username", _username },
                    { "password", _password }
            };

            var clientVersion = "1.0";

            using (var client = new HttpClient())
            {
                //Configure the client
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", configClient());

                //User-Agent
                client.DefaultRequestHeaders.Add("User-Agent", $"{_name} / v{clientVersion} by {_username}");

                //Loggin form
                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "username", _username },
                    { "password", _password }
                };
                var encodedFormData = new FormUrlEncodedContent(formData);

                //AccessToken
                var authUrl = "https://www.reddit.com/api/v1/access_token";
                var response = client.PostAsync(authUrl, encodedFormData).GetAwaiter().GetResult();

                //Response code
                Console.WriteLine(response.StatusCode);

                //Actuall Token
                var responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                //Json
                var accessToken = JObject.Parse(responseData).SelectToken("access_token").ToString();

                //Update AuthenticationHeader
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);

                //Access to Reddit API
                client.GetAsync("https://oauth.reddit.com/api/v1/me").GetAwaiter().GetResult();
                responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine(responseData);
            }

        
        }

        private string configClient()
        {
            var authenticationArray = Encoding.ASCII.GetBytes($"{_clientId}:{_secret}");
            return Convert.ToBase64String(authenticationArray);


        }

    }
}
