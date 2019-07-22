using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text;

namespace WebUtilities
{
    public class HttpResponseWrapper : IWebResponseMessage
    {
        private HttpResponseMessage _response;
        public HttpStatusCode StatusCode { get { return _response.StatusCode; } }

        public bool IsSuccessStatusCode { get { return _response.IsSuccessStatusCode; } }

        public IWebResponseContent Content { get; protected set; }

        private ReadOnlyDictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public HttpResponseWrapper(HttpResponseMessage response)
        {
            _response = response;
            Content = new HttpContentWrapper(response.Content);
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Content != null)
                    {
                        Content.Dispose();
                        Content = null;
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
