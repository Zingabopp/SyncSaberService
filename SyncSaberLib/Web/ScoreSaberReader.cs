using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SyncSaberLib.Data;
using static SyncSaberLib.Web.WebUtils;

namespace SyncSaberLib.Web
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

        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }

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

        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings _settings)
        {
            PrepareReader();
            if (!(_settings is ScoreSaberFeedSettings settings))
                throw new InvalidCastException(INVALID_FEED_SETTINGS_MESSAGE);
            List<SongInfo> songs = new List<SongInfo>();
            int maxSongs = settings.MaxSongs > 0 ? settings.MaxSongs : settings.SongsPerPage * settings.SongsPerPage;
            switch ((ScoreSaberFeeds)settings.FeedIndex)
            {
                case ScoreSaberFeeds.TRENDING:

                    if (!settings.searchOnline)
                    {
                        if (maxSongs <= 0)
                            throw new ArgumentException("You must specify a value greater than 0 for MaxPages or MaxSongs.");
                        songs.AddRange(ScrapedDataProvider.Songs.Values.Where(s => s.ScoreSaberInfo.Count > 0).OrderByDescending(s =>
                        s.ScoreSaberInfo.Values.Select(ss => ss.scores_day).Aggregate((a, b) => a + b)).Take(maxSongs));
                    }
                    break;
                case ScoreSaberFeeds.LATEST_RANKED:
                    if (!settings.searchOnline)
                    {
                        if (maxSongs <= 0)
                            throw new ArgumentException("You must specify a value greater than 0 for MaxPages or MaxSongs.");
                        songs.AddRange(ScrapedDataProvider.Songs.Values.Where(s => s.RankedDifficulties.Count > 0).OrderByDescending(ss =>
                        ss.ScoreSaberInfo.Keys.Max()).Take(maxSongs));
                    }
                    break;
                case ScoreSaberFeeds.TOP_PLAYED:
                    if (!settings.searchOnline)
                    {
                        if (maxSongs <= 0)
                            throw new ArgumentException("You must specify a value greater than 0 for MaxPages or MaxSongs.");
                        songs.AddRange(ScrapedDataProvider.Songs.Values.Where(s => s.ScoreSaberInfo.Count > 0).OrderByDescending(s =>
                        s.ScoreSaberInfo.Values.Select(ss => ss.scores).Aggregate((a, b) => a + b)).Take(maxSongs));
                    }
                    break;
                case ScoreSaberFeeds.TOP_RANKED:
                    if (!settings.searchOnline)
                    {
                        if (maxSongs <= 0)
                            throw new ArgumentException("You must specify a value greater than 0 for MaxPages or MaxSongs.");
                        songs.AddRange(ScrapedDataProvider.Songs.Values.Where(s => s.RankedDifficulties.Count > 0).OrderByDescending(s => s.RankedDifficulties.Max(d => d.Value)).Take(maxSongs));
                    }
                    else
                        songs.AddRange(GetTopPPSongs(settings));
                    break;
                default:
                    break;
            }

            Dictionary<int, SongInfo> retDict = new Dictionary<int, SongInfo>();
            foreach (var song in songs)
            {
                if (!retDict.ContainsKey(song.keyAsInt))
                {
                    retDict.Add(song.keyAsInt, song);
                }
            }
            return retDict;
        }

        public void GetPageUrl(ref StringBuilder baseUrl, Dictionary<string, string> replacements)
        {
            foreach (var key in replacements.Keys)
            {
                baseUrl.Replace(key, replacements[key]);
            }
        }

        public List<SongInfo> GetTopPPSongs(ScoreSaberFeedSettings settings)
        {
            // "https://scoresaber.com/api.php?function=get-leaderboards&cat={CATKEY}&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}"
            int songsPerPage = settings.SongsPerPage;
            int pageNum = 1;
            int maxPages = (int)Math.Ceiling(settings.MaxSongs / ((float)songsPerPage));
            if (settings.MaxPages > 0)
                maxPages = maxPages < settings.MaxPages ? maxPages : settings.MaxPages; // Take the lower limit.
            List<SongInfo> songs = new List<SongInfo>();

            StringBuilder url = new StringBuilder(Feeds[settings.Feed].BaseUrl);
            Dictionary<string, string> urlReplacements = new Dictionary<string, string>() {
                {LIMITKEY, songsPerPage.ToString() },
                {PAGENUMKEY, pageNum.ToString()},
                {RANKEDKEY, "1" }
            };
            GetPageUrl(ref url, urlReplacements);

            string pageText = "";
            using (var response = GetPage(url.ToString()))
            {
                if (response.IsSuccessStatusCode)
                    pageText = response.Content.ReadAsStringAsync().Result;
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
            songs.AddRange(GetSongsFromPage(pageText));
            List<Task<List<SongInfo>>> pageReadTasks = new List<Task<List<SongInfo>>>();
            bool continueLooping = true;
            do
            {
                pageNum++;
                if (pageNum > maxPages)
                    break;
                url.Clear();
                url.Append(Feeds[settings.Feed].BaseUrl);
                urlReplacements.AddOrUpdate(PAGENUMKEY, pageNum.ToString());
                GetPageUrl(ref url, urlReplacements);
                Logger.Trace($"Adding pageReadTask {url.ToString()}");
                pageReadTasks.Add(GetSongsFromPageAsync(url.ToString()));
                if (pageNum >= maxPages)
                    continueLooping = false;
            } while (continueLooping);

            Task.WaitAll(pageReadTasks.ToArray());
            foreach (var job in pageReadTasks)
            {
                songs.AddRange(job.Result);
            }
            return songs;
        }


        public async Task<List<SongInfo>> GetSongsFromPageAsync(string url)
        {
            var response = await GetPageAsync(url).ConfigureAwait(false);
            List<SongInfo> songs;
            if (response.IsSuccessStatusCode)
            {
                var pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                songs = GetSongsFromPage(pageText);
            }
            else
            {
                Logger.Error($"Error getting page {url}, response was {response.StatusCode.ToString()}: {response.ReasonPhrase}");
                songs = new List<SongInfo>();
            }
            return songs;
        }


        public static List<SongInfo> GetSongsFromPage(string pageText)
        {
            var sssongs = GetSSSongsFromPage(pageText);

            List<SongInfo> songs = new List<SongInfo>();
            SongInfo tempSong;
            //sssongs.AsParallel().WithDegreeOfParallelism(Config.MaxConcurrentPageChecks).ForAll(s => s.PopulateFields());
            //Parallel.ForEach(sssongs, new ParallelOptions { MaxDegreeOfParallelism = Config.MaxConcurrentPageChecks }, s => s.PopulateFields());
            foreach (var song in sssongs)
            {
                tempSong = ScrapedDataProvider.GetSong(song, false); // Don't search online because creating the ScoreSaberSong already tried
                if (tempSong != null && !string.IsNullOrEmpty(tempSong.key))
                {
                    //tempSong.ScoreSaberInfo.AddOrUpdate(song.uid, song);
                    songs.Add(tempSong);
                }
                else
                {
                    Logger.Warning($"Could not find song {song.name} with hash {song.hash} on Beat Saver, skipping...");
                }
            }

            return songs;
        }

        public static List<ScoreSaberSong> GetSSSongsFromPage(string pageText)
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
            List<ScoreSaberSong> songs = new List<ScoreSaberSong>();

            var songJSONAry = result["songs"]?.ToArray();
            if (songJSONAry == null)
            {
                Logger.Error("Invalid page text: 'songs' field not found.");
            }
            ScoreSaberSong newSong;
            foreach (var song in songJSONAry)
            {
                newSong = GetScoreSaberSongFromJson(song);
                if (newSong != null)
                    songs.Add(newSong);
            }
            return songs;
        }

        public Playlist[] PlaylistsForFeed(int feedIndex)
        {
            switch ((ScoreSaberFeeds)feedIndex)
            {
                case ScoreSaberFeeds.TOP_RANKED:
                    return new Playlist[] { _topRanked };
                case ScoreSaberFeeds.TRENDING:
                    return new Playlist[] { _trending };
                case ScoreSaberFeeds.TOP_PLAYED:
                    return new Playlist[] { _topPlayed };
                case ScoreSaberFeeds.LATEST_RANKED:
                    return new Playlist[] { _latestRanked };
                default:
                    break;
            }
            return new Playlist[] { };
        }

        public void PrepareReader()
        {

        }

        public static List<ScoreSaberSong> ScrapeScoreSaber(int requestDelay, int songsPerPage, bool rankedOnly, int maxPages = 0)
        {
            bool useMaxPages = maxPages != 0;
            List<ScoreSaberSong> songs = new List<ScoreSaberSong>();

            string rankVal = rankedOnly ? "1" : "0";
            int songCount = 0;
            int pageNum = 1;

            StringBuilder url = new StringBuilder();

            bool continueLooping = true;
            do
            {
                url.Clear();
                if (rankedOnly)
                    url.Append(Feeds[ScoreSaberFeeds.LATEST_RANKED].BaseUrl);
                else
                    url.Append(Feeds[ScoreSaberFeeds.TOP_PLAYED].BaseUrl);
                url.Replace(RANKEDKEY, rankVal);
                url.Replace(LIMITKEY, songsPerPage.ToString());
                url.Replace(PAGENUMKEY, pageNum.ToString());
                Thread.Sleep(requestDelay);

                Logger.Info($"On page {pageNum}");

                //Logger.Debug($"Creating task for {url}");
                string pageText = GetPageText(url.ToString());
                songs.AddRange(GetSSSongsFromPage(pageText));
                if (songs.Count == songCount)
                    continueLooping = false;
                songCount = songs.Count;
                if (pageText.ToLower().Contains("rate limit"))
                {
                    Logger.Warning("Rate limit exceeded?");
                }
                pageNum++;
                if (useMaxPages && (pageNum > maxPages))
                    continueLooping = false;
            } while (continueLooping);
            //Logger.Info($"Scraped {songs.Count} new songs");
            return songs;
        }

        public static ScoreSaberSong GetScoreSaberSongFromJson(JToken song)
        {
            //JSONObject song = (JSONObject) aKeyValue;
            //string songIndex = song["key"]?.Value<string>();
            string songHash = song["id"]?.Value<string>();
            string songName = song["name"]?.Value<string>();
            string author = song["author"]?.Value<string>();
            //string songUrl = "https://beatsaver.com/download/" + songIndex;
            ScoreSaberSong newSong = null;
            if (ScoreSaberSong.TryParseScoreSaberSong(song, ref newSong))
            {
                //newSong.Feed = "followings";
                newSong.ScrapedAt = DateTime.Now;
                ScrapedDataProvider.GetOrCreateSong(newSong, true);
                return newSong;
            }
            else
            {
                if (!(string.IsNullOrEmpty(songName)))
                {
                    Logger.Warning($"Couldn't parse song {songName}, using sparse definition.");
                    //songs.Add(new ScoreSaberSong("", songName, "", author));
                }
                else
                    Logger.Error("Unable to identify song, skipping");
            }
            return null;
        }

        private readonly Playlist _topRanked = new Playlist("ScoreSaberTopRanked", "ScoreSaber Top Ranked", "SyncSaber", "1");
        private readonly Playlist _trending = new Playlist("ScoreSaberTrending", "ScoreSaber Trending", "SyncSaber", "1");
        private readonly Playlist _topPlayed = new Playlist("ScoreSaberTopPlayed", "ScoreSaber Top Played", "SyncSaber", "1");
        private readonly Playlist _latestRanked = new Playlist("ScoreSaberLatestRanked", "ScoreSaber Latest Ranked", "SyncSaber", "1");
    }

    public class ScoreSaberFeedSettings : IFeedSettings
    {
        public int SongsPerPage = 100;
        public string FeedName { get { return ScoreSaberReader.Feeds[Feed].Name; } }
        public ScoreSaberFeeds Feed { get { return (ScoreSaberFeeds)FeedIndex; } set { FeedIndex = (int)value; } }
        public int FeedIndex { get; set; }
        public bool UseSongKeyAsOutputFolder { get; set; }
        public bool searchOnline { get; set; }
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
