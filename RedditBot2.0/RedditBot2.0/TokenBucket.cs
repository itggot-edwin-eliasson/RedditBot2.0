using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditBot2._0
{
    class OutOfTokensExceptions : Exception
    {
        public OutOfTokensExceptions(string message) : base(message)
        {

        }
    }

    class TokenBucket
    {

        private int _currentTokens, _refilAmount, _refreshRateInSeconds;
        private DateTime _startTime;

        public TokenBucket()
        {
            _currentTokens = 30;
            _refilAmount = 30;
            _refreshRateInSeconds = 60;
            _startTime = DateTime.Now;
        }

        public TokenBucket(int capacity, int refreshRateInSeconds)
        {
            _currentTokens = capacity;
            _refilAmount = capacity;
            _refreshRateInSeconds = refreshRateInSeconds;
            _startTime = DateTime.Now;
        }

        public bool RequestIsAllowed(int requestTokens)
        {

            Refill();

            if (_currentTokens < requestTokens)
            {
                Console.WriteLine("Out of tokens");
            }

            if (_currentTokens >= requestTokens)
            {
                _currentTokens -= requestTokens;
                Console.WriteLine($"{_currentTokens}");
                return true;
            }

            return false;

        }

        public bool Refill()
        {
            if ((DateTime.Now.Subtract(_startTime).TotalSeconds) >= _refreshRateInSeconds)
            {
                _currentTokens = _refilAmount;
                _startTime = DateTime.Now;
                Console.WriteLine("Full bucket again");
                return true;
            }
            return false;
        }

        public bool SendRequest()
        {
            if (RequestIsAllowed(1))
            {
                return true;
            }
            return false;
        }
    }
   
}
