using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public interface IWebClient : IDisposable
    {
        int Timeout { get; set; }
        ErrorHandling ErrorHandling { get; set; }
        Task<IWebResponseMessage> GetAsync(Uri uri);
        Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders);
        Task<IWebResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken);
        Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders, CancellationToken cancellationToken);
        Task<IWebResponseMessage> GetAsync(string url);
        Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders);
        Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken);
        Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders, CancellationToken cancellationToken);

    }

    public enum ErrorHandling
    {
        ThrowOnException,
        ThrowOnWebFault,
        ReturnEmptyContent
    }
}
