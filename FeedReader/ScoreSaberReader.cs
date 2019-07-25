using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FeedReader.Logging;
using static FeedReader.WebUtils;
using Newtonsoft.Json;

namespace FeedReader
{
    public class ScoreSaberReader : IFeedReader
    {
        /// API Examples:
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=50&page=1&ranked=1 // Sorted by PP
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=10&page=1&search=honesty&ranked=1
        /// cat options:
        /// 0 = trending
        /// 1 = date ranked
        /// 2 = scores set
        /// 3 = star rating
        /// 4 = author
        #region Constants
        private const string BEATSAVER_DOWNLOAD_URL_BASE = "http://beatsaver.com/api/download/hash/";
        public static string NameKey => "ScoreSaberReader";
        public static string SourceKey => "ScoreSaber";
        private const string PAGENUMKEY = "{PAGENUM}";
        //private static readonly string CATKEY = "{CAT}";
        private const string RANKEDKEY = "{RANKKEY}";
        private const string LIMITKEY = "{LIMIT}";
        private const string QUERYKEY = "{QUERY}";
        private const string INVALID_FEED_SETTINGS_MESSAGE = "The IFeedSettings passed is not a ScoreSaberFeedSettings.";
        private const string TOP_RANKED_KEY = "Top Ranked";
        private const string TRENDING_KEY = "Trending";
        private const string TOP_PLAYED_KEY = "Top Played";
        private const string LATEST_RANKED_KEY = "Latest Ranked";
        private const string SEARCH_KEY = "Search";
        #endregion
        private static FeedReaderLoggerBase _logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static FeedReaderLoggerBase Logger { get { return _logger; } set { _logger = value; } }
        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public Uri RootUri { get { return new Uri("https://scoresaber.com/"); } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }

        public void PrepareReader()
        {
            if(!Ready)
            {
                Ready = true;
            }
        }

