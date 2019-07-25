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
using FeedReader.Logging;
using System.Diagnostics;
using WebUtilities;

namespace FeedReader
{
    public class BeastSaberReader : IFeedReader
    {
        #region Constants
        public static readonly string NameKey = "BeastSaberReader";
        public static readonly string SourceKey = "BeastSaber";
        private const string USERNAMEKEY = "{USERNAME}";
        private const string PAGENUMKEY = "{PAGENUM}";
        private static readonly Dictionary<string, ContentType> ContentDictionary =
            new Dictionary<string, ContentType>() { { "text/xml", ContentType.XML }, { "application/json", ContentType.JSON } };
        //private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private const string BeatSaverDownloadURL_Base = "https://beatsaver.com/api/download/key/";
        public Uri RootUri { get { return new Uri("https://bsaber.com"); } }
        public const int SongsPerXmlPage = 50;
        public const int SongsPerJsonPage = 50;
        private const string XML_TITLE_KEY = "SongTitle";
        private const string XML_DOWNLOADURL_KEY = "DownloadURL";
        private const string XML_HASH_KEY = "Hash";
        private const string XML_AUTHOR_KEY = "LevelAuthorName";
        private const string XML_SONGKEY_KEY = "SongKey";
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeastSaberFeedSettings.";
        #endregion

        private static FeedReaderLoggerBase _logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static FeedReaderLoggerBase Logger { get { return _logger; } set { _logger = value; } }
        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }
        private int _maxConcurrency;

        /// <summary>
        /// Sets the maximum number of simultaneous page checks.
        /// </summary>
        ///<exception cref="ArgumentOutOfRangeException">Thrown when setting MaxConcurrency less than 1.</exception>
        public int MaxConcurrency
        {
            get { return _maxConcurrency; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("MaxConcurrency", value, "MaxConcurrency must be >= 1.");
                _maxConcurrency = value;
            }
        }

        private static Dictionary<BeastSaberFeed, FeedInfo> _feeds;
        public static Dictionary<BeastSaberFeed, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeastSaberFeed, FeedInfo>()
                    {
                        { (BeastSaberFeed)0, new FeedInfo("followings", "https://bsaber.com/members/" + USERNAMEKEY + "/wall/followings/feed/?acpage=" + PAGENUMKEY) },
                        { (BeastSaberFeed)1, new FeedInfo("bookmarks", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=" + USERNAMEKEY + "&page=" + PAGENUMKEY + "&count=" + SongsPerJsonPage)},
                        { (BeastSaberFeed)2, new FeedInfo("curator recommended", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=" + PAGENUMKEY + "&count=" + SongsPerJsonPage) }
                    };
                }
                return _feeds;
            }
        }

        public void PrepareReader()
        {
            if (!Ready)
            {
                Ready = true;
            }
        }

        public BeastSaberReader(string username, int maxConcurrency = 0)
        {
            Ready = false;
            Username = username;
            MaxConcurrency = maxConcurrency;
        }

        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText</exception>
        /// <returns></returns>
        public List<ScrapedSong> GetSongsFromPageText(string pageText, Uri sourceUri, ContentType contentType)
        {
            List<ScrapedSong> songsOnPage = new List<ScrapedSong>();
            //if (pageText.ToLower().StartsWith(@"<?xml"))
            if (contentType == ContentType.XML)
            {
                songsOnPage = ParseXMLPage(pageText, sourceUri);
            }
            else if (contentType == ContentType.JSON) // Page is JSON
            {
                songsOnPage = ParseJsonPage(pageText, sourceUri);
            }
            //Logger.Debug($"{songsOnPage.Count} songs on page at {sourceUrl}");
            return songsOnPage;
        }

        /// <summary>
        /// Most of this yoinked from Brian's SyncSaber.
        /// https://github.com/brian91292/SyncSaber/blob/master/SyncSaber/SyncSaber.cs#L259
        /// </summary>
        /// <param name="pageText"></param>
        /// <returns></returns>
        public List<ScrapedSong> ParseXMLPage(string pageText, Uri sourceUrl)
        {
            if (string.IsNullOrEmpty(pageText))
                return new List<ScrapedSong>();
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
                                DownloadUri = Util.GetUriFromString(downloadUrl),
                                SourceUri = sourceUrl,
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

        public List<ScrapedSong> ParseJsonPage(string pageText, Uri sourceUri)
        {
            JObject result = new JObject();
            var songsOnPage = new List<ScrapedSong>();
            try
            {
                result = JObject.Parse(pageText);

            }
            catch (JsonReaderException ex)
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
                        DownloadUri = Util.GetUriFromString(downloadUrl),
                        SourceUri = sourceUri,
                        SongName = songName,
                        MapperName = mapperName,
                        RawData = StoreRawData ? bSong.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                    });
                }
            }
            return songsOnPage;
        }

