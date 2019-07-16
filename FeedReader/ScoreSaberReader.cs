using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FeedReader.Logging;
using static FeedReader.WebUtils;

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
        public static readonly string NameKey = "ScoreSaberReader";
        public static readonly string SourceKey = "ScoreSaber";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        //private static readonly string CATKEY = "{CAT}";
        private static readonly string RANKEDKEY = "{RANKKEY}";
        private static readonly string LIMITKEY = "{LIMIT}";
        private const string INVALID_FEED_SETTINGS_MESSAGE = "The IFeedSettings passed is not a ScoreSaberFeedSettings.";
        private const string TOP_RANKED_KEY = "Top Ranked";
        private const string TRENDING_KEY = "Trending";
        private const string TOP_PLAYED_KEY = "Top Played";
        private const string LATEST_RANKED_KEY = "Latest Ranked";
        #endregion

        public static FeedReaderLoggerBase Logger = new FeedReaderLogger(LoggingController.DefaultLogger);
        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }

        private static Dictionary<ScoreSaberFeeds, FeedInfo> _feeds;
        public static Dictionary<ScoreSaberFeeds, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<ScoreSaberFeeds, FeedInfo>()
                    {
                        { (ScoreSaberFeeds)0, new FeedInfo(TRENDING_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=0&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeeds)1, new FeedInfo(LATEST_RANKED_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=1&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeeds)2, new FeedInfo(TOP_PLAYED_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=2&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeeds)3, new FeedInfo(TOP_RANKED_KEY, $"https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") }
                    };
                }
                return _feeds;
            }
        }
        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None);
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
                case ScoreSaberFeeds.TRENDING:
                    retDict = await GetSongsFromScoreSaberAsync(settings);
                    break;
                case ScoreSaberFeeds.LATEST_RANKED:
                    settings.RankedOnly = true;
                    retDict = await GetSongsFromScoreSaberAsync(settings);
                    break;
                case ScoreSaberFeeds.TOP_PLAYED:
                    retDict = await GetSongsFromScoreSaberAsync(settings);
                    break;
                case ScoreSaberFeeds.TOP_RANKED:
                    settings.RankedOnly = true;
                    retDict = await GetSongsFromScoreSaberAsync(settings);
                    break;
                default:
                    break;
            }
            return retDict;
        }

        public Dictionary<string, ScrapedSong> GetSongsFromFeed(IFeedSettings _settings)
        {
            return GetSongsFromFeedAsync(_settings).Result;
        }

        public void GetPageUrl(ref StringBuilder baseUrl, Dictionary<string, string> replacements)
        {
            foreach (var key in replacements.Keys)
            {
                baseUrl.Replace(key, replacements[key]);
            }
        }

        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromScoreSaberAsync(ScoreSaberFeedSettings settings)
        {
            // "https://scoresaber.com/api.php?function=get-leaderboards&cat={CATKEY}&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}"
            int songsPerPage = settings.SongsPerPage;
            int pageNum = 1;
            //int maxPages = (int)Math.Ceiling(settings.MaxSongs / ((float)songsPerPage));
            int maxPages = settings.MaxPages;
            if (settings.MaxPages > 0)
                maxPages = maxPages < settings.MaxPages ? maxPages : settings.MaxPages; // Take the lower limit.
            Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
            StringBuilder url = new StringBuilder(Feeds[settings.Feed].BaseUrl);
            Dictionary<string, string> urlReplacements = new Dictionary<string, string>() {
                {LIMITKEY, songsPerPage.ToString() },
                {PAGENUMKEY, pageNum.ToString()},
                {RANKEDKEY, settings.RankedOnly ? "1" : "0" }
            };
            GetPageUrl(ref url, urlReplacements);

            string pageText = "";
            using (var response = await GetPageAsync(url.ToString()))
            {
                if (response.IsSuccessStatusCode)
                    pageText = await response.Content.ReadAsStringAsync();
                else
                {
                    Logger.Error($"Error getting text from {url.ToString()}, HTTP Status Code is: {response.StatusCode.ToString()}: {response.ReasonPhrase}");
                }
            }

            JObject result;
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (Exception ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            foreach (var song in GetSongsFromPageText(pageText))
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
                foreach (var song in await GetSongsFromPageAsync(url.ToString()))
                {
                    diffCount++;
                    if (!songs.ContainsKey(song.Hash) && songs.Count < settings.MaxSongs)
                        songs.Add(song.Hash, song);
                }
                if (diffCount == 0)
                    continueLooping = false;
                //pageReadTasks.Add(GetSongsFromPageAsync(url.ToString()));
                if ((maxPages > 0 && pageNum >= maxPages) || songs.Count >= settings.MaxSongs)
                    continueLooping = false;
            } while (continueLooping);


            return songs;
        }


        public async Task<List<ScrapedSong>> GetSongsFromPageAsync(string url)
        {
            var response = await GetPageAsync(url).ConfigureAwait(false);
            List<ScrapedSong> songs;
            if (response.IsSuccessStatusCode)
            {
                var pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                songs = GetSongsFromPageText(pageText);
            }
            else
            {
                Logger.Error($"Error getting page {url}, response was {response.StatusCode.ToString()}: {response.ReasonPhrase}");
                songs = new List<ScrapedSong>();
            }
            return songs;
        }

        public List<ScrapedSong> GetSongsFromPageText(string pageText)
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
                        DownloadUrl = BEATSAVER_DOWNLOAD_URL_BASE + hash,
                        SongName = songName,
                        MapperName = mapperName,
                        RawData = StoreRawData ? song.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                    });
                ;
            }
            return songs;
        }

        public void PrepareReader()
        {

        }

    }

    public class ScoreSaberFeedSettings : IFeedSettings
    {
        public int SongsPerPage = 100;
        public string FeedName { get { return ScoreSaberReader.Feeds[Feed].Name; } }
        public ScoreSaberFeeds Feed { get { return (ScoreSaberFeeds)FeedIndex; } set { FeedIndex = (int)value; } }
        public int FeedIndex { get; set; }
        public bool UseSongKeyAsOutputFolder { get; set; }
        public bool searchOnline { get; set; }
        public bool RankedOnly { get; set; }
        public int MaxPages;
        public int MaxSongs { get; set; }
        public ScoreSaberFeedSettings(int feedIndex)
        {
            searchOnline = false;
            FeedIndex = feedIndex;
            UseSongKeyAsOutputFolder = true;
        }
    }

    public enum ScoreSaberFeeds
    {
        TRENDING = 0,
        LATEST_RANKED = 1,
        TOP_PLAYED = 2,
        TOP_RANKED = 3,
    }
}