        private static Dictionary<ScoreSaberFeed, FeedInfo> _feeds;
        public static Dictionary<ScoreSaberFeed, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<ScoreSaberFeed, FeedInfo>()
                    {
                        { (ScoreSaberFeed)0, new FeedInfo(TRENDING_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=0&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)1, new FeedInfo(LATEST_RANKED_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=1&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)2, new FeedInfo(TOP_PLAYED_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=2&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)3, new FeedInfo(TOP_RANKED_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)99, new FeedInfo(SEARCH_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}&search={QUERYKEY}") }
                    };
                }
                return _feeds;
            }
        }
        
        public static void GetPageUrl(ref StringBuilder baseUrl, Dictionary<string, string> replacements)
        {
            if (baseUrl == null)
                throw new ArgumentNullException(nameof(replacements), "baseUrl cannot be null for ScoreSaberReader.GetPageUrl");
            if (replacements == null)
                throw new ArgumentNullException(nameof(replacements), "replacements cannot be null for ScoreSaberReader.GetPageUrl");
            foreach (var key in replacements.Keys)
            {
                baseUrl.Replace(key, replacements[key]);
            }
        }

        public List<ScrapedSong> GetSongsFromPageText(string pageText, Uri sourceUri)
        {
            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);

            }
            catch (JsonReaderException ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            List<ScrapedSong> songs = new List<ScrapedSong>();

            var songJSONAry = result["songs"]?.ToArray();
            if (songJSONAry == null)
            {
                Logger.Error("Invalid page text: 'songs' field not found.");
            }
            foreach (var song in songJSONAry)
            {
                var hash = song["id"]?.Value<string>();
                var songName = song["name"]?.Value<string>();
                var mapperName = song["levelAuthorName"]?.Value<string>();

                if (!string.IsNullOrEmpty(hash))
                    songs.Add(new ScrapedSong(hash)
                    {
                        DownloadUri = Util.GetUriFromString(BEATSAVER_DOWNLOAD_URL_BASE + hash),
                        SourceUri = sourceUri,
                        SongName = songName,
                        MapperName = mapperName,
                        RawData = StoreRawData ? song.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                    });
                ;
            }
            return songs;
        }

        #region Web Requests

        #region Async
        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromScoreSaberAsync(ScoreSaberFeedSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for ScoreSaberReader.GetSongsFromScoreSaberAsync");
            // "https://scoresaber.com/api.php?function=get-leaderboards&cat={CATKEY}&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}"
            int songsPerPage = settings.SongsPerPage;
            int pageNum = settings.StartingPage;
            //int maxPages = (int)Math.Ceiling(settings.MaxSongs / ((float)songsPerPage));
            int maxPages = settings.MaxPages;
            if (pageNum > 1 && maxPages != 0)
                maxPages = maxPages + pageNum - 1;
            //if (settings.MaxPages > 0)
            //    maxPages = maxPages < settings.MaxPages ? maxPages : settings.MaxPages; // Take the lower limit.
            Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
            StringBuilder url = new StringBuilder(Feeds[settings.Feed].BaseUrl);
            Dictionary<string, string> urlReplacements = new Dictionary<string, string>() {
                {LIMITKEY, songsPerPage.ToString() },
                {PAGENUMKEY, pageNum.ToString()},
                {RANKEDKEY, settings.RankedOnly ? "1" : "0" }
            };
            GetPageUrl(ref url, urlReplacements);
            var uri = new Uri(url.ToString());
            string pageText = "";
            using (var response = await WebUtils.WebClient.GetAsync(uri).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                else
                {
                    Logger.Error($"Error getting text from {uri}, HTTP Status Code is: {response.StatusCode.ToString()}: {response.ReasonPhrase}");
                }
            }

            JObject result;
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (JsonReaderException ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            foreach (var song in GetSongsFromPageText(pageText, uri))
            {
                if (!songs.ContainsKey(song.Hash) && songs.Count < settings.MaxSongs)
                    songs.Add(song.Hash, song);
            }
            bool continueLooping = true;
            do
            {
                pageNum++;
                int diffCount = 0;
                if ((maxPages > 0 && pageNum > maxPages) || songs.Count >= settings.MaxSongs)
                    break;
                url.Clear();
                url.Append(Feeds[settings.Feed].BaseUrl);
                if (!urlReplacements.ContainsKey(PAGENUMKEY))
                    urlReplacements.Add(PAGENUMKEY, pageNum.ToString());
                else
                    urlReplacements[PAGENUMKEY] = pageNum.ToString();
                GetPageUrl(ref url, urlReplacements);
                uri = new Uri(url.ToString());
                foreach (var song in await GetSongsFromPageAsync(uri).ConfigureAwait(false))
                {
                    diffCount++;
                    if (!songs.ContainsKey(song.Hash) && songs.Count < settings.MaxSongs)
                        songs.Add(song.Hash, song);
                }
                if (diffCount == 0)
                    continueLooping = false;
                //pageReadTasks.Add(GetSongsFromPageAsync(url.ToString()));
                if ((maxPages > 0 && pageNum >= maxPages) || (songs.Count >= settings.MaxSongs && settings.MaxSongs > 0))
                    continueLooping = false;
            } while (continueLooping);


            return songs;
        }

        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings _settings, CancellationToken cancellationToken)
        {
            PrepareReader();
            if (!(_settings is ScoreSaberFeedSettings settings))
                throw new InvalidCastException(INVALID_FEED_SETTINGS_MESSAGE);
            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            int maxSongs = settings.MaxSongs > 0 ? settings.MaxSongs : settings.SongsPerPage * settings.SongsPerPage;
            switch (settings.Feed)
            {
                case ScoreSaberFeed.Trending:
                    retDict = await GetSongsFromScoreSaberAsync(settings).ConfigureAwait(false);
                    break;
                case ScoreSaberFeed.LatestRanked:
                    settings.RankedOnly = true;
                    retDict = await GetSongsFromScoreSaberAsync(settings).ConfigureAwait(false);
                    break;
                case ScoreSaberFeed.TopPlayed:
                    retDict = await GetSongsFromScoreSaberAsync(settings).ConfigureAwait(false);
                    break;
                case ScoreSaberFeed.TopRanked:
                    settings.RankedOnly = true;
                    retDict = await GetSongsFromScoreSaberAsync(settings).ConfigureAwait(false);
                    break;
                default:
                    break;
            }
            return retDict;
        }
        public async Task<List<ScrapedSong>> GetSongsFromPageAsync(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri), "uri cannot be null in ScoreSaberReader.GetSongsFromPageAsync");
            List<ScrapedSong> songs = null;
            using (var response = await WebUtils.WebClient.GetAsync(uri).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    var pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    songs = GetSongsFromPageText(pageText, uri);
                }
                else
                {
                    Logger.Error($"Error getting page {uri?.ToString()}, response was {response.StatusCode.ToString()}: {response.ReasonPhrase}");
                }
            }
            return songs ?? new List<ScrapedSong>();
        }

        #endregion

        #region Sync
        public Dictionary<string, ScrapedSong> GetSongsFromFeed(IFeedSettings _settings)
        {
            return GetSongsFromFeedAsync(_settings).Result;
        }

        #endregion

        #endregion

        #region Overloads
        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None).ConfigureAwait(false);
        }

        public Task<List<ScrapedSong>> GetSongsFromPageAsync(string url)
        {
            return GetSongsFromPageAsync(Util.GetUriFromString(url));
        }

        public List<ScrapedSong> GetSongsFromPageText(string pageText, string sourceUrl)
        {
            return GetSongsFromPageText(pageText, Util.GetUriFromString(sourceUrl));
        }

        #endregion

    }

    public class ScoreSaberFeedSettings : IFeedSettings
    {

        public string FeedName { get { return ScoreSaberReader.Feeds[Feed].Name; } }
        public ScoreSaberFeed Feed { get { return (ScoreSaberFeed)FeedIndex; } set { FeedIndex = (int)value; } }
        public int FeedIndex { get; set; }

        /// <summary>
        /// Only get ranked songs. Forced true for TOP_RANKED and LATEST_RANKED feeds.
        /// </summary>
        public bool RankedOnly { get; set; }

        /// <summary>
        /// Maximum songs to retrieve, will stop the reader before MaxPages is met. Use 0 for unlimited.
        /// </summary>
        public int MaxSongs { get; set; }

        /// <summary>
        /// Maximum pages to check, will stop the reader before MaxSongs is met. Use 0 for unlimited.
        /// </summary>
        public int MaxPages { get; set; }

        /// <summary>
        /// Number of songs shown on a page. 100 is default.
        /// </summary>
        public int SongsPerPage { get; set; }

        /// <summary>
        /// Page of the feed to start on, default is 1. For all feeds, setting '1' here is the same as starting on the first page.
        /// </summary>
        public int StartingPage { get; set; }

        public ScoreSaberFeedSettings(int feedIndex)
        {
            FeedIndex = feedIndex;
            SongsPerPage = 100;
            StartingPage = 1;
        }
    }

    public enum ScoreSaberFeed
    {
        Trending = 0,
        LatestRanked = 1,
        TopPlayed = 2,
        TopRanked = 3,
        Search = 99
    }
}
