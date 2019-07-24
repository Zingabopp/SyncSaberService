using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public class HttpClientWrapper : IWebClient
    {
        private HttpClient httpClient;
        public ILogger Logger;

        public HttpClientWrapper(HttpClient client = null)
        {
            if (client == null)
                httpClient = new HttpClient();
            else
                httpClient = client;
            ErrorHandling = ErrorHandling.ThrowOnException;
        }

        public int Timeout { get; set; }
        public ErrorHandling ErrorHandling { get; set; }

        public async Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            HttpCompletionOption completionOption = 
                completeOnHeaders ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead;
            try
            {
                //TODO: Need testing for cancellation token
                return new HttpResponseWrapper(await httpClient.GetAsync(uri, completionOption, cancellationToken).ConfigureAwait(false));
            }
            catch(ArgumentException ex)
            {
                if (ErrorHandling != ErrorHandling.ReturnEmptyContent)
                    throw;
                else
                {
                    Logger?.Log(LogLevel.Error, $"Invalid URL, {uri?.ToString()}, passed to GetAsync()\n{ex.Message}\n{ex.StackTrace}");
                    return new HttpResponseWrapper(null);
                }
            }
            catch(HttpRequestException ex)
            {
                if (ErrorHandling == ErrorHandling.ThrowOnException)
                    throw;
                else
                {
                    Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                    return new HttpResponseWrapper(null);
                }
            }

        }

        #region GetAsyncOverloads

        public Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            var urlAsUri = string.IsNullOrEmpty(url) ? null : new Uri(url);
            return GetAsync(urlAsUri, completeOnHeaders, cancellationToken);
        }
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
                    if (httpClient != null)
                    {
                        httpClient.Dispose();
                        httpClient = null;
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
