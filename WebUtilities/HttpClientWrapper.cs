using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
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

        public async Task<IWebResponseMessage> GetAsync(string url)
        {
            try
            {
                return new HttpResponseWrapper(await httpClient.GetAsync(url).ConfigureAwait(false));
            }
            catch(ArgumentException ex)
            {
                if (ErrorHandling != ErrorHandling.ReturnEmptyContent)
                    throw;
                else
                {
                    Logger?.Log(LogLevel.Error, $"Invalid URL, {url}, passed to GetAsync()\n{ex.Message}\n{ex.StackTrace}");
                    return new HttpResponseWrapper(null);
                }
            }
            catch(HttpRequestException ex)
            {
                if (ErrorHandling == ErrorHandling.ThrowOnException)
                    throw;
                else
                {
                    Logger?.Log(LogLevel.Error, $"Exception getting {url}\n{ex.Message}\n{ex.StackTrace}");
                    return new HttpResponseWrapper(null);
                }
            }

        }

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
