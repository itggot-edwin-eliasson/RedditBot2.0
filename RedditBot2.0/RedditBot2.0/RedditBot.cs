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
    class MissingArgument : Exception
    {
        public MissingArgument(string message) : base(message)
        {

        }
    }

    class IncorrectLogin : Exception
    {
        public IncorrectLogin(string message) : base(message)
        {

        }
    }

    class NoSubreddit : Exception
    {
        public NoSubreddit(string message) : base(message)
        {

        }
    }

    class RedditBot
    {
        private string _name;
        private string _description;
        private string _username, _password, _clientId, _secret;
        private Dictionary<string,string> _formData;
        private string _authUrl = "https://www.reddit.com/api/v1/access_token";
        private TokenBucket _tb;
        private System.Net.Http.Headers.AuthenticationHeaderValue _token;

        HttpClient _client = new HttpClient();


        public RedditBot(string name, string description, TokenBucket tb)
        {
            _name = name;
            _description = description;
            _tb = tb;
        }
        /// <summary>
        /// Uses other methods to authorize the user.
        /// </summary>
        /// <param name="username">Users Reddit username</param>
        /// <param name="password">Users Reddit password</param>
        /// <param name="clientId">Users bots Reddit client id string</param>
        /// <param name="secret">Users bots Reddit secret string</param>
        public void Authenticate(string username, string password, string clientId, string secret)
        {
            if (username == null || password == null || clientId == null || secret == null)
            {
                throw new MissingArgument("Missing argument");
            }
            else
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
            }
            var clientVersion = "1.0";

            ConfigClient(_client);

            ConfigUserAgent(_client, clientVersion);

            var encodedFormData = ClientForm();
            
            var response = ClientResponse(_client, encodedFormData);

            Console.WriteLine(response.StatusCode);

            var responseData = ClientResponseData(response);

            UpdateClient(_client, responseData);

            Console.WriteLine(AccessReddit(_client, response));



            
        
        }


        /// <summary>
        /// Sets clients header authorization to basic together with the client and secret
        /// </summary>
        /// <param name="client">HttpClient used for authentication</param>
        private void ConfigClient(HttpClient client)
        {
            var authenticationArray = Encoding.ASCII.GetBytes($"{_clientId}:{_secret}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authenticationArray));

        }

        /// <summary>
        /// Creates a unique User-Agent to clients header
        /// </summary>
        /// <param name="client">HttpClient used for authentication</param>
        /// <param name="clientVersion">Version of client</param>
        private void ConfigUserAgent(HttpClient client, string clientVersion)
        {
            client.DefaultRequestHeaders.Add("User-Agent", $"{_name} / v{clientVersion} by {_username}");

        }

        /// <summary>
        /// Turns a dictionary with login info into an encoded form
        /// </summary>
        /// <returns>FormUrlEncodedContent from a dictionary. Makes unviewable for spies</returns>
        public FormUrlEncodedContent ClientForm()
        {
            return new FormUrlEncodedContent(_formData);
        }

        /// <summary>
        /// Sends the login form to a dedicated url (in this case reddit) together with the client.
        /// </summary>
        /// <param name="client">HttpClient used for authentication</param>
        /// <param name="encodedFormData">Encoded form for login data</param>
        /// <returns>A response message from Reddit whether or not your authorization are valid</returns>
        private HttpResponseMessage ClientResponse(HttpClient client, FormUrlEncodedContent encodedFormData)
        {
            return client.PostAsync(_authUrl, encodedFormData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Takes resonse and turns it into a string
        /// </summary>
        /// <param name="response">Result from ClientResponse</param>
        /// <returns>A Json formated string with the content from ClientResponse</returns>
        private string ClientResponseData(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets clients authorization header to bearer and accesstoken from the Token method
        /// </summary>
        /// <param name="client">HttpClient used for authentication</param>
        /// <param name="responseData">Json formated string from ClientResponseData</param>
        private void UpdateClient(HttpClient client, string responseData)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Token(responseData));
            // _token = client.DefaultRequestHeaders.Authorization;
            _token = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Token(responseData));
        }

        /// <summary>
        /// Extracts the access token from the Json formated string. If something is wrong it will show up here.
        /// </summary>
        /// <param name="responseData">Json formated string</param>
        /// <returns>The value of access-token in responseData</returns>
        private string Token(string responseData)
        {
            var accessToken = JObject.Parse(responseData).SelectToken("access_token");

            if (accessToken == null)
            {
                throw new IncorrectLogin("Wrong username or password!");
            }

            return accessToken.ToString();


        }

        /// <summary>
        /// Doublechecks if everything works as intended.
        /// </summary>
        /// <param name="client">Http</param>
        /// <param name="response"></param>
        /// <returns></returns>
        private string AccessReddit(HttpClient client, HttpResponseMessage response)
        {
            client.GetAsync("https://oauth.reddit.com/api/v1/me").GetAwaiter().GetResult();
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Asks for the numberOfRequest latest posts to a subreddit and then extracts the permalink to then send them to GetCommentsFromPost.
        /// </summary>
        /// <param name="subReddit">String containing the name of a Subreddit</param>
        /// <param name="numberOfRequests">Integer containing the amounts of post to get</param>
        public void MakeRequest(string subReddit,int numberOfRequests)
        {

            if (_tb.SendRequest())
            {
                var result = _client.GetAsync($"https://oauth.reddit.com/r/{subReddit}/new.json?sort=new&limit={numberOfRequests}").GetAwaiter().GetResult();
                var text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (text.Contains("<html>"))
                {
                    throw new NoSubreddit("There is no Subreddit by that name!");
                }

                for (var i = 0; i < numberOfRequests; i++)
                {
                    var permalink = JObject.Parse(text).SelectToken($"data.children[{i}].data.permalink").ToString();

                    

                    //Console.WriteLine(permalink);

                    GetCommentsFromPost(permalink);
                }
            }
            else
            {
                System.Threading.Thread.Sleep(10000);
                MakeRequest(subReddit, numberOfRequests);

            }
           
            

        }

        /// <summary>
        /// Asks for one of the specific post, extracts its comments and sends them of to CheckForReplies
        /// </summary>
        /// <param name="permalinkResult">String containing the permalink id of a specific post</param>
        public void GetCommentsFromPost(string permalinkResult)
        {
            if (_tb.SendRequest())
            {
                var post = _client.GetAsync(String.Format("https://oauth.reddit.com/{0}.json", permalinkResult)).GetAwaiter().GetResult();
                var textPost = post.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                var amountOfCommentsInPost = JArray.Parse(textPost).First.Next.SelectToken("data.children").Count();
                var allCommentsFromPost = JArray.Parse(textPost).First.Next;


                //Console.WriteLine(amountOfCommentsInPost);
                if (allCommentsFromPost.ToString() != "")
                {
                    CheckForReplies(allCommentsFromPost, amountOfCommentsInPost);

                }
            }
            else
            {
                System.Threading.Thread.Sleep(10000);
                GetCommentsFromPost(permalinkResult);
            }
           
        }

        /// <summary>
        /// Checks if the comment has replies and if it has replies it calls itself.
        /// It also calls PostComment if certain requirements are fullfilled.
        /// </summary>
        /// <param name="commentsOrReplies">JToken containing all data for a specific comment</param>
        /// <param name="numberOfChildren">Integer containing the number of replies or comments on a comment or post</param>
        public void CheckForReplies(JToken commentsOrReplies, int numberOfChildren)
        {
            for (var i = 0; i < numberOfChildren; i++)
            {
                var replies = commentsOrReplies.SelectToken($"data.children[{i}].data.replies");
                var commentText = commentsOrReplies.SelectToken($"data.children[{i}].data.body");
                var comment = commentsOrReplies.SelectToken($"data.children[{i}].data");
                if (replies.ToString() == "" && CheckContent(commentText))
                {
                    if (CheckIfMadeComment(comment))
                    {
                        PostComment(comment);
                    }
                }
                else if (CheckContent(commentText) && CheckIfAnswered(comment, replies.SelectToken("data.children").Count()))
                {
                    if (CheckIfMadeComment(comment))
                    {
                        PostComment(comment);
                    }
                }

                //Console.WriteLine(commentText);
                if (replies != null)
                {
                    if (replies.SelectToken("data.children") != null && replies.SelectToken($"data.children[0].kind").ToString() != "more" && replies.ToString() != "")
                    {
                        var numberOfReplies = replies.SelectToken("data.children").Count();
                        //Console.WriteLine(numberOfReplies);

                        CheckForReplies(replies, numberOfReplies);
                    }
                }
               


            }
        }

        /// <summary>
        /// Checks if the comment contains either rick or morty. If it does it returns true.
        /// </summary>
        /// <param name="commentText">String containing the content of a comment.</param>
        /// <returns>A boolean, true if it contains the chosen string, false if it doesn't</returns>
        public bool CheckContent(JToken commentText)
        {
            if (commentText.ToString().ToLower().Contains("rick"))
            {
                return true;
            }
            else if (commentText.ToString().ToLower().Contains("morty"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the comment has already been answered by the user
        /// </summary>
        /// <param name="comment">JToken containing the nececssary data of the comment</param>
        /// <param name="numberOfReplies">Integer containing the number of replies to the comment</param>
        /// <returns>A boolean, true if it hasn't been answered, false if it has been.</returns>
        public bool CheckIfAnswered(JToken comment, int numberOfReplies)
        {
            var value = new bool();
            for (var i = 0; i < numberOfReplies; i++)
            {
                var author = comment.SelectToken($"replies.data.children[{i}].data.author");
                if (author.ToString() != _username && author != null)
                {
                     value = true;
                }
                else
                {
                    return false;
                }
            }
            return value;
        }

        /// <summary>
        /// Checks if it was the user that made the comment
        /// </summary>
        /// <param name="comment">JToken containing the data of the comment</param>
        /// <returns>A boolean, true if the user didn't make the comment, false otherwise</returns>
        public bool CheckIfMadeComment(JToken comment)
        {
            var author = comment.SelectToken("author");
            if (author.ToString() != _username)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Answeres the comment with a quote.
        /// </summary>
        /// <param name="comment">JToken containing the data of the comment</param>
        public void PostComment(JToken comment)
        {

            if (_tb.SendRequest())
            {
                var formData = new Dictionary<string, string>
                {
                    {"api_type", "json" },
                    {"text", "WUBBA LUBBA DUB DUB - Rick"},
                    {"thing_id", comment.SelectToken("name").ToString() }
                };
                var encodedFormData = new FormUrlEncodedContent(formData);
                var authUrl = "https://oauth.reddit.com/api/comment";
                var response = _client.PostAsync(authUrl, encodedFormData).GetAwaiter().GetResult();
                Console.WriteLine(response.StatusCode);
                Console.WriteLine($"Your response to {comment.SelectToken("body")} is posted!");
            }
            else
            {
                System.Threading.Thread.Sleep(10000);
                PostComment(comment);
            }
            

        }



        public void DisposeClient()
        {
            _client.Dispose();
        }

        
        
    }
}
