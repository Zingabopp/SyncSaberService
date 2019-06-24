using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net;

namespace SyncSaberLib.Web
{
    public static class WebUtils
    {
        private static bool _initialized = false;
        private static readonly object lockObject = new object();
        private static HttpClientHandler _httpClientHandler;
        public static HttpClientHandler HttpClientHandler
        {
            get
            {
                if (_httpClientHandler == null)
                {
                    _httpClientHandler = new HttpClientHandler();
                    HttpClientHandler.MaxConnectionsPerServer = 10;
                    HttpClientHandler.UseCookies = true;
                    HttpClientHandler.AllowAutoRedirect = true; // Needs to be false to detect Beat Saver song download rate limit
                }
                return _httpClientHandler;
            }
        }
        private static HttpClient _httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                lock (lockObject)
                {
                    if (_httpClient == null)
                    {
                        _httpClient = new HttpClient(HttpClientHandler);
                        lock (_httpClient)
                        {
                            _httpClient.Timeout = new TimeSpan(0, 0, 10);
                        }
                    }
                }
                return _httpClient;
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

        public static RateLimit ParseRateLimit(Dictionary<string, string> headers)
        {
            return new RateLimit()
            {
                CallsRemaining = int.Parse(headers[RATE_LIMIT_REMAINING_KEY]),
                TimeToReset = UnixTimeStampToDateTime(double.Parse(headers[RATE_LIMIT_RESET_KEY])) - DateTime.Now,
                CallsPerReset = int.Parse(headers[RATE_LIMIT_TOTAL_KEY])
            };
        }

        public static void Initialize(int maxConnectionsPerServer)
        {
            if (_initialized == false)
            {
                _initialized = true;
                HttpClientHandler.MaxConnectionsPerServer = maxConnectionsPerServer;
                HttpClientHandler.UseCookies = true;
                _httpClient = new HttpClient(HttpClientHandler);
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

        public static void AddCookies(CookieContainer newCookies, Uri uri)
        {
            lock (HttpClientHandler)
            {
                if (HttpClientHandler.CookieContainer == null)
                    HttpClientHandler.CookieContainer = newCookies;
                else
                    HttpClientHandler.CookieContainer.Add(newCookies.GetCookies(uri));
            }
        }

        public static void AddCookies(CookieContainer newCookies, string url)
        {
            AddCookies(newCookies, new Uri(url));
        }

        public async static Task<bool> DownloadFileAsync(string downloadUrl, string path, bool overwrite = true)
        {
            var success = true;
            var response = await WebUtils.HttpClient.GetAsync(downloadUrl).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();


            await response.Content.ReadAsFileAsync(path, overwrite).ConfigureAwait(false);
            return success;
        }

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
