using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net;
using FeedReader.Logging;
using WebUtilities;

namespace FeedReader
{
    public static class WebUtils
    {
        private static FeedReaderLoggerBase _logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static FeedReaderLoggerBase Logger { get { return _logger; } set { _logger = value; } }
        public static bool IsInitialized { get; private set; }
        //private static readonly object lockObject = new object();
        //private static HttpClientHandler _httpClientHandler;
        //public static HttpClientHandler HttpClientHandler
        //{
        //    get
        //    {
        //        if (_httpClientHandler == null)
        //        {
        //            _httpClientHandler = new HttpClientHandler();
        //            HttpClientHandler.MaxConnectionsPerServer = 10;
        //            HttpClientHandler.UseCookies = true;
        //            HttpClientHandler.AllowAutoRedirect = true; // Needs to be false to detect Beat Saver song download rate limit
        //        }
        //        return _httpClientHandler;
        //    }
        //}

        private static IWebClient _webClient;
        public static IWebClient WebClient
        {
            get
            {
                if (_webClient == null)
                    _webClient = new HttpClientWrapper();
                return _webClient;
            }

        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        private const string RATE_LIMIT_REMAINING_KEY = "Rate-Limit-Remaining";
        private const string RATE_LIMIT_RESET_KEY = "Rate-Limit-Reset";
        private const string RATE_LIMIT_TOTAL_KEY = "Rate-Limit-Total";
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1823 // Remove unused private members
        private const string RATE_LIMIT_PREFIX = "Rate-Limit";
#pragma warning restore CA1823 // Remove unused private members
#pragma warning restore IDE0051 // Remove unused private members
        private static readonly string[] RateLimitKeys = new string[] { RATE_LIMIT_REMAINING_KEY, RATE_LIMIT_RESET_KEY, RATE_LIMIT_TOTAL_KEY };
        public static RateLimit ParseRateLimit(Dictionary<string, string> headers)
        {
            if(headers == null)
                throw new ArgumentNullException(nameof(headers), "headers cannot be null for WebUtils.ParseRateLimit");
            if (RateLimitKeys.All(k => headers.Keys.Contains(k)))
                return new RateLimit()
                {
                    CallsRemaining = int.Parse(headers[RATE_LIMIT_REMAINING_KEY]),
                    TimeToReset = UnixTimeStampToDateTime(double.Parse(headers[RATE_LIMIT_RESET_KEY])) - DateTime.Now,
                    CallsPerReset = int.Parse(headers[RATE_LIMIT_TOTAL_KEY])
                };
            else
                return null;
        }

        public static void Initialize()
        {
            if (!IsInitialized)
            {
                _webClient = new HttpClientWrapper();
                IsInitialized = true;
            }
        }

        public static void Initialize(HttpClient client)
        {
            if (!IsInitialized)
            {
                if (client == null)
                {
                    _webClient = new HttpClientWrapper();
                }
                else
                {
                    _webClient = new HttpClientWrapper(client);
                }
                IsInitialized = true;
            }
        }
        public static void Initialize(IWebClient client)
        {
            if (!IsInitialized)
            {
                if (client == null)
                {
                    _webClient = new HttpClientWrapper();
                }
                else
                {
                    _webClient = client;
                }
                IsInitialized = true;
            }
        }
    }

    public class RateLimit
    {
        public int CallsRemaining { get; set; }
        public TimeSpan TimeToReset { get; set; }
        public int CallsPerReset { get; set; }
    }

    //public class HttpGetException : Exception
    //{
    //    public HttpStatusCode HttpStatusCode { get; private set; }
    //    public string Url { get; private set; }

    //    public HttpGetException()
    //        : base()
    //    {
    //        base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
    //        base.Data.Add("Url", string.Empty);
    //    }

    //    public HttpGetException(string message)
    //        : base(message)
    //    {

    //        base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
    //        base.Data.Add("Url", string.Empty);
    //    }

    //    public HttpGetException(string message, Exception inner)
    //        : base(message, inner)
    //    {
    //        base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
    //        base.Data.Add("Url", string.Empty);
    //    }

    //    public HttpGetException(HttpStatusCode code, string url)
    //        : base()
    //    {
    //        base.Data.Add("StatusCode", code);
    //        base.Data.Add("Url", url);
    //        HttpStatusCode = code;
    //        Url = url;
    //    }

    //    public HttpGetException(HttpStatusCode code, string url, string message)
    //    : base(message)
    //    {
    //        base.Data.Add("StatusCode", code);
    //        base.Data.Add("Url", url);
    //        HttpStatusCode = code;
    //        Url = url;
    //    }
    //}

    // From https://stackoverflow.com/questions/45711428/download-file-with-webclient-or-httpclient
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Downloads the provided HttpContent to the specified file.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="filename"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">Thrown when content or the filename are null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when overwrite is false and a file at the provided path already exists.</exception>
        /// <returns></returns>
        public static async Task ReadAsFileAsync(this HttpContent content, string filename, bool overwrite)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content), "content cannot be null for HttpContent.ReadAsFileAsync");
            if (string.IsNullOrEmpty(filename?.Trim()))
                throw new ArgumentNullException(nameof(filename), "filename cannot be null or empty for HttpContent.ReadAsFileAsync");
            string pathname = Path.GetFullPath(filename);
            if (!overwrite && File.Exists(filename))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
            }

            using (Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                using (Stream writeStream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await contentStream.CopyToAsync(writeStream).ConfigureAwait(false);
                }
            }
        }
    }
}
