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
using System.Net.Http;
using FeedReader.Logging;

namespace FeedReader
{
    public class BeastSaberReader : IFeedReader
    {
        #region Constants
        public static readonly string NameKey = "BeastSaberReader";
        public static readonly string SourceKey = "BeastSaber";
        private static readonly string USERNAMEKEY = "{USERNAME}";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        private static readonly Dictionary<string, ContentType> ContentDictionary =
            new Dictionary<string, ContentType>() { { "text/xml", ContentType.XML }, { "application/json", ContentType.JSON } };
        private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private const string BeatSaverDownloadURL_Base = "https://beatsaver.com/api/download/key/";
        private static readonly Uri FeedRootUri = new Uri("https://bsaber.com");
        public const int SONGS_PER_XML_PAGE = 50;
        public const int SONGS_PER_JSON_PAGE = 50;
        private const string XML_TITLE_KEY = "SongTitle";
        private const string XML_DOWNLOADURL_KEY = "DownloadURL";
        private const string XML_HASH_KEY = "Hash";
        private const string XML_AUTHOR_KEY = "LevelAuthorName";
        private const string XML_SONGKEY_KEY = "SongKey";
        #endregion
        public static FeedReaderLoggerBase Logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }

        private string _username, _password, _loginUri;
        private int _maxConcurrency;

        private static Dictionary<int, int> _earliestEmptyPage;
        public static int EarliestEmptyPageForFeed(int feedIndex)
        {
            return _earliestEmptyPage[feedIndex];
        }

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
                        { (BeastSaberFeeds)1, new FeedInfo("bookmarks", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=" + USERNAMEKEY + "&page=" + PAGENUMKEY + "&count=" + SONGS_PER_JSON_PAGE)},
                        { (BeastSaberFeeds)2, new FeedInfo("curator recommended", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=" + PAGENUMKEY + "&count=" + SONGS_PER_JSON_PAGE) }
                    };
                }
                return _feeds;
            }
        }

