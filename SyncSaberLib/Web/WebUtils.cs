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
        private static object lockObject = new object();
        private static HttpClientHandler _httpClientHandler;
        public static HttpClientHandler httpClientHandler
        {
            get
            {
                if (_httpClientHandler == null)
                {
                    _httpClientHandler = new HttpClientHandler();
                    httpClientHandler.MaxConnectionsPerServer = 10;
                    httpClientHandler.UseCookies = true;
                    httpClientHandler.AllowAutoRedirect = true; // Needs to be false to detect Beat Saver song download rate limit
                }
                return _httpClientHandler;
            }
        }
        private static HttpClient _httpClient;
        public static HttpClient httpClient
        {
            get
            {
                lock (lockObject)
                {
                    if (_httpClient == null)
                    {
                        _httpClient = new HttpClient(httpClientHandler);
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
            return new RateLimit() {
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
                httpClientHandler.MaxConnectionsPerServer = maxConnectionsPerServer;
                httpClientHandler.UseCookies = true;
                _httpClient = new HttpClient(httpClientHandler);
            }
        }



        /// <summary>
        /// Downloads the page and returns it as a string.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public static string GetPageText(string url)
        {
            Task<string> pageReadTask;
            //lock (lockObject)
            pageReadTask = httpClient.GetStringAsync(url);
            pageReadTask.Wait();
            string pageText = pageReadTask.Result;
            //Logger.Debug(pageText.Result);
            return pageText;
        }

        /// <summary>
        /// Downloads the page and returns it as a string in an asynchronous operation.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public static async Task<string> GetPageTextAsync(string url)
        {
            //lock (lockObject)

            string pageText = await httpClient.GetStringAsync(url);
            //Logger.Debug(pageText.Result);
            //Logger.Debug($"Got page text for {url}");
            return pageText;
        }

        public static void AddCookies(CookieContainer newCookies, Uri uri)
        {
            lock (httpClientHandler)
            {
                if (httpClientHandler.CookieContainer == null)
                    httpClientHandler.CookieContainer = newCookies;
                else
                    httpClientHandler.CookieContainer.Add(newCookies.GetCookies(uri));
            }
        }

        public static void AddCookies(CookieContainer newCookies, string url)
        {
            AddCookies(newCookies, new Uri(url));
        }

        public async static Task DownloadFileAsync(string downloadUrl, string path, bool overwrite = true)
        {
            var downloadTask = WebUtils.httpClient.GetAsync(downloadUrl).ContinueWith((requestTask) => {
                HttpResponseMessage response = requestTask.Result;
                response.EnsureSuccessStatusCode();
                response.Content.ReadAsFileAsync(path, overwrite);
            });
            await downloadTask;
        }

        public async static Task<string> TryGetStringAsync(string url)
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            if(response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
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
                    (copyTask) => {
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