#pragma warning disable CA1054 // Uri parameters should not be strings
        /// <summary>
        /// Gets the page URI for a given UrlBase and page number.
        /// </summary>
        /// <param name="feedUrlBase"></param>
        /// <param name="page"></param>
        /// <exception cref="ArgumentNullException">Thrown when feedUrlBase is null or empty.</exception>
        /// <returns></returns>
        public Uri GetPageUri(string feedUrlBase, int page)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            if (string.IsNullOrEmpty(feedUrlBase))
                throw new ArgumentNullException(nameof(feedUrlBase), "feedUrlBase cannot be null or empty for GetPageUrl");
            string feedUrl = feedUrlBase.Replace(USERNAMEKEY, _username).Replace(PAGENUMKEY, page.ToString());
            //Logger.Debug($"Replacing {USERNAMEKEY} with {_username} in base URL:\n   {feedUrlBase}");
            return Util.GetUriFromString(feedUrl);
        }

        #region Web Requests
        #region Async
        // TODO: Abort early when bsaber.com is down (check if all items in block failed?)
        // TODO: Make cancellationToken actually do something.
        /// <summary>
        /// Gets all songs from the feed defined by the provided settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeastSaberFeedSettings.</exception>
        /// <exception cref="ArgumentException">Thrown when trying to access a feed that requires a username and the username wasn't provided.</exception>
        /// <returns></returns>
        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings, CancellationToken cancellationToken)
        {
            if (cancellationToken != CancellationToken.None)
                Logger.Warning("CancellationToken in GetSongsFromFeedAsync isn't implemented.");
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for BeastSaberReader.GetSongsFromFeedAsync.");
            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            if (!(settings is BeastSaberFeedSettings _settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (_settings.FeedIndex != 2 && string.IsNullOrEmpty(_username?.Trim()))
            {
                Logger.Error($"Can't access feed without a valid username in the config file");
                throw new ArgumentException("Cannot access this feed without a valid username.");
            }
            int pageIndex = settings.StartingPage;
            int maxPages = _settings.MaxPages;
            bool useMaxSongs = _settings.MaxSongs != 0;
            bool useMaxPages = maxPages != 0;
            if (useMaxPages && pageIndex > 1)
                maxPages = maxPages + pageIndex - 1;
            var ProcessPageBlock = new TransformBlock<Uri, List<ScrapedSong>>(async feedUrl =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //Logger.Debug($"Checking URL: {feedUrl}");
                string pageText = "";

                ContentType contentType;
                string contentTypeStr = string.Empty;
                try
                {
                    using (var response = await WebUtils.WebClient.GetAsync(feedUrl).ConfigureAwait(false))
                    {
                        contentTypeStr = response.Content.ContentType.ToLower();
                        if (ContentDictionary.ContainsKey(contentTypeStr))
                            contentType = ContentDictionary[contentTypeStr];
                        else
                            contentType = ContentType.Unknown;
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
                catch (HttpRequestException ex)
                {
                    Logger.Exception($"Error downloading {feedUrl} in TransformBlock.", ex);
                    return new List<ScrapedSong>();
                }

                var newSongs = GetSongsFromPageText(pageText, feedUrl, contentType);
                sw.Stop();
                //Logger.Debug($"Task for {feedUrl} completed in {sw.ElapsedMilliseconds}ms");
                return newSongs.Count > 0 ? newSongs : null;
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = MaxConcurrency,
                BoundedCapacity = MaxConcurrency,
                EnsureOrdered = true
            });
            bool continueLooping = true;
            int itemsInBlock = 0;
            do
            {
                while (continueLooping)
                {

                    var feedUrl = GetPageUri(Feeds[_settings.Feed].BaseUrl, pageIndex);
                    await ProcessPageBlock.SendAsync(feedUrl).ConfigureAwait(false); // TODO: Need check with SongsPerPage
                    itemsInBlock++;
                    pageIndex++;

                    if (pageIndex > maxPages && useMaxPages)
                        continueLooping = false;

                    while (ProcessPageBlock.OutputCount > 0 || itemsInBlock == MaxConcurrency || !continueLooping)
                    {
                        if (itemsInBlock <= 0)
                            break;
                        await ProcessPageBlock.OutputAvailableAsync().ConfigureAwait(false);
                        while (ProcessPageBlock.TryReceive(out List<ScrapedSong> newSongs))
                        {
                            itemsInBlock--;
                            if (newSongs == null)
                            {
                                Logger.Debug("Received no new songs, last page reached.");
                                ProcessPageBlock.Complete();
                                itemsInBlock = 0;
                                continueLooping = false;
                                break;
                            }
                            Logger.Debug($"Receiving {newSongs.Count} potential songs from {newSongs.First().SourceUri}");
                            foreach (var song in newSongs)
                            {
                                if (retDict.ContainsKey(song.Hash))
                                {
                                    Logger.Debug($"Song {song.Hash} already exists.");
                                }
                                else
                                {
                                    if (retDict.Count < settings.MaxSongs || settings.MaxSongs == 0)
                                        retDict.Add(song.Hash, song);
                                    if (retDict.Count >= settings.MaxSongs && useMaxSongs)
                                        continueLooping = false;
                                }
                            }
                            if (!useMaxPages || pageIndex <= maxPages)
                                if (retDict.Count < settings.MaxSongs)
                                    continueLooping = true;
                        }
                    }
                }
            }
            while (continueLooping);

            return retDict;
        }

        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None).ConfigureAwait(false);
        }

        #endregion

        #region Sync
        public Dictionary<string, ScrapedSong> GetSongsFromFeed(IFeedSettings settings)
        {
            // Pointless to have these checks here?
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
        #endregion
        #endregion

        #region Overloads
        public List<ScrapedSong> ParseJsonPage(string pageText, string sourceUrl)
        {
            return ParseJsonPage(pageText, Util.GetUriFromString(sourceUrl));
        }
        public List<ScrapedSong> ParseXMLPage(string pageText, string sourceUrl)
        {
            return ParseXMLPage(pageText, Util.GetUriFromString(sourceUrl));
        }
        public Uri GetPageUrl(int feedIndex, int page)
        {
            return GetPageUri(Feeds[(BeastSaberFeed)feedIndex].BaseUrl, page);
        }
        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText</exception>
        /// <returns></returns>
        public List<ScrapedSong> GetSongsFromPageText(string pageText, string sourceUrl, ContentType contentType)
        {
            return GetSongsFromPageText(pageText, Util.GetUriFromString(sourceUrl), contentType);
        }


        #endregion


        public enum ContentType
        {
            Unknown = 0,
            XML = 1,
            JSON = 2
        }
    }

    public class BeastSaberFeedSettings : IFeedSettings
    {
        /// <summary>
        /// Name of the chosen feed.
        /// </summary>
        public string FeedName { get { return BeastSaberReader.Feeds[Feed].Name; } }
        public int FeedIndex { get; set; }
        public BeastSaberFeed Feed { get { return (BeastSaberFeed)FeedIndex; } set { FeedIndex = (int)value; } }

        /// <summary>
        /// Maximum songs to retrieve, will stop the reader before MaxPages is met. Use 0 for unlimited.
        /// </summary>
        public int MaxSongs { get; set; }

        /// <summary>
        /// Maximum pages to check, will stop the reader before MaxSongs is met. Use 0 for unlimited.
        /// </summary>
        public int MaxPages { get; set; }

        /// <summary>
        /// Page of the feed to start on, default is 1. For all feeds, setting '1' here is the same as starting on the first page.
        /// </summary>
        public int StartingPage { get; set; }

        public BeastSaberFeedSettings(int feedIndex, int maxPages = 0)
        {
            FeedIndex = feedIndex;
            MaxPages = maxPages;
            StartingPage = 1;
        }
    }

    public enum BeastSaberFeed
    {
        Following = 0,
        Bookmarks = 1,
        CuratorRecommended = 2
    }
}
