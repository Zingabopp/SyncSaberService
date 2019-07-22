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
using static FeedReader.WebUtils;

namespace FeedReader
{
    public class BeatSaverReader : IFeedReader
    {
        public static FeedReaderLoggerBase Logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static string NameKey => "BeatSaverReader";
        public string Name { get { return NameKey; } }
        public static readonly string SourceKey = "BeatSaver";
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }
        #region Constants
        //private static readonly string AUTHORKEY = "{AUTHOR}";
        private static readonly string AUTHORIDKEY = "{AUTHORID}";
        private static readonly string PAGEKEY = "{PAGE}";
        private static readonly string SEARCHTYPEKEY = "{TYPE}";
        private static readonly string SEARCHKEY = "{SEARCH}";
        private const int SONGS_PER_PAGE = 10;
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";
        private const string BEATSAVER_DOWNLOAD_URL_BASE = "https://beatsaver.com/api/download/key/";
        private const string BEATSAVER_DETAILS_BASE_URL = "https://beatsaver.com/api/maps/detail/";
        private const string BEATSAVER_GETBYHASH_BASE_URL = "https://beatsaver.com/api/maps/by-hash/";
        private const string BEATSAVER_NIGHTLYDUMP_URL = "https://beatsaver.com/api/download/dumps/maps";
        #endregion

