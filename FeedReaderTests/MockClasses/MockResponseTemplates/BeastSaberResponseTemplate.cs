using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FeedReaderTests.MockClasses.MockResponseTemplates
{
    public class BeastSaberResponseTemplate : IMockResponseTemplate
    {
        public BeastSaberResponseTemplate()
        {
            BuildResponsesDictionary();
        }

        public void LoadResponse(ref MockHttpResponse response, ref MockHttpContent content, ResponseType responseType)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response), "response cannot be null in BeastSaberResponseTemplate.LoadResponse.");
            if (content == null)
                throw new ArgumentNullException(nameof(content), "content cannot be null in BeastSaberResponseTemplate.LoadResponse.");



        }

        public Dictionary<ResponseType, Dictionary<string, object>> Responses { get; private set; }
        public Dictionary<ResponseType, Dictionary<string, object>> Contents { get; private set; }


        #region Responses
        public void BuildResponsesDictionary()
        {
            if (Responses != null)
                return;
            Responses = new Dictionary<ResponseType, Dictionary<string, object>>()
            {
                { ResponseType.Normal, GetNormalResponse() },
                { ResponseType.NotFound, GetNotFoundResponse() },
                { ResponseType.BadGateway, GetBadGatewayResponse() },
                { ResponseType.RateLimitExceeded, GetRateLimitExceededResponse() }
            };

        }

        private Dictionary<string, object> GetNormalResponse()
        {
            return new Dictionary<string, object>()
            {
                {"StatusCode", HttpStatusCode.OK },
                {"ReasonPhrase", "OK" },
                {"IsSuccessStatusCode", true }
            };
        }

        private Dictionary<string, object> GetNotFoundResponse()
        {
            return new Dictionary<string, object>()
            {
                {"StatusCode", HttpStatusCode.NotFound },
                {"ReasonPhrase", "NotFound" },
                {"IsSuccessStatusCode", false }
            };
        }

        private Dictionary<string, object> GetBadGatewayResponse()
        {
            return new Dictionary<string, object>()
            {
                {"StatusCode", HttpStatusCode.BadGateway },
                {"ReasonPhrase", "BadGateway" }, // Probably wrong
                {"IsSuccessStatusCode", false }
            };
        }

        private Dictionary<string, object> GetRateLimitExceededResponse()
        {
            return new Dictionary<string, object>()
            {
                {"StatusCode", HttpStatusCode.TooManyRequests },
                {"ReasonPhrase", "Rate limit exceeded" }, // Probably wrong, maybe doesn't exist
                {"IsSuccessStatusCode", false }
            };
        }
        #endregion

        #region Contents
        public void BuildContentsDictionary()
        {
            if (Contents != null)
                return;
            Contents = new Dictionary<ResponseType, Dictionary<string, object>>()
            {
                { ResponseType.Normal, GetNormalResponse() },
                { ResponseType.NotFound, GetNotFoundResponse() },
                { ResponseType.BadGateway, GetBadGatewayResponse() },
                { ResponseType.RateLimitExceeded, GetRateLimitExceededResponse() }
            };

        }

        private Dictionary<string, object> GetNormalContent()
        {
            return new Dictionary<string, object>()
            {
                {"StatusCode", HttpStatusCode.OK },
                {"ReasonPhrase", "OK" },
                {"IsSuccessStatusCode", true }
            };
        }
        #endregion
    }
}
