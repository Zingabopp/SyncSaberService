using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SimpleJSON;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Http;
using static SyncSaberService.Utilities;

namespace SyncSaberService.Web
{
    public class BeastSaverReader : IFeedReader
    {
        private string _username, _password, _loginUri;
        private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private static readonly string USERNAMEKEY = "{USERNAME}";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        private static readonly Uri FeedRootUri = new Uri("https://bsaber.com");
        private static Dictionary<int, int> _earliestEmptyPage;
        public static int EarliestEmptyPageForFeed(int feedIndex)
        {
            return _earliestEmptyPage[feedIndex];
        }
        private static object _cookieLock;
        private static CookieContainer _cookies;
        private static CookieContainer Cookies
        {
            get
            {
                return _cookies;
            }
            set
            {
                _cookies = value;
            }
        }
        private Dictionary<int, FeedInfo> _feeds;
        public Dictionary<int, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<int, FeedInfo>()
                    {
                        { 0, new FeedInfo("followings", "https://bsaber.com/members/" + USERNAMEKEY + "/wall/followings/feed/?acpage=" + PAGENUMKEY) },
                        { 1, new FeedInfo("bookmarks", "https://bsaber.com/members/" + USERNAMEKEY + "/bookmarks/feed/?acpage=" + PAGENUMKEY )},
                        { 2, new FeedInfo("curator recommended", "https://bsaber.com/members/curatorrecommended/bookmarks/feed/?acpage=" + PAGENUMKEY) }
                    };
                }
                return _feeds;
            }
        }

        public BeastSaverReader(string username, string password, string loginUri = DefaultLoginUri)
        {
            _username = username;
            _password = password;
            _loginUri = loginUri;
            _earliestEmptyPage = new Dictionary<int, int>() {
                {0, 9999 },
                {1, 9999 },
                {2, 9999 }
            };
            _cookieLock = new object();
        }

        public static CookieContainer GetBSaberCookies(string username, string password)
        {
            CookieContainer tempContainer = null;
            lock (_cookieLock)
            {
                if (_cookies != null)
                {
                    tempContainer = new CookieContainer();
                    tempContainer.Add(_cookies.GetCookies(FeedRootUri));
                }
                else
                {
                    string loginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
                    string reqString = $"log={username}&pwd={password}&rememberme=forever";
                    var tempCookies = GetCookies(loginUri, reqString);

                    _cookies = tempCookies;
                }
            }
            return Cookies;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginUri"></param>
        /// <param name="requestString"></param>
        /// <exception cref="WebException">Thrown when the web request times out</exception>
        /// <returns></returns>
        public static CookieContainer GetCookies(string loginUri, string requestString)
        {
            byte[] requestData = Encoding.UTF8.GetBytes(requestString);
            CookieContainer cc = new CookieContainer();
            var request = (HttpWebRequest) WebRequest.Create(loginUri);
            request.Proxy = null;
            request.AllowAutoRedirect = false;
            request.CookieContainer = cc;
            request.Method = "post";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = requestData.Length;
            using (Stream s = request.GetRequestStream())
                s.Write(requestData, 0, requestData.Length);
            HttpWebResponse response = (HttpWebResponse) request.GetResponse(); // Needs this to populate cookies

            return cc;
        }

        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText</exception>
        /// <returns></returns>
        public SongInfo[] GetSongsFromPage(string pageText)
        {
            List<SongInfo> songsOnPage = new List<SongInfo>();

            int totalSongsForPage = 0;
            XmlDocument xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(pageText);

            XmlNodeList xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/rss/channel/item");
            foreach (object obj in xmlNodeList)
            {
                XmlNode node = (XmlNode) obj;
                if (node["DownloadURL"] == null || node["SongTitle"] == null)
                {
                    Logger.Debug("Not a song! Skipping!");
                }
                else
                {
                    string songName = node["SongTitle"].InnerText;
                    string innerText = node["DownloadURL"].InnerText;
                    if (innerText.Contains("dl.php"))
                    {
                        Logger.Warning("Skipping BeastSaber download with old url format!");
                        totalSongsForPage++;
                    }
                    else
                    {
                        string songIndex = innerText.Substring(innerText.LastIndexOf('/') + 1);
                        string mapper = GetMapperFromBsaber(node.InnerText);
                        string songUrl = "https://beatsaver.com/download/" + songIndex;
                        SongInfo currentSong = new SongInfo(songIndex, songName, songUrl, mapper);
                        string currentSongDirectory = Path.Combine(Config.BeatSaberPath, "CustomSongs", songIndex);
                        //bool downloadFailed = false;
                        songsOnPage.Add(currentSong);
                    }
                }
            }
            return songsOnPage.ToArray();
        }

        /// <summary>
        /// Downloads the page and returns it as a string.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public string GetPageText(string url)
        {
            HttpClient hClient = new HttpClient();
            hClient.DefaultRequestHeaders.Add(HttpRequestHeader.Cookie.ToString(), GetBSaberCookies(_username, _password).GetCookieHeader(FeedRootUri));
            var pageReadTask = hClient.GetStringAsync(url); //jobClient.DownloadString(info.feedUrl);
            pageReadTask.Wait();
            string pageText = pageReadTask.Result;
            //Logger.Debug(pageText.Result);
            hClient.Dispose();
            return pageText;
        }

        public async Task<string> GetPageTextAsync(string url)
        {
            return await new Task<string>(() => GetPageText(url));
        }

        public string GetPageUrl(string feedUrlBase, int page)
        {
            string feedUrl = feedUrlBase.Replace(USERNAMEKEY, _username).Replace(PAGENUMKEY, page.ToString());
            //Logger.Debug($"Replacing {USERNAMEKEY} with {_username} in base URL:\n   {feedUrlBase}");
            return feedUrl;
        }

        public string GetPageUrl(int feedIndex, int page)
        {
            return GetPageUrl(Feeds[feedIndex].BaseUrl, page);
        }

        private static string GetMapperFromBsaber(string innerText)
        {
            //TODO: Needs testing for when a mapper's name isn't obvious
            string prefix = "Mapper: ";
            string suffix = "</p>";

            int startIndex = innerText.IndexOf(prefix);
            if (startIndex < 0)
                return "";
            startIndex += prefix.Length;
            int endIndex = innerText.IndexOf(suffix, startIndex);
            if (endIndex > startIndex && startIndex >= 0)
                return innerText.Substring(startIndex, endIndex - startIndex);
            else
                return "";
        }

        public List<SongInfo> GetSongsFromFeed(int feedIndex, int maxPages, int maxConcurrency, bool ignoreDuplicateIDs = true)
        {
            int pageIndex = 0;
            if (maxConcurrency < 1)
                maxConcurrency = 1;
            ConcurrentQueue<SongInfo> songList = new ConcurrentQueue<SongInfo>();
            Queue<FeedPageInfo> pageQueue = new Queue<FeedPageInfo>();
            var actionBlock = new ActionBlock<FeedPageInfo>(info => {
                //bool cancelJob = false;
                lock (_earliestEmptyPage)
                {
                    if (EarliestEmptyPageForFeed(feedIndex) < info.pageIndex)
                    {
                        //Logger.Debug($"Skipping page {info.pageIndex}: {info.pageIndex} > {EarliestEmptyPage.number} ");
                        return;
                        //cancelJob = true;
                    }
                }

                var pageText = GetPageText(info.feedUrl);//hClient.GetStringAsync(info.feedUrl); //jobClient.DownloadString(info.feedUrl);
                //pageText.Wait();
                //Logger.Debug(pageText.Result);
                var songsFound = GetSongsFromPage(pageText);
                if (songsFound.Count() == 0)
                {
                    Logger.Debug($"No songs found on page {info.pageIndex}");
                    lock (_earliestEmptyPage)
                    {
                        _earliestEmptyPage[feedIndex] = info.pageIndex;
                    }
                }
                else
                {
                    foreach (var song in songsFound)
                    {
                        songList.Enqueue(song);
                    }
                }
            }, new ExecutionDataflowBlockOptions {
                BoundedCapacity = maxConcurrency + 2,
                MaxDegreeOfParallelism = maxConcurrency
            });
            lock (_earliestEmptyPage)
            {
                _earliestEmptyPage[feedIndex] = 9999;
            }
            int earliestEmptyPage = 9999;
            // Keep queueing pages to check until max pages is reached, or pageIndex is greater than earliestEmptyPage
            do
            {
                pageIndex++; // Increment page index because it starts with 1.
                             //int totalSongsForPage = 0;
                             //int downloadCountForPage = 0;

                lock (_earliestEmptyPage)
                {
                    earliestEmptyPage = _earliestEmptyPage[feedIndex];
                }
                string feedUrl = GetPageUrl(Feeds[feedIndex].BaseUrl, pageIndex);

                FeedPageInfo pageInfo = new FeedPageInfo();
                pageInfo.feedToDownload = feedIndex;
                pageInfo.feedUrl = feedUrl;
                pageInfo.pageIndex = pageIndex;
                actionBlock.SendAsync(pageInfo).Wait();
                Logger.Debug($"Queued page {pageIndex} for reading. EarliestEmptyPage is {earliestEmptyPage}");
                //Logger.Debug($"FeedURL is {feedUrl}");
            }
            while ((pageIndex < maxPages || maxPages == 0) && pageIndex < earliestEmptyPage);

            while (pageQueue.Count > 0)
            {
                var page = pageQueue.Dequeue();
                actionBlock.SendAsync(page).Wait();
            }

            actionBlock.Complete();
            actionBlock.Completion.Wait();

            Logger.Debug($"Finished checking pages, found {songList.Count} songs");
            return songList.ToList();
        }

        public struct FeedPageInfo
        {
            public int feedToDownload;
            public string feedUrl;
            public int FeedIndex;
            public int pageIndex;
        }


    }
}
