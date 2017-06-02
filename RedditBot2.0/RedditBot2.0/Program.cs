using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace RedditBot2._0
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var tb = new TokenBucket();
                RedditBot bot = new RedditBot("wow", "i'm cool", tb);

                var username = ConfigurationManager.AppSettings["redditUsername"];
                var password = ConfigurationManager.AppSettings["redditPassword"];
                var clientId = ConfigurationManager.AppSettings["clientId"];
                var clientSecret = ConfigurationManager.AppSettings["clientSecret"];


                bot.Authenticate(username, password, clientId, clientSecret);
                while (true)
                {
                    bot.MakeRequest("sandboxtest", 9);
                    System.Threading.Thread.Sleep(10000);
                }

                bot.DisposeClient();


            }
            catch (MissingArgument ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (IncorrectLogin ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (NoSubreddit ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (OutOfTokensExceptions ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            finally
            {
                Console.ReadKey();
            }

            
           
        }
    }
}
