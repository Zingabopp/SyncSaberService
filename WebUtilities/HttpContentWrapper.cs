using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;

namespace WebUtilities
{
    public class HttpContentWrapper : IWebResponseContent
    {
        private HttpContent _content;
        public HttpContentWrapper(HttpContent content)
        {
            _content = content;
            _headers = new Dictionary<string, IEnumerable<string>>();
            if (_content?.Headers != null)
            {
                foreach (var header in _content.Headers)
                {
                    _headers.Add(header.Key, header.Value);
                }
            }
        }

        protected Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public string ContentType { get { return _content?.Headers?.ContentType?.MediaType; } }

        public Task<byte[]> ReadAsByteArrayAsync()
        {
            return _content?.ReadAsByteArrayAsync();
        }

        public Task<Stream> ReadAsStreamAsync()
        {
            return _content?.ReadAsStreamAsync();
        }

        public Task<string> ReadAsStringAsync()
        {
            return _content?.ReadAsStringAsync();
        }

        public Task ReadAsFileAsync(string filePath, bool overwrite)
        {
            if (_content == null)
                return null;
            string pathname = Path.GetFullPath(filePath);
            if (!overwrite && File.Exists(filePath))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
            }

            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
                return _content.CopyToAsync(fileStream).ContinueWith(
                    (copyTask) =>
                    {
                        fileStream.Close();
                    });
            }
            catch
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }

                throw;
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
                    if (_content != null)
                    {
                        _content.Dispose();
                        _content = null;
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
