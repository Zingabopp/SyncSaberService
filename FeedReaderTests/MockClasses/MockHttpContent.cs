using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebUtilities;

namespace FeedReaderTests.MockClasses
{
    public class MockHttpContent : IWebResponseContent
    {
        public string FileSourcePath { get; private set; }
        private readonly string _contentType;

        private Dictionary<string, IEnumerable<string>> _headers;
        public MockHttpContent(string filePath, Dictionary<string, IEnumerable<string>> headers = null)
        {
            if (headers == null)
                _headers = new Dictionary<string, IEnumerable<string>>();
            FileSourcePath = filePath;
            if (!string.IsNullOrEmpty(filePath) && File.Exists(FileSourcePath))
            {
                if (FileSourcePath.EndsWith("json"))
                    _contentType = @"application/json";
                else if (FileSourcePath.EndsWith("xml"))
                    _contentType = @"text/xml";
                else
                    _contentType = @"text/html";
            }
            else
            {
                _contentType = @"text/html";
            }
            Headers = new ReadOnlyDictionary<string, IEnumerable<string>>(_headers);
        }


        #region IWebResponseContent
        public string ContentType { get { return _contentType; } }

        public ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

        public async Task<byte[]> ReadAsByteArrayAsync()
        {
            using (FileStream stream = new FileStream(FileSourcePath, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    await Task.Yield();
                    await stream.CopyToAsync(memStream).ConfigureAwait(false);
                    return memStream.ToArray();
                }
            }
        }

        public async Task ReadAsFileAsync(string filePath, bool overwrite)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            if (!overwrite && File.Exists(filePath))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", filePath));
            }
            using (var stream = File.OpenRead(FileSourcePath))
            using (var writeStream = File.OpenWrite(filePath))
            {
                await Task.Yield();
                await stream.CopyToAsync(writeStream).ConfigureAwait(false);
            }

        }

        public async Task<Stream> ReadAsStreamAsync()
        {
            FileStream stream = new FileStream(FileSourcePath, FileMode.Open, FileAccess.Read);
            await Task.Yield();
            return stream;
        }

        public async Task<string> ReadAsStringAsync()
        {
            using (FileStream stream = new FileStream(FileSourcePath, FileMode.Open, FileAccess.Read))
            using (var sr = new StreamReader(stream))
            {
                await Task.Yield();
                return await sr.ReadToEndAsync().ConfigureAwait(false);
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
                    _headers = null;
                    Headers = null;
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

        #endregion

    }
}
