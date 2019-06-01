using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Http;
using static SyncSaberService.Utilities;
using SyncSaberService.Data;
using static SyncSaberService.Web.HttpClientWrapper;

namespace SyncSaberService.Web
{
    public class BeastSaberReader : IFeedReader
    {
        #region Constants
        public static readonly string NameKey = "BeastSaberReader";
        public static readonly string SourceKey = "BeastSaber";
        private static readonly string USERNAMEKEY = "{USERNAME}";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private static readonly Uri FeedRootUri = new Uri("https://bsaber.com");
        #endregion

        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }

        private string _username, _password, _loginUri;
        private int _maxConcurrency;

        private static Dictionary<int, int> _earliestEmptyPage;
        public static int EarliestEmptyPageForFeed(int feedIndex)
        {
            return _earliestEmptyPage[feedIndex];
        }
        private static object _cookieLock = new object();
        private static CookieContainer Cookies { get; set; }

        private static Dictionary<BeastSaberFeeds, FeedInfo> _feeds;
        public static Dictionary<BeastSaberFeeds, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeastSaberFeeds, FeedInfo>()
                    {
                        { (BeastSaberFeeds)0, new FeedInfo("followings", "https://bsaber.com/members/" + USERNAMEKEY + "/wall/followings/feed/?acpage=" + PAGENUMKEY) },
                        { (BeastSaberFeeds)1, new FeedInfo("bookmarks", "https://bsaber.com/members/" + USERNAMEKEY + "/bookmarks/feed/?acpage=" + PAGENUMKEY )},
                        { (BeastSaberFeeds)2, new FeedInfo("curator recommended", "https://bsaber.com/members/curatorrecommended/bookmarks/feed/?acpage=" + PAGENUMKEY) }
                    };
                }
                return _feeds;
            }
        }

        private readonly Playlist _curatorRecommendedSongs = new Playlist("SyncSaberCuratorRecommendedPlaylist", "BeastSaber Curator Recommended", "brian91292", "1");
        private readonly Playlist _followingsSongs = new Playlist("SyncSaberFollowingsPlaylist", "BeastSaber Followings", "brian91292", "1");
        private readonly Playlist _bookmarksSongs = new Playlist("SyncSaberBookmarksPlaylist", "BeastSaber Bookmarks", "brian91292", "1");

        public Playlist[] PlaylistsForFeed(int feedIndex)
        {
            switch (feedIndex)
            {
                case 0:
                    return new Playlist[] { _followingsSongs };
                case 1:
                    return new Playlist[] { _bookmarksSongs };
                case 2:
                    return new Playlist[] { _curatorRecommendedSongs };
            }
            return new Playlist[0];
        }

        public void PrepareReader()
        {
            if (!Ready)
            {
                Cookies = GetBSaberCookies(_username, _password);
                AddCookies(Cookies, FeedRootUri);
                for (int i = 0; i < Feeds.Keys.Count; i++)
                    _earliestEmptyPage.AddOrUpdate((int)Feeds.Keys.ElementAt(i), 9999);
                Ready = true;
            }
        }

        public BeastSaberReader(string username, string password, int maxConcurrency, string loginUri = DefaultLoginUri)
        {
            Ready = false;
            _username = username;
            _password = password;
            _loginUri = loginUri;

            if (maxConcurrency > 0)
                _maxConcurrency = maxConcurrency;
            else
                _maxConcurrency = 5;
            _earliestEmptyPage = new Dictionary<int, int>();
            _cookieLock = new object();
        }

        public static CookieContainer GetBSaberCookies(string username, string password)
        {
            CookieContainer tempContainer = null;
            lock (_cookieLock)
            {
                if (Cookies != null)
                {
                    tempContainer = new CookieContainer();
                    tempContainer.Add(Cookies.GetCookies(FeedRootUri));
                }
                else
                {
                    string loginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
                    string reqString = $"log={username}&pwd={password}&rememberme=forever";
                    var tempCookies = GetCookies(loginUri, reqString);

                    Cookies = tempCookies;
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
            Logger.Debug("Requesting cookies");
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
        public List<SongInfo> GetSongsFromPage(string pageText)
        {
            List<SongInfo> songsOnPage = new List<SongInfo>();

            int totalSongsForPage = 0;
            XmlDocument xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(pageText);
            List<Task> populateTasks = new List<Task>();
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
                        //string currentSongDirectory = Path.Combine(Config.BeatSaberPath, "CustomSongs", songIndex);
                        //bool downloadFailed = false;
                        //populateTasks.Add(currentSong.PopulateFieldsAsync());
                        //SongInfo.PopulateFields(currentSong);
                        songsOnPage.Add(currentSong);
                    }
                }
            }

            //Task.WaitAll(populateTasks.ToArray());
            return songsOnPage;
        }

        public string GetPageUrl(string feedUrlBase, int page)
        {
            string feedUrl = feedUrlBase.Replace(USERNAMEKEY, _username).Replace(PAGENUMKEY, page.ToString());
            //Logger.Debug($"Replacing {USERNAMEKEY} with {_username} in base URL:\n   {feedUrlBase}");
            return feedUrl;
        }

        public string GetPageUrl(int feedIndex, int page)
        {
            return GetPageUrl(Feeds[(BeastSaberFeeds)feedIndex].BaseUrl, page);
        }

        private static string GetMapperFromBsaber(string innerText)
        {
            //TODO: Needs testing for when a mapper's name isn't obvious
            string prefix = "Mapper: ";
            string suffix = "<"; //"</p>"; Some mapper names don't end with </p>

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
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeastSaberFeedSettings.";

        /// <summary>
        /// Gets all songs from the feed defined by the provided settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeastSaberFeedSettings</exception>
        /// <returns></returns>
        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings settings)
        {
            PrepareReader();
            BeastSaberFeedSettings _settings = settings as BeastSaberFeedSettings;
            if (_settings == null)
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (_settings.FeedIndex != 2 && _username == string.Empty)
            {
                Logger.Error($"Can't access feed without a valid username and password in the config file");
                return new Dictionary<int, SongInfo>();
            }
            int pageIndex = 0;
            ConcurrentQueue<SongInfo> songList = new ConcurrentQueue<SongInfo>();
            //ConcurrentDictionary<int, SongInfo> songDict = new ConcurrentDictionary<int, SongInfo>();
            Queue<FeedPageInfo> pageQueue = new Queue<FeedPageInfo>();
            var actionBlock = new ActionBlock<FeedPageInfo>(info => {
                //bool cancelJob = false;
                var pageText = GetPageText(info.feedUrl);
                var songsFound = GetSongsFromPage(pageText);
                if (songsFound.Count() == 0)
                {
                    Logger.Debug($"No songs found on page {info.pageIndex}");
                    lock (_earliestEmptyPage)
                    {
                        _earliestEmptyPage[_settings.FeedIndex] = info.pageIndex;
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
                BoundedCapacity = _maxConcurrency, // So pages don't get overqueued when a page with no songs is found
                MaxDegreeOfParallelism = _maxConcurrency
            });
            lock (_earliestEmptyPage)
            {
                _earliestEmptyPage[_settings.FeedIndex] = 9999;
            }
            int earliestEmptyPage = 9999;
            // Keep queueing pages to check until max pages is reached, or pageIndex is greater than earliestEmptyPage
            do
            {
                pageIndex++; // Increment page index first because it starts with 1.

                lock (_earliestEmptyPage)
                {
                    earliestEmptyPage = _earliestEmptyPage[_settings.FeedIndex];
                }
                string feedUrl = GetPageUrl(Feeds[_settings.Feed].BaseUrl, pageIndex);

                FeedPageInfo pageInfo = new FeedPageInfo {
                    feedToDownload = _settings.FeedIndex,
                    feedUrl = feedUrl,
                    pageIndex = pageIndex
                };
                actionBlock.SendAsync(pageInfo).Wait();
                Logger.Debug($"Queued page {pageIndex} for reading. EarliestEmptyPage is {earliestEmptyPage}");
                //Logger.Debug($"FeedURL is {feedUrl}");
            }
            while ((pageIndex < _settings.MaxPages || _settings.MaxPages == 0) && pageIndex <= earliestEmptyPage);

            while (pageQueue.Count > 0)
            {
                var page = pageQueue.Dequeue();
                actionBlock.SendAsync(page).Wait();
            }

            actionBlock.Complete();
            actionBlock.Completion.Wait();

            Logger.Info($"Finished checking pages, found {songList.Count} songs");
            Dictionary<int, SongInfo> retDict = new Dictionary<int, SongInfo>();
            foreach (var song in songList)
            {
                if (retDict.ContainsKey(song.id))
                {
                    if (retDict[song.id].SongVersion < song.SongVersion)
                    {
                        Logger.Debug($"Song with ID {song.id} already exists, updating");
                        retDict[song.id] = song;
                    }
                    else
                    {
                        Logger.Debug($"Song with ID {song.id} is already the newest version");
                    }
                }
                else
                {
                    retDict.Add(song.id, song);
                }
            }
            return retDict;
        }

        public static int GetMaxBeastSaberPages(int feedToDownload)
        {
            switch (feedToDownload)
            {
                case 0:
                    return Config.MaxFollowingsPages;
                case 1:
                    return Config.MaxBookmarksPages;
                case 2:
                    return Config.MaxCuratorRecommendedPages;
                default:
                    return 0;
            }
        }

    }

    public struct FeedPageInfo
    {
        public int feedToDownload;
        public string feedUrl;
        public int FeedIndex;
        public int pageIndex;
    }

    public class BeastSaberFeedSettings : IFeedSettings
    {
        public string FeedName { get { return BeastSaberReader.Feeds[Feed].Name; } }
        public int FeedIndex { get; set; }
        public BeastSaberFeeds Feed { get { return (BeastSaberFeeds) FeedIndex; } set { FeedIndex = (int) value; } }
        public bool UseSongKeyAsOutputFolder { get; set; }
        public int MaxPages;
        public BeastSaberFeedSettings(int feedIndex, int _maxPages = 0)
        {
            FeedIndex = feedIndex;
            MaxPages = _maxPages;
            UseSongKeyAsOutputFolder = true;
        }
    }

    public enum BeastSaberFeeds
    {
        FOLLOWING = 0,
        BOOKMARKS = 1,
        CURATOR_RECOMMENDED = 2
    }
}