        public void PrepareReader()
        {
            if (!Ready)
            {
                for (int i = 0; i < Feeds.Keys.Count; i++)
                    if (!_earliestEmptyPage.ContainsKey((int)Feeds.Keys.ElementAt(i)))
                        _earliestEmptyPage.Add((int)Feeds.Keys.ElementAt(i), 9999); // Do I even need this?
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

        }
        public enum ContentType
        {
            Unknown = 0,
            XML = 1,
            JSON = 2
        }

        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText</exception>
        /// <returns></returns>
        public List<ScrapedSong> GetSongsFromPageText(string pageText, ContentType contentType)
        {
            List<ScrapedSong> songsOnPage = new List<ScrapedSong>();
            //if (pageText.ToLower().StartsWith(@"<?xml"))
            if (contentType == ContentType.XML)
            {
                songsOnPage = ParseXMLPage(pageText);
            }
            else if (contentType == ContentType.JSON) // Page is JSON
            {
                songsOnPage = ParseJsonPage(pageText);
            }
            Logger.Debug($"{songsOnPage.Count} songs on the page");
            return songsOnPage;
        }

        public List<ScrapedSong> ParseXMLPage(string pageText)
        {
            bool retry = false;
            var songsOnPage = new List<ScrapedSong>();
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
                    string songName = node[XML_TITLE_KEY].InnerText;
                    string downloadUrl = node[XML_DOWNLOADURL_KEY]?.InnerText;
                    string hash = node[XML_HASH_KEY]?.InnerText?.ToUpper();
                    string authorName = node[XML_AUTHOR_KEY]?.InnerText;
                    string songKey = node[XML_SONGKEY_KEY]?.InnerText;
                    if (downloadUrl.Contains("dl.php"))
                    {
                       Logger.Warning("Skipping BeastSaber download with old url format!");
                    }
                    else
                    {
                        string songIndex = !string.IsNullOrEmpty(songKey) ? songKey : downloadUrl.Substring(downloadUrl.LastIndexOf('/') + 1);
                        //string mapper = !string.IsNullOrEmpty(authorName) ? authorName : GetMapperFromBsaber(node.InnerText);
                        //string songUrl = !string.IsNullOrEmpty(downloadUrl) ? downloadUrl : BeatSaverDownloadURL_Base + songIndex;
                        if (!string.IsNullOrEmpty(hash))
                        {
                            JObject jObject = null;
                            if (StoreRawData)
                            {
                                jObject = new JObject();
                                jObject.Add(XML_TITLE_KEY, songName);
                                jObject.Add(XML_DOWNLOADURL_KEY, downloadUrl);
                                jObject.Add(XML_HASH_KEY, hash);
                                jObject.Add(XML_AUTHOR_KEY, authorName);
                                jObject.Add(XML_SONGKEY_KEY, songKey);
                            }

                            songsOnPage.Add(new ScrapedSong(hash)
                            {
                                DownloadUrl = downloadUrl,
                                SongName = songName,
                                MapperName = authorName,
                                RawData = jObject != null ? jObject.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                            });
                        }
                    }
                }
            }
            return songsOnPage;
        }

        public List<ScrapedSong> ParseJsonPage(string pageText)
        {
            JObject result = new JObject();
            var songsOnPage = new List<ScrapedSong>();
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
                string songKey = bSong["song_key"]?.Value<string>();
                string songName = bSong["title"]?.Value<string>();
                string mapperName = bSong["level_author_name"]?.Value<string>();
                string downloadUrl = "";
                if (!string.IsNullOrEmpty(songKey))
                {
                    downloadUrl = BeatSaverDownloadURL_Base + songKey;
                }
                if (!string.IsNullOrEmpty(songHash))
                {
                    songsOnPage.Add(new ScrapedSong(songHash)
                    {
                        DownloadUrl = downloadUrl,
                        SongName = songName,
                        MapperName = mapperName,
                        RawData = StoreRawData ? bSong.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                    });
                }
                //else
                //{
                //    // Unable to get song hash, try getting song_key from BeastSaber
                //    string songKey = bSong["song_key"]?.Value<string>();
                //    if (!string.IsNullOrEmpty(songKey))
                //    {
                //        if (ScrapedDataProvider.TryGetSongByKey(songKey, out SongInfo currentSong))
                //        {
                //            songsOnPage.Add(currentSong);
                //        }
                //        else
                //        {
                //            Logger.Debug($"ScrapedDataProvider could not find song: {bSong.Value<string>()}");
                //        }
                //    }
                //    else
                //    {
                //        Logger.Debug($"Not a song, skipping: {bSong.ToString()}");
                //    }
                //}
            }
            return songsOnPage;
        }



        public string GetPageUrl(string feedUrlBase, int page)
        {
            string feedUrl = feedUrlBase.Replace(USERNAMEKEY, _username).Replace(PAGENUMKEY, page.ToString());
            Logger.Debug($"Replacing {USERNAMEKEY} with {_username} in base URL:\n   {feedUrlBase}");
            return feedUrl;
        }

        public string GetPageUrl(int feedIndex, int page)
        {
            return GetPageUrl(Feeds[(BeastSaberFeeds)feedIndex].BaseUrl, page);
        }

        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeastSaberFeedSettings.";

        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None);
        }

        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings, CancellationToken cancellationToken)
        {
            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            if (!(settings is BeastSaberFeedSettings _settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (_settings.FeedIndex != 2 && string.IsNullOrEmpty(_username?.Trim()))
            {
                Logger.Error($"Can't access feed without a valid username in the config file");
                throw new ArgumentException("Cannot access this feed without a valid username.");
            }
            int pageIndex = 0;
            List<ScrapedSong> newSongs = null;
            int maxPages = _settings.MaxPages;
            bool maxPagesSet = false;
            if (maxPages == 0 && _settings.MaxSongs == 0)
            {
                maxPagesSet = true;
            }
            do
            {
                pageIndex++; // Increment page index first because it starts with 1.
                if (newSongs != null)
                    newSongs.Clear();

                string feedUrl = GetPageUrl(Feeds[_settings.Feed].BaseUrl, pageIndex);
                string pageText = "";

                ContentType contentType;
                using (var response = await WebUtils.GetPageAsync(feedUrl))
                {
                    string contentTypeStr = response.Content.Headers.ContentType.MediaType.ToLower();
                    if (ContentDictionary.ContainsKey(contentTypeStr))
                        contentType = ContentDictionary[contentTypeStr];
                    else
                        contentType = ContentType.Unknown;
                    pageText = await response.Content.ReadAsStringAsync();

                }
                if (!maxPagesSet)
                {
                    maxPagesSet = true;
                    if (contentType == ContentType.JSON)
                    {
                        maxPages = _settings.MaxSongs / SONGS_PER_JSON_PAGE;
                        if (_settings.MaxSongs % SONGS_PER_JSON_PAGE != 0)
                            maxPages++;
                    }
                    else if (contentType == ContentType.XML)
                    {
                        maxPages = _settings.MaxSongs / SONGS_PER_XML_PAGE;
                        if (_settings.MaxSongs % SONGS_PER_XML_PAGE != 0)
                            maxPages++;
                    }
                    else
                        maxPagesSet = false;
                }
                newSongs = GetSongsFromPageText(pageText, contentType);
                foreach (var song in newSongs)
                {
                    if (retDict.ContainsKey(song.Hash))
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
                        if (retDict.Count < settings.MaxSongs)
                            retDict.Add(song.Hash, song);
                    }
                }
                //Logger.Debug($"FeedURL is {feedUrl}");
                //Logger.Debug($"Queued page {pageIndex} for reading. EarliestEmptyPage is now {earliestEmptyPage}");
            }
            while (retDict.Count < settings.MaxSongs && newSongs.Count > 0);
            //while ((pageIndex < maxPages || maxPages == 0) && newSongs.Count > 0);

            return retDict;
        }

        /// <summary>
        /// Gets all songs from the feed defined by the provided settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeastSaberFeedSettings.</exception>
        /// <exception cref="ArgumentException">Thrown when trying to access a feed that requires a username and the username wasn't provided.</exception>
        /// <returns></returns>
        public Dictionary<string, ScrapedSong> GetSongsFromFeed(IFeedSettings settings)
        {
            PrepareReader();
            if (!(settings is BeastSaberFeedSettings _settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (_settings.FeedIndex != 2 && string.IsNullOrEmpty(_username?.Trim()))
            {
                Logger.Error($"Can't access feed without a valid username in the config file");
                throw new ArgumentException("Cannot access this feed without a valid username.");
            }
            var retDict = GetSongsFromFeedAsync(settings).Result;

            return retDict;
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
