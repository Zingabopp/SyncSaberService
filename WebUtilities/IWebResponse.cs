using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.IO;
using System.Collections.ObjectModel;

namespace WebUtilities
{
    public interface IWebResponseMessage : IDisposable
    {
        HttpStatusCode StatusCode { get; }
        string ReasonPhrase { get; }
        bool IsSuccessStatusCode { get; }
        IWebResponseContent Content { get; }

        ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
    }

    public interface IWebResponseContent : IDisposable
    {

        Task<string> ReadAsStringAsync();
        Task<Stream> ReadAsStreamAsync();
        Task<byte[]> ReadAsByteArrayAsync();
        Task ReadAsFileAsync(string filePath, bool overwrite);

        string ContentType { get; }
        ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    }
}
