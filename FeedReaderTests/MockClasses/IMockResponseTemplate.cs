using System;
using System.Collections.Generic;
using System.Text;

namespace FeedReaderTests.MockClasses
{
    public interface IMockResponseTemplate
    {
        void LoadResponse(ref MockHttpResponse response, ref MockHttpContent content, ResponseType responseType);

    }


    public enum ResponseType
    {
        Normal,
        NotFound,
        RateLimitExceeded,
        BadGateway
    }
}
