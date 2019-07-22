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
        public static FeedReaderLoggerBase Logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static bool IsInitialized { get; private set; }
        private static readonly object lockObject = new object();
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
        private static HttpClient _httpClient;
        private static HttpClient HttpClient
        {
            get
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("WebUtils was not Initialized with an HttpClient.");
                return _httpClient;
            }
            set
            {
                _httpClient = value;
            }
        }
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
        private const string RATE_LIMIT_PREFIX = "Rate-Limit";
        private static readonly string[] RateLimitKeys = new string[] { RATE_LIMIT_REMAINING_KEY, RATE_LIMIT_RESET_KEY, RATE_LIMIT_TOTAL_KEY };
        public static RateLimit ParseRateLimit(Dictionary<string, string> headers)
        {
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

        public static void Initialize(HttpClient client = null)
        {
            if (!IsInitialized)
            {
                if (client == null)
                {
                    HttpClient = new HttpClient();
                    _webClient = new HttpClientWrapper();
                }
                else
                {
                    HttpClient = client;
                    _webClient = new HttpClientWrapper(client);
                }
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Retrieves a web page as an HttpResponseMessage.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public static HttpResponseMessage GetPage(string url)
        {
            Task<HttpResponseMessage> pageGetTask;
            //lock (lockObject)
            bool goodUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri result);
            if (!goodUrl)
                throw new ArgumentException($"Error in GetPage, invalid URL: {url}");
            pageGetTask = HttpClient.GetAsync(result);
            try
            {
                pageGetTask.Wait();
            }
            catch (InvalidOperationException ex)
            {
                Logger.Exception($"Error getting page {url}", ex);
            }
            HttpResponseMessage response = pageGetTask.Result;
            //Logger.Debug(pageText.Result);
            return response;
        }

        /// <summary>
        /// Downloads the page and returns it as a string.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpGetException">Thrown when Http response code is not a success.</exception>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        [Obsolete("Use GetPage instead.")]
        public static string GetPageText(string url, bool exceptionOnHttpNotSuccessful = true)
        {
            // TODO: Change to use httpClient.GetAsync(url) so status codes can be handled and passed back
            //Task<string> pageReadTask;
            //lock (lockObject)
            string pageText = string.Empty;
            using (HttpResponseMessage pageReadTask = GetPage(url))
            {//HttpClient.GetStringAsync(url);
                //pageReadTask.Wait();
                if (!pageReadTask.IsSuccessStatusCode)
                {
                    if (exceptionOnHttpNotSuccessful)
                        throw new HttpGetException(pageReadTask.StatusCode, url, $"Exception getting page ({url}): {pageReadTask.ReasonPhrase}");
                }
                else
                    pageText = pageReadTask.Content.ReadAsStringAsync().Result;
            }
            //Logger.Debug(pageText.Result);
            return pageText;
        }

        /// <summary>
        /// Retrieves a web page as an HttpResponseMessage as an asynchronous operation.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        [Obsolete("Use WebClient.GetAsync() instead")]
        public static async Task<HttpResponseMessage> GetPageAsync(string url)
        {
            //lock (lockObject)

            HttpResponseMessage response = await HttpClient.GetAsync(url).ConfigureAwait(false);
            //Logger.Debug(pageText.Result);
            //Logger.Debug($"Got page text for {url}");
            return response;
        }

        /// <summary>
        /// Downloads the page and returns it as a string in an asynchronous operation.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        [Obsolete("Use GetPageAsync instead")]
        public static async Task<string> GetPageTextAsync(string url)
        {
            //lock (lockObject)

            string pageText = await HttpClient.GetStringAsync(url).ConfigureAwait(false);
            //Logger.Debug(pageText.Result);
            //Logger.Debug($"Got page text for {url}");
            return pageText;
        }

        //public static void AddCookies(CookieContainer newCookies, Uri uri)
        //{
        //    lock (HttpClientHandler)
        //    {
        //        if (HttpClientHandler.CookieContainer == null)
        //            HttpClientHandler.CookieContainer = newCookies;
        //        else
        //            HttpClientHandler.CookieContainer.Add(newCookies.GetCookies(uri));
        //    }
        //}

        //public static void AddCookies(CookieContainer newCookies, string url)
        //{
        //    AddCookies(newCookies, new Uri(url));
        //}

        public async static Task<string> TryGetStringAsync(string url)
        {
            HttpResponseMessage response = await HttpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            return string.Empty;
        }
    }

    public class RateLimit
    {
        public int CallsRemaining;
        public TimeSpan TimeToReset;
        public int CallsPerReset;
    }

    public class HttpGetException : Exception
    {
        public HttpStatusCode HttpStatusCode { get; private set; }
        public string Url { get; private set; }

        public HttpGetException()
            : base()
        {
            base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
            base.Data.Add("Url", string.Empty);
        }

        public HttpGetException(string message)
            : base(message)
        {

            base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
            base.Data.Add("Url", string.Empty);
        }

        public HttpGetException(string message, Exception inner)
            : base(message, inner)
        {
            base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
            base.Data.Add("Url", string.Empty);
        }

        public HttpGetException(HttpStatusCode code, string url)
            : base()
        {
            base.Data.Add("StatusCode", code);
            base.Data.Add("Url", url);
            HttpStatusCode = code;
            Url = url;
        }

        public HttpGetException(HttpStatusCode code, string url, string message)
        : base(message)
        {
            base.Data.Add("StatusCode", code);
            base.Data.Add("Url", url);
            HttpStatusCode = code;
            Url = url;
        }
    }

    // From https://stackoverflow.com/questions/45711428/download-file-with-webclient-or-httpclient
    public static class HttpContentExtensions
    {
        public static Task ReadAsFileAsync(this HttpContent content, string filename, bool overwrite)
        {
            string pathname = Path.GetFullPath(filename);
            if (!overwrite && File.Exists(filename))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
            }

            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
                return content.CopyToAsync(fileStream).ContinueWith(
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
    }
}