        private static ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        // { (BeatSaverFeeds)99, new FeedInfo("search-by-author", "https://beatsaver.com/api/songs/search/user/" + AUTHORKEY) }
        private static Dictionary<BeatSaverFeeds, FeedInfo> _feeds;
        public static Dictionary<BeatSaverFeeds, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeatSaverFeeds, FeedInfo>()
                    {
                        { (BeatSaverFeeds)0, new FeedInfo("author", "https://beatsaver.com/api/maps/uploader/" +  AUTHORIDKEY + "/" + PAGEKEY)},
                        { (BeatSaverFeeds)1, new FeedInfo("latest", "https://beatsaver.com/api/maps/latest/" + PAGEKEY) },
                        { (BeatSaverFeeds)2, new FeedInfo("hot", "https://beatsaver.com/api/maps/hot/" + PAGEKEY) },
                        { (BeatSaverFeeds)3, new FeedInfo("plays", "https://beatsaver.com/api/maps/plays/" + PAGEKEY) },
                        { (BeatSaverFeeds)4, new FeedInfo("downloads", "https://beatsaver.com/api/maps/downloads/" + PAGEKEY) },
                        { (BeatSaverFeeds)98, new FeedInfo("search", $"https://beatsaver.com/api/search/text/{PAGEKEY}?q={SEARCHKEY}") },
                    };
                }
                return _feeds;
            }
        }

        public void PrepareReader()
        {
            Ready = true;
        }

        public static string GetPageUrl(int feedIndex, int pageIndex = 0, Dictionary<string, string> replacements = null)
        {
            string mapperId = string.Empty;
            StringBuilder url = new StringBuilder(Feeds[(BeatSaverFeeds)feedIndex].BaseUrl);
            //if (!string.IsNullOrEmpty(author) && author.Length > 3)
            //    mapperId = GetAuthorID(author);
            if (replacements != null)
                foreach (var key in replacements.Keys)
                {
                    url.Replace(key, replacements[key]);
                }
            return url.Replace(PAGEKEY, pageIndex.ToString()).ToString();
        }

        /// <summary>
        /// Parses out a List of ScrapedSongs from the given page text. Also works if the page is for a single song.
        /// </summary>
        /// <param name="pageText"></param>
        /// <returns></returns>
        public static List<ScrapedSong> ParseSongsFromPage(string pageText, string sourceUrl)
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
            ScrapedSong newSong;
            int? resultTotal = result["totalDocs"]?.Value<int>();
            if (resultTotal == null) resultTotal = 0;

            // Single song in page text.
            if (resultTotal == 0)
            {
                if (result["key"] != null)
                {
                    newSong = ParseSongFromJson(result, sourceUrl);
                    if (newSong != null)
                    {
                        songs.Add(newSong);
                        return songs;
                    }
                }
                return songs;
            }

            // Array of songs in page text.
            var songJSONAry = result["docs"]?.ToArray();

            if (songJSONAry == null)
            {
                Logger.Error("Invalid page text: 'songs' field not found.");
                return songs;
            }

            foreach (JObject song in songJSONAry)
            {
                newSong = ParseSongFromJson(song, sourceUrl);
                if (newSong != null)
                    songs.Add(newSong);
            }
            return songs;
        }

        /// <summary>
        /// Creates a SongInfo from a JObject.
        /// </summary>
        /// <param name="song"></param>
        /// <exception cref="ArgumentException">Thrown when a hash can't be found for the given song JObject.</exception>
        /// <returns></returns>
        public static ScrapedSong ParseSongFromJson(JObject song, string sourceUrl)
        {
            //JSONObject song = (JSONObject) aKeyValue;
            string songKey = song["key"]?.Value<string>();
            string songHash = song["hash"]?.Value<string>().ToUpper();
            var songName = song["name"]?.Value<string>();
            var mapperName = song["uploader"]?["username"]?.Value<string>();
            if (string.IsNullOrEmpty(songHash))
                throw new ArgumentException("Unable to find hash for the provided song, is this a valid song JObject?");
            string songUrl = !string.IsNullOrEmpty(songKey) ? BEATSAVER_DOWNLOAD_URL_BASE + songKey : string.Empty;
            var newSong = new ScrapedSong(songHash)
            {
                DownloadUrl = songUrl,
                SourceUrl = sourceUrl,
                SongName = songName,
                MapperName = mapperName,
                RawData = song.ToString()
            };
            return newSong;
        }

        private static int CalcMaxSongs(int maxPages, int maxSongs)
        {
            int retVal = 0;
            if (maxPages > 0)
                retVal = maxPages * SONGS_PER_PAGE;
            if (maxSongs > 0)
            {
                if (retVal == 0)
                    retVal = maxSongs;
                else
                    retVal = Math.Min(retVal, maxSongs);
            }
            return retVal;
        }

        private static int CalcMaxPages(int maxPages, int maxSongs)
        {
            int retVal = 0;
            if (maxPages > 0)
                retVal = maxPages;
            if (maxSongs > 0)
            {
                int pagesForSongs = (int)Math.Ceiling(maxSongs / (float)SONGS_PER_PAGE);
                if (retVal == 0)
                    retVal = pagesForSongs;
                else
                    retVal = Math.Min(retVal, pagesForSongs);
            }
            return retVal;
        }

        #region Web Requests

        #region Async
        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings _settings, CancellationToken cancellationToken)
        {
            PrepareReader();
            if (!(_settings is BeatSaverFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            List<ScrapedSong> songs = null;

            switch ((BeatSaverFeeds)settings.FeedIndex)
            {
                // Author
                case BeatSaverFeeds.AUTHOR:
                    string songSource = string.Empty;
                    List<ScrapedSong> newSongs = null;
                    songs = new List<ScrapedSong>();
                    foreach (var author in settings.Authors)
                    {
                        newSongs = await GetSongsByAuthorAsync(author, CalcMaxPages(settings.MaxPages, settings.MaxSongs)).ConfigureAwait(false);
                        songSource = "Beat Saver";
                        songs.AddRange(newSongs.Take(settings.MaxSongs));

                        Logger.Info($"Found {newSongs.Count} songs uploaded by {author} from {songSource}");
                    }
                    break;
                case BeatSaverFeeds.SEARCH:
                    songs = await SearchAsync(settings.SearchCriteria, settings.SearchType);
                    break;
                // Latest/Hot/Plays/Downloads
                default:
                    songs = await GetBeatSaverSongsAsync(settings).ConfigureAwait(false);
                    break;
            }

            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            foreach (var song in songs)
            {
                if (!retDict.ContainsKey(song.Hash))
                {
                    retDict.Add(song.Hash, song);
                }
            }
            return retDict;
        }
        public async Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None).ConfigureAwait(false);
        }
        public async Task<List<ScrapedSong>> GetBeatSaverSongsAsync(BeatSaverFeedSettings settings)
        {
            // TODO: double checks the first page
            int feedIndex = settings.FeedIndex;
            bool useMaxPages = settings.MaxPages != 0;
            bool useMaxSongs = settings.MaxSongs != 0;
            List<ScrapedSong> songs = new List<ScrapedSong>();
            string pageText = string.Empty;
            using (var response = await WebUtils.WebClient.GetAsync(GetPageUrl(feedIndex)).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                else
                    return songs;
            }

            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (Exception ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            int? numSongs = result["totalDocs"]?.Value<int>();
            int? lastPage = result["lastPage"]?.Value<int>();
            if (numSongs == null || lastPage == null || numSongs == 0)
            {
                Logger.Warning($"Error checking Beat Saver's {settings.FeedName} feed.");
                return songs;
            }
            Logger.Info($"Checking Beat Saver's {settings.FeedName} feed, {numSongs} songs available");
            int maxPages = settings.MaxPages;
            int pageNum = Math.Max(settings.StartingPage - 1, 0);
            if (pageNum > 0 && useMaxPages)
                maxPages = maxPages + pageNum; // Add starting page to maxPages so we actually get songs if maxPages < starting page
            List<Task<List<ScrapedSong>>> pageReadTasks = new List<Task<List<ScrapedSong>>>();
            string url = "";
            bool continueLooping = true;
            do
            {
                url = GetPageUrl(feedIndex, pageNum);
                Logger.Debug($"Creating task for {url}");
                pageReadTasks.Add(GetSongsFromPageAsync(url));
                pageNum++;
                if ((pageNum > lastPage))
                    continueLooping = false;
                if (useMaxPages && (pageNum >= maxPages))
                    continueLooping = false;
                if (useMaxSongs && pageNum * SONGS_PER_PAGE >= settings.MaxSongs)
                    continueLooping = false;
            } while (continueLooping);
            try
            {
                await Task.WhenAll(pageReadTasks.ToArray()).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Logger.Error($"Error waiting for pageReadTasks");
            }
            foreach (var job in pageReadTasks)
            {
                songs.AddRange(await job.ConfigureAwait(false));
                foreach (var song in await job.ConfigureAwait(false))
                {
                    if (!useMaxSongs || songs.Count <= settings.MaxSongs)
                        songs.Add(song);
                }
            }
            return songs;
        }

        public static async Task<List<string>> GetAuthorNamesByIDAsync(string mapperId)
        {
            List<string> authorNames = new List<string>();
            List<ScrapedSong> songs = await GetSongsByUploaderIdAsync(mapperId).ConfigureAwait(false);
            authorNames = songs.Select(s => s.MapperName).Distinct().ToList();
            //authorNames.ForEach(n => Logger.Warning($"Found authorName: {n}"));
            return authorNames;
        }

        public static async Task<string> GetAuthorIDAsync(string authorName)
        {
            if (_authors.ContainsKey(authorName))
                return _authors[authorName];
            string mapperId = string.Empty;

            int page = 0;
            int? totalResults;
            string searchURL, pageText;
            JObject result;
            JToken matchingSong;
            JToken[] songJSONAry;
            do
            {
                Logger.Debug($"Checking page {page + 1} for the author ID.");
                searchURL = Feeds[BeatSaverFeeds.SEARCH].BaseUrl.Replace(SEARCHKEY, authorName).Replace(PAGEKEY, (page * SONGS_PER_PAGE).ToString());

                using (var response = await WebUtils.WebClient.GetAsync(searchURL).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        Logger.Error($"Error getting UploaderID from author name, {searchURL} responded with {response.StatusCode}:{response.ReasonPhrase}");
                        return string.Empty;
                    }
                }

                result = new JObject();
                try { result = JObject.Parse(pageText); }
                catch (Exception ex)
                {
                    Logger.Exception("Unable to parse JSON from text", ex);
                }
                totalResults = result["totalDocs"]?.Value<int>(); // TODO: Check this
                if (totalResults == null || totalResults == 0)
                {
                    Logger.Warning($"No songs by {authorName} found, is the name spelled correctly?");
                    return string.Empty;
                }
                songJSONAry = result["docs"].ToArray();
                matchingSong = (JObject)songJSONAry.FirstOrDefault(c => c["uploader"]?["username"]?.Value<string>()?.ToLower() == authorName.ToLower());

                page++;
                searchURL = Feeds[BeatSaverFeeds.SEARCH].BaseUrl.Replace(SEARCHKEY, authorName).Replace(PAGEKEY, (page * SONGS_PER_PAGE).ToString());
            } while ((matchingSong == null) && page * SONGS_PER_PAGE < totalResults);


            if (matchingSong == null)
            {
                Logger.Warning($"No songs by {authorName} found, is the name spelled correctly?");
                return string.Empty;
            }
            mapperId = matchingSong["uploader"]["_id"].Value<string>();
            _authors.TryAdd(authorName, mapperId);

            return mapperId;
        }
        public static async Task<List<ScrapedSong>> GetSongsFromPageAsync(string url)
        {
            string pageText = string.Empty;
            var songs = new List<ScrapedSong>();
            using (var response = await WebUtils.WebClient.GetAsync(url).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                else
                {
                    Logger.Error($"Error getting songs from page, {url} responded with {response.StatusCode}:{response.ReasonPhrase}");
                    return songs;
                }
            }

            foreach (var song in ParseSongsFromPage(pageText, url))
            {
                songs.Add(song);
            }
            return songs;
        }
        /// <summary>
        /// Gets a list of songs by an author with the provided ID (NOT the author's username).
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>
        public static async Task<List<ScrapedSong>> GetSongsByUploaderIdAsync(string authorId, int maxPages = 0)
        {
            int feedIndex = 0;
            List<ScrapedSong> songs = new List<ScrapedSong>();
            string pageText = string.Empty;
            string url = GetPageUrl(feedIndex, 0, new Dictionary<string, string>() { { AUTHORIDKEY, authorId } });
            try
            {
                using (var response = await WebUtils.WebClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        Logger.Error($"Error getting songs by UploaderId, {url} responded with {response.StatusCode}:{response.ReasonPhrase}");
                        return songs;
                    }
                }
            }
            catch (HttpGetException ex)
            {
                Logger.Error($"Error getting songs by UploaderId, {ex.Url} responded with {ex.HttpStatusCode.ToString()}");
                return songs;
            }
            catch (Exception ex)
            {
                Logger.Exception($"Error getting songs by UploaderId, {authorId}, from {url}", ex);
                return songs;
            }

            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (Exception ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
                return songs;
            }

            int numSongs = result["totalDocs"]?.Value<int>() ?? 0; // Check this
            int lastPage = result["lastPage"]?.Value<int>() ?? 0;
            if (maxPages > 0)
                lastPage = Math.Min(lastPage, maxPages);
            Logger.Debug($"{numSongs} songs by {authorId} available on Beat Saver");
            int pageNum = 0;
            List<Task<List<ScrapedSong>>> pageReadTasks = new List<Task<List<ScrapedSong>>>();
            do
            {
                url = GetPageUrl(feedIndex, pageNum, new Dictionary<string, string>() { { AUTHORIDKEY, authorId } });
                Logger.Debug($"Creating task for {url}");
                pageReadTasks.Add(GetSongsFromPageAsync(url));
                pageNum++;
            } while (pageNum <= lastPage);

            await Task.WhenAll(pageReadTasks.ToArray()).ConfigureAwait(false);
            foreach (var job in pageReadTasks)
            {
                songs.AddRange(await job);
            }
            return songs;
        }
        public static async Task<List<ScrapedSong>> GetSongsByAuthorAsync(string uploader, int maxPages = 0)
        {
            string mapperId = await GetAuthorIDAsync(uploader).ConfigureAwait(false);
            if (string.IsNullOrEmpty(mapperId))
                return new List<ScrapedSong>();
            return await GetSongsByUploaderIdAsync(mapperId, maxPages).ConfigureAwait(false);
        }
        public static async Task<ScrapedSong> GetSongByHashAsync(string hash)
        {
            string url = BEATSAVER_GETBYHASH_BASE_URL + hash;
            string pageText = "";
            ScrapedSong song = null;
            try
            {
                using (var response = await WebUtils.WebClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        Logger.Error($"Error getting song by hash, {url} responded with {response.StatusCode}:{response.ReasonPhrase}");
                        return song;
                    }
                }
                if (string.IsNullOrEmpty(pageText))
                {
                    Logger.Warning($"Unable to get web page at {url}");
                    return null;
                }
            }
            catch (HttpRequestException)
            {
                Logger.Error($"HttpRequestException while trying to populate fields for {hash}");
                return null;
            }
            catch (AggregateException ae)
            {
                ae.WriteExceptions($"Exception while trying to get details for {hash}");
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception getting page", ex);
            }
            song = ParseSongsFromPage(pageText, url).FirstOrDefault();
            return song;
        }
        public static async Task<ScrapedSong> GetSongByKeyAsync(string key)
        {
            string url = BEATSAVER_DETAILS_BASE_URL + key;
            string pageText = "";
            ScrapedSong song = null;
            try
            {
                using (var response = await WebUtils.WebClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        Logger.Error($"Error getting song by key, {url} responded with {response.StatusCode}:{response.ReasonPhrase}");
                        return song;
                    }
                }
                if (string.IsNullOrEmpty(pageText))
                {
                    Logger.Warning($"Unable to get web page at {url}");
                    return null;
                }
            }
            catch (HttpRequestException)
            {
                Logger.Error($"HttpRequestException while trying to populate fields for {key}");
                return null;
            }
            catch (AggregateException ae)
            {
                ae.WriteExceptions($"Exception while trying to get details for {key}");
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception getting page", ex);
            }
            song = ParseSongsFromPage(pageText, url).FirstOrDefault();
            return song;
        }
        public static async Task<List<ScrapedSong>> SearchAsync(string criteria, SearchType type, BeatSaverFeedSettings settings = null)
        {
            // TODO: Hits rate limit
            if (type == SearchType.key)
            {
                return new List<ScrapedSong>() { await GetSongByKeyAsync(criteria).ConfigureAwait(false) };
            }

            if (type == SearchType.user)
            {
                return await GetSongsByUploaderIdAsync((await GetAuthorNamesByIDAsync(criteria).ConfigureAwait(false)).FirstOrDefault()).ConfigureAwait(false);
            }

            if (type == SearchType.hash)
            {
                return new List<ScrapedSong>() { await GetSongByHashAsync(criteria).ConfigureAwait(false) };
            }
            StringBuilder url;
            int maxSongs = 0;
            int maxPages = 0;
            //int lastPage;
            //int nextPage;
            int pageIndex = 0;
            if (settings != null)
            {
                maxSongs = settings.MaxSongs;
                maxPages = settings.MaxPages;
                pageIndex = Math.Max(settings.StartingPage - 1, 0);
            }
            bool useMaxPages = maxPages > 0;
            bool useMaxSongs = maxSongs > 0;
            if (useMaxPages && pageIndex > 0)
                maxPages = maxPages + pageIndex;
            bool continueLooping = true;
            var songs = new List<ScrapedSong>();
            List<ScrapedSong> newSongs;
            do
            {
                url = new StringBuilder(Feeds[BeatSaverFeeds.SEARCH].BaseUrl);
                url.Replace(SEARCHTYPEKEY, type.ToString());
                url.Replace(SEARCHKEY, criteria);
                url.Replace(PAGEKEY, pageIndex.ToString());
                string pageText = string.Empty;
                using (var response = await WebUtils.WebClient.GetAsync(url.ToString()).ConfigureAwait(false))
                {
                    Logger.Debug($"Checking {url.ToString()} for songs.");
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        Logger.Error($"Error searching for song, {url.ToString()} responded with {response.StatusCode}:{response.ReasonPhrase}");
                        return songs;
                    }
                }
                newSongs = ParseSongsFromPage(pageText, url.ToString());
                foreach (var song in newSongs)
                {
                    if (!useMaxSongs || songs.Count < maxSongs)
                        songs.Add(song);
                }
                pageIndex++;
                if (newSongs.Count == 0)
                    continueLooping = false;
                if (useMaxPages && (pageIndex >= maxPages))
                    continueLooping = false;
                if (useMaxSongs && pageIndex * SONGS_PER_PAGE >= maxSongs)
                    continueLooping = false;
            } while (continueLooping);

            return songs;
        }
        #endregion

        #region Sync
        /// <summary>
        /// Retrieves the songs from a feed with the given settings in the form of a Dictionary, with the key being the song's hash and a ScrapedSong as the value.
        /// </summary>
        /// <param name="_settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeatSaverFeedSettings</exception>
        /// <returns></returns>
        public Dictionary<string, ScrapedSong> GetSongsFromFeed(IFeedSettings _settings)
        {
            return GetSongsFromFeedAsync(_settings).Result;
        }
        public List<ScrapedSong> GetBeatSaverSongs(BeatSaverFeedSettings settings)
        {
            return GetBeatSaverSongsAsync(settings).Result;
        }

        public static List<string> GetAuthorNamesByID(string mapperId)
        {
            return GetAuthorNamesByIDAsync(mapperId).Result;
        }
        public static string GetAuthorID(string authorName)
        {
            return GetAuthorIDAsync(authorName).Result;
        }
        public static List<ScrapedSong> GetSongsFromPage(string url)
        {
            return GetSongsFromPageAsync(url).Result;
        }
        [Obsolete("Check this")]
        public static List<ScrapedSong> GetSongsByUploaderId(string authorId)
        {
            return GetSongsByUploaderIdAsync(authorId).Result;
        }
        /// <summary>
        /// Searches Beat Saver and retrieves all songs by the provided uploader name.
        /// </summary>
        /// <param name="uploader"></param>
        /// <returns></returns>
        public static List<ScrapedSong> GetSongsByAuthor(string uploader)
        {
            return GetSongsByAuthorAsync(uploader).Result;
        }
        public static ScrapedSong GetSongByHash(string hash)
        {
            return GetSongByHashAsync(hash).Result;
        }
        public static ScrapedSong GetSongByKey(string key)
        {
            return GetSongByKeyAsync(key).Result;
        }
        public static List<ScrapedSong> Search(string criteria, SearchType type)
        {
            return SearchAsync(criteria, type).Result;
        }


        #endregion

        #endregion

        public enum SearchType
        {
            author, // author name (not necessarily uploader)
            name, // song name only
            user, // user (uploader) name
            hash, // MD5 Hash
            song, // song name, song subname, author 
            key,
            all // name, user, song
        }

    }

    public class BeatSaverFeedSettings : IFeedSettings
    {
        /// <summary>
        /// Name of the chosen feed.
        /// </summary>
        public string FeedName { get { return BeatSaverReader.Feeds[Feed].Name; } } // Name of the chosen feed
        public BeatSaverFeeds Feed { get { return (BeatSaverFeeds)FeedIndex; } set { FeedIndex = (int)value; } } // Which feed to use
        public int FeedIndex { get; private set; } // Which feed to use

        /// <summary>
        /// List of authors, only used for the AUTHOR feed
        /// </summary>
        public string[] Authors { get; set; }

        /// <summary>
        /// Criteria for search, only used for SEARCH feed.
        /// </summary>
        public string SearchCriteria { get; set; }

        /// <summary>
        /// Type of search to perform, only used for SEARCH feed.
        /// Default is 'song' (song name, song subname, author)
        /// </summary>
        public BeatSaverReader.SearchType SearchType { get; set; }

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

        public BeatSaverFeedSettings(int feedIndex)
        {
            FeedIndex = feedIndex;
            MaxPages = 0;
            StartingPage = 1;
            SearchType = BeatSaverReader.SearchType.song;
        }
    }

    public enum BeatSaverFeeds
    {
        AUTHOR = 0,
        LATEST = 1,
        HOT = 2,
        PLAYS = 3,
        DOWNLOADS = 4,
        SEARCH = 98,
    }
}
