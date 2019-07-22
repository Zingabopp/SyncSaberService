using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebUtilities
{
    public interface IWebClient : IDisposable
    {
        int Timeout { get; set; }
        ErrorHandling ErrorHandling { get; set; }
        Task<IWebResponseMessage> GetAsync(string url);

    }

    public enum ErrorHandling
    {
        ThrowOnException,
        ThrowOnWebFault,
        ReturnEmptyContent
    }
}
