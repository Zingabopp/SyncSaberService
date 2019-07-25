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

        public Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            //var content = new MockHttpContent(url);
#pragma warning disable CA2000 // Dispose objects before losing scope
            var response = new MockHttpResponse(uri);
#pragma warning restore CA2000 // Dispose objects before losing scope
            return Task.Run(() => { return (IWebResponseMessage)response; });
        }

        #region GetAsyncOverloads
        public Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            var urlAsUri = string.IsNullOrEmpty(url) ? null : new Uri(url);
            return GetAsync(urlAsUri, completeOnHeaders, cancellationToken);
        }
        public Task<IWebResponseMessage> GetAsync(string url)
        {
            var urlAsUri = string.IsNullOrEmpty(url) ? null : new Uri(url);
            return GetAsync(urlAsUri, false, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders)
        {
            var urlAsUri = string.IsNullOrEmpty(url) ? null : new Uri(url);
            return GetAsync(urlAsUri, completeOnHeaders, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            var urlAsUri = string.IsNullOrEmpty(url) ? null : new Uri(url);
            return GetAsync(urlAsUri, false, cancellationToken);
        }

        public Task<IWebResponseMessage> GetAsync(Uri uri)
        {
            return GetAsync(uri, false, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders)
        {
            return GetAsync(uri, completeOnHeaders, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
        {
            return GetAsync(uri, false, cancellationToken);
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
