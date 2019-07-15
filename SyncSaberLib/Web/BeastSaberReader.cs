using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Http;
using SyncSaberLib.Data;
using static SyncSaberLib.Web.WebUtils;

namespace SyncSaberLib.Web
{
    public class BeastSaberReader : IFeedReader
    {
        #region Constants
        public static readonly string NameKey = "BeastSaberReader";
        public static readonly string SourceKey = "BeastSaber";
        private static readonly string USERNAMEKEY = "{USERNAME}";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private const string BeatSaverDownloadURL_Base = "https://beatsaver.com/api/download/key/";
        private static readonly Uri FeedRootUri = new Uri("https://bsaber.com");
        public const int SONGS_PER_PAGE = 50;
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
                        { (BeastSaberFeeds)1, new FeedInfo("bookmarks", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=" + USERNAMEKEY + "&page=" + PAGENUMKEY )},
                        { (BeastSaberFeeds)2, new FeedInfo("curator recommended", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=" + PAGENUMKEY) }
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
                //Cookies = GetBSaberCookies(_username, _password);
                //AddCookies(Cookies, FeedRootUri);
                for (int i = 0; i < Feeds.Keys.Count; i++)
                    _earliestEmptyPage.AddOrUpdate((int)Feeds.Keys.ElementAt(i), 9999);
                Ready = true;
            }
        }

        public BeastSaberReader(string username, int maxConcurrency)
        {
            Ready = false;
            _username = username;
            if (maxConcurrency > 0)
                _maxConcurrency = maxConcurrency;
            else
                _maxConcurrency = 5;
            _earliestEmptyPage = new Dictionary<int, int>();
        }

        [Obsolete("Login info is no longer required for Bookmarks and Followings.")]
        public BeastSaberReader(string username, string password, int maxConcurrency, string loginUri = DefaultLoginUri)
            : this(username, maxConcurrency)
        {

            _password = password;
            _loginUri = loginUri;

            _cookieLock = new object();
        }

        [Obsolete("Not necessary anymore")]
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
            var request = (HttpWebRequest)WebRequest.Create(loginUri);
            request.Proxy = null;
            request.AllowAutoRedirect = false;
            request.CookieContainer = cc;
            request.Method = "post";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = requestData.Length;
            using (Stream s = request.GetRequestStream())
                s.Write(requestData, 0, requestData.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse(); // Needs this to populate cookies

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
            List<BSaberSong> bSongs = new List<BSaberSong>();
            int totalSongsForPage = 0;
            if (pageText.ToLower().StartsWith(@"<?xml"))
            {
                bool retry = false;
                XmlDocument xmlDocument = new XmlDocument();
                do
                {
                    try
                    {
                        xmlDocument.LoadXml(pageText);
                        retry = false;
                    }
                    catch (XmlException ex)
                    {
                        if (retry == true)
                        {
                            Logger.Exception("Exception parsing XML.", ex);
                            retry = false;
                        }
                        else
                        {
                            Logger.Warning("Invalid XML formatting detected, attempting to fix...");
                            pageText = pageText.Replace(" & ", " &amp; ");
                            retry = true;
                        }
                        //File.WriteAllText("ErrorText.xml", pageText);
                    }
                } while (retry == true);
                List<Task> populateTasks = new List<Task>();
                XmlNodeList xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/rss/channel/item");
                foreach (object obj in xmlNodeList)
                {
                    XmlNode node = (XmlNode)obj;
                    if (node["DownloadURL"] == null || node["SongTitle"] == null)
                    {
                        Logger.Debug("Not a song! Skipping!");
                    }
                    else
                    {
                        // TODO: Not really using any of this except the hash.
                        string songName = node["SongTitle"].InnerText;
                        string downloadUrl = node["DownloadURL"]?.InnerText;
                        string hash = node["Hash"]?.InnerText?.ToUpper();
                        string authorName = node["LevelAuthorName"]?.InnerText;
                        string songKey = node["SongKey"]?.InnerText;
                        if (downloadUrl.Contains("dl.php"))
                        {
                            Logger.Warning("Skipping BeastSaber download with old url format!");
                            totalSongsForPage++;
                        }
                        else
                        {
                            string songIndex = !string.IsNullOrEmpty(songKey) ? songKey : downloadUrl.Substring(downloadUrl.LastIndexOf('/') + 1);
                            //string mapper = !string.IsNullOrEmpty(authorName) ? authorName : GetMapperFromBsaber(node.InnerText);
                            //string songUrl = !string.IsNullOrEmpty(downloadUrl) ? downloadUrl : BeatSaverDownloadURL_Base + songIndex;

                            if (ScrapedDataProvider.TryGetSongByKey(songIndex, out SongInfo currentSong))
                                songsOnPage.Add(currentSong);
                        }
                    }
                }
            }
            else // Page is JSON (hopefully)
            {
                JObject result = new JObject();
                try
                {
                    result = JObject.Parse(pageText);

                }
                catch (Exception ex)
                {
                    Logger.Exception("Unable to parse JSON from text", ex);
                }

                var songs = result["songs"];
                foreach (var bSong in songs)
                {
                    // Try to get the song hash from BeastSaber
                    string songHash = bSong["hash"]?.Value<string>();
                    if (!string.IsNullOrEmpty(songHash))
                    {
                        if (ScrapedDataProvider.TryGetSongByHash(songHash, out SongInfo currentSong))
                        {
                            songsOnPage.Add(currentSong);
                        }
                    }
                    else
                    {
                        // Unable to get song hash, try getting song_key from BeastSaber
                        string songKey = bSong["song_key"]?.Value<string>();
                        if (!string.IsNullOrEmpty(songKey))
                        {
                            if (ScrapedDataProvider.TryGetSongByKey(songKey, out SongInfo currentSong))
                            {
                                songsOnPage.Add(currentSong);
                            }
                            else
                            {
                                Logger.Debug($"ScrapedDataProvider could not find song: {bSong.Value<string>()}");
                            }
                        }
                        else
                        {
                            Logger.Debug($"Not a song, skipping: {bSong.ToString()}");
                        }
                    }
                }
            }
            //Task.WaitAll(populateTasks.ToArray());
            Logger.Debug($"{songsOnPage.Count} songs on the page");
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

        [Obsolete("Don't seem to need this with BeastSaber anymore.")]
        private static string GetMapperFromBsaber(string innerText)
        {
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
            if (!(settings is BeastSaberFeedSettings _settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (_settings.FeedIndex != 2 && _username.Trim() == string.Empty)
            {
                Logger.Error($"Can't access feed without a valid username in the config file");
                return new Dictionary<int, SongInfo>();
            }
            int pageIndex = 0;
            ConcurrentQueue<SongInfo> songList = new ConcurrentQueue<SongInfo>();
            //ConcurrentDictionary<int, SongInfo> songDict = new ConcurrentDictionary<int, SongInfo>();
            Queue<FeedPageInfo> pageQueue = new Queue<FeedPageInfo>();
            var actionBlock = new ActionBlock<FeedPageInfo>(info =>
            {
                //bool cancelJob = false;
                string pageText = "";
                try
                {
                    pageText = GetPageText(info.feedUrl);
                }
                catch (HttpGetException ex)
                {
                    if (ex.HttpStatusCode == HttpStatusCode.NotFound)
                    {
                        Logger.Error($"Page not found {ex.Url}");
                    }
                }
                catch (HttpRequestException)
                { }
                var songsFound = new List<SongInfo>();
                if (!string.IsNullOrEmpty(pageText))
                    songsFound = GetSongsFromPage(pageText);
                if (songsFound.Count() > 0)
                {
                    Logger.Debug($"{songsFound.Count()} songs found, incrementing EarliestEmptyPage");
                    lock (_earliestEmptyPage)
                    {
                        int newEarliest = info.pageIndex + _maxConcurrency + 1;
                        if (_earliestEmptyPage[_settings.FeedIndex] < newEarliest)
                            _earliestEmptyPage[_settings.FeedIndex] = newEarliest;
                    }
                }
                if (songsFound.Count() == 0)
                {
                    Logger.Debug($"No songs found on page {info.pageIndex}");

                }
                else
                {
                    foreach (var song in songsFound)
                    {
                        songList.Enqueue(song);
                    }

                }
            }, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = _maxConcurrency, // So pages don't get overqueued when a page with no songs is found
                MaxDegreeOfParallelism = _maxConcurrency
            });
            lock (_earliestEmptyPage)
            {
                _earliestEmptyPage[_settings.FeedIndex] = _maxConcurrency + 1;
            }
            int earliestEmptyPage = _maxConcurrency + 1;
            // Keep queueing pages to check until max pages is reached, or pageIndex is greater than earliestEmptyPage
            do
            {
                pageIndex++; // Increment page index first because it starts with 1.


                string feedUrl = GetPageUrl(Feeds[_settings.Feed].BaseUrl, pageIndex);

                FeedPageInfo pageInfo = new FeedPageInfo
                {
                    feedToDownload = _settings.FeedIndex,
                    feedUrl = feedUrl,
                    pageIndex = pageIndex
                };
                actionBlock.SendAsync(pageInfo).Wait();

                //Logger.Debug($"FeedURL is {feedUrl}");
                lock (_earliestEmptyPage)
                {
                    earliestEmptyPage = _earliestEmptyPage[_settings.FeedIndex];
                }
                Logger.Debug($"Queued page {pageIndex} for reading. EarliestEmptyPage is now {earliestEmptyPage}");
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
                if (retDict.ContainsKey(song.keyAsInt))
                {
                    /*
                    if (retDict[song.keyAsInt].SongVersion < song.SongVersion)
                    {
                        Logger.Debug($"Song with ID {song.keyAsInt} already exists, updating");
                        retDict[song.keyAsInt] = song;
                    }
                    else
                    {
                        Logger.Debug($"Song with ID {song.keyAsInt} is already the newest version");
                    }*/
                }
                else
                {
                    retDict.Add(song.keyAsInt, song);
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

    [Serializable]
    public class BSaberSong
    {
        public string title;
        public string song_key;
        public string hash;
        public string level_author_name;
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
        public BeastSaberFeeds Feed { get { return (BeastSaberFeeds)FeedIndex; } set { FeedIndex = (int)value; } }
        public bool UseSongKeyAsOutputFolder { get; set; }
        public bool searchOnline { get; set; }
        public int MaxPages { get; set; }
        private int _maxSongs;
        public int MaxSongs
        {
            get { return _maxSongs; }
            set
            {
                _maxSongs = value;
            }
        }
        public BeastSaberFeedSettings(int feedIndex, int _maxPages = 0)
        {
            MaxSongs = BeastSaberReader.SONGS_PER_PAGE * _maxPages;
            searchOnline = true;
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
