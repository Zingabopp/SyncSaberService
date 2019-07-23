using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace FeedReaderTests.MockClasses
{
    public class MockWebClient : IWebClient
    {
        public int Timeout { get; set; }
        public ErrorHandling ErrorHandling { get; set; }

        public Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            //var content = new MockHttpContent(url);
            var response = new MockHttpResponse(url);
            return Task.Run(() => { return (IWebResponseMessage)response; });
        }

        #region GetAsyncOverloads
        public Task<IWebResponseMessage> GetAsync(string url)
        {
            return GetAsync(url, false, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders)
        {
            return GetAsync(url, completeOnHeaders, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            return GetAsync(url, false, cancellationToken);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //if (httpClient != null)
                    //{
                    //    httpClient.Dispose();
                    //    httpClient = null;
                    //}
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
