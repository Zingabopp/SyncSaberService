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
using SyncSaberLib.Data;
using static SyncSaberLib.Utilities;
using static SyncSaberLib.Web.WebUtils;

namespace SyncSaberLib.Web
{
    public class BeatSaverReader : IFeedReader
    {
        public static string NameKey => "BeatSaverReader";
        public string Name { get { return NameKey; } }
        public static readonly string SourceKey = "BeatSaver";
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }
        //private static readonly string AUTHORKEY = "{AUTHOR}";
        private static readonly string AUTHORIDKEY = "{AUTHORID}";
        private static readonly string PAGEKEY = "{PAGE}";
        private static readonly string SEARCHTYPEKEY = "{TYPE}";
        private static readonly string SEARCHKEY = "{SEARCH}";
        private const int SONGSPERUSERPAGE = 10;
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";
        private const string BEATSAVER_DETAILS_BASE_URL = "https://beatsaver.com/api/maps/detail/";
        private const string BEATSAVER_GETBYHASH_BASE_URL = "https://beatsaver.com/api/maps/by-hash/";

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
        private readonly Playlist _beatSaverNewest = new Playlist("BeatSaverNewestPlaylist", "BeatSaver Newest", "SyncSaber", "1");

        public Playlist[] PlaylistsForFeed(int feedIndex)
        {
            switch (feedIndex)
            {
                case 1:
                    return new Playlist[] { _beatSaverNewest };
                default:
                    break;
            }
            return new Playlist[0];
        }

        public void PrepareReader()
        {
            Ready = true;
        }

        public static string GetPageUrl(int feedIndex, int pageIndex = 0, Dictionary<string, string> replacements = null)
        {
            string mapperId = string.Empty;
            StringBuilder url = new StringBuilder(Feeds[(BeatSaverFeeds) feedIndex].BaseUrl);
            //if (!string.IsNullOrEmpty(author) && author.Length > 3)
            //    mapperId = GetAuthorID(author);
            if (replacements != null)
                foreach (var key in replacements.Keys)
                {
                    url.Replace(key, replacements[key]);
                }
            return url.Replace(PAGEKEY, pageIndex.ToString()).ToString();
        }

        public static List<BeatSaverSong> ScrapeBeatSaver(int requestDelay, bool onlyGetNew, int maxPages = 0)
        {
            int feedIndex = (int) BeatSaverFeeds.LATEST;
            bool useMaxPages = maxPages != 0;
            int latestVersion = 0;
            if (ScrapedDataProvider.Songs.Values.Count > 0)
                latestVersion = ScrapedDataProvider.Songs.Values.Max(s => s.BeatSaverInfo?.KeyAsInt ?? 0);
            List<BeatSaverSong> songs = new List<BeatSaverSong>();
            string pageText = GetPageText(GetPageUrl(feedIndex));
            JObject result = new JObject();
            try { result = JObject.Parse(pageText); }
            catch (Exception ex) { Logger.Exception("Unable to parse JSON from text", ex); }
            string mapperId = string.Empty;
            int? numSongs = result["totalDocs"]?.Value<int>();
            if (numSongs == null || numSongs == 0) return songs;
            var totalPages = result["lastPage"]?.Value<int>();
            Logger.Info($"{numSongs} songs available on {totalPages} pages");
            int songCount = 0;
            int pageNum = 0;
            string url = "";
            List<BeatSaverSong> newSongs;
            bool continueLooping = true;
            do
            {
                bool retry = false;
                int attempts = 0;
                if (pageNum % 10 == 0)
                    Logger.Info($"On page {pageNum} / {totalPages}");
                url = GetPageUrl(feedIndex, pageNum);
                do
                {
                    attempts++;
                    Thread.Sleep(requestDelay);
                    songCount = songs.Count;
                    pageText = GetPageText(url);
                    newSongs = ParseSongsFromPage(pageText);

                    if (newSongs.Count == 0)
                    {
                        retry = true;
                    }
                    else
                    {
                        songs.AddRange(newSongs);
                        retry = false;
                    }
                } while (retry && attempts < 5);

                pageNum++;
                if (onlyGetNew && newSongs.Min(s => s.KeyAsInt) <= latestVersion)
                    continueLooping = false;
                if (pageNum > totalPages)
                    continueLooping = false;
                if (useMaxPages && (pageNum >= maxPages))
                    continueLooping = false;
            } while (continueLooping);
            Logger.Info($"Scraped {songs.Count} new songs");
            return songs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeatSaverFeedSettings</exception>
        /// <returns></returns>
        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings _settings)
        {
            PrepareReader();
            if (!(_settings is BeatSaverFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            List<SongInfo> songs = new List<SongInfo>();

            switch ((BeatSaverFeeds) settings.FeedIndex)
            {
                // Author
                case BeatSaverFeeds.AUTHOR:
                    List<SongInfo> newSongs = null;
                    string songSource = string.Empty;
                    foreach (var author in settings.Authors)
                    {
                        if (!settings.searchOnline)
                        {
                            newSongs = ScrapedDataProvider.Songs.Values.Where(s => !string.IsNullOrEmpty(s.BeatSaverInfo?.uploader?.username) && s.BeatSaverInfo?.uploader.username.ToLower() == author.ToLower()).ToList();
                            songSource = "scraped data";
                        }
                        if (newSongs == null || newSongs.Count == 0)
                        {
                            newSongs = GetSongsByAuthor(author);
                            songSource = "Beat Saver";
                        }
                        songs.AddRange(newSongs);
                        Logger.Info($"Found {newSongs.Count} songs uploaded by {author} from {songSource}");
                    }
                    break;
                // Newest
                case BeatSaverFeeds.LATEST:
                    songs.AddRange(GetNewestSongs(settings));
                    break;
                // Top
                case BeatSaverFeeds.HOT:
                    break;
                default:
                    break;
            }

            Dictionary<int, SongInfo> retDict = new Dictionary<int, SongInfo>();
            foreach (var song in songs)
            {
                if (retDict.ContainsKey(song.keyAsInt))
                {/*
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

        public List<SongInfo> GetNewestSongs(BeatSaverFeedSettings settings)
        {
            int feedIndex = 1;
            bool useMaxPages = settings.MaxPages != 0;
            List<SongInfo> songs = new List<SongInfo>();
            string pageText = GetPageText(GetPageUrl(feedIndex));

            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (Exception ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            string mapperId = string.Empty;
            int? numSongs = result["total"]?.Value<int>();
            if (numSongs == null || numSongs == 0) return songs;
            Logger.Info($"{numSongs} songs available");
            int songCount = 0;
            int pageNum = 0;
            List<Task<List<SongInfo>>> pageReadTasks = new List<Task<List<SongInfo>>>();
            string url = "";
            bool continueLooping = true;
            do
            {
                songCount = songs.Count;
                url = GetPageUrl(feedIndex, pageNum);
                //Logger.Debug($"Creating task for {url}");
                pageReadTasks.Add(GetSongsFromPageAsync(url, true));
                pageNum++;
                if ((pageNum * SONGSPERUSERPAGE >= numSongs))
                    continueLooping = false;
                if (useMaxPages && (pageNum >= settings.MaxPages))
                    continueLooping = false;
            } while (continueLooping);

            Task.WaitAll(pageReadTasks.ToArray());
            foreach (var job in pageReadTasks)
            {
                songs.AddRange(job.Result);
            }
            return songs;
        }

        public static async Task<List<SongInfo>> GetSongsFromPageAsync(string url, bool useDateLimit = false)
        {
            string pageText = await GetPageTextAsync(url).ConfigureAwait(false);
            List<SongInfo> songs = GetSongsFromPage(pageText);
            return songs;
        }

        public List<SongInfo> GetSongsByAuthor(string author)
        {
            string mapperId = GetAuthorID(author);
            return GetSongsByUploaderId(mapperId);
        }

        public static List<SongInfo> GetSongsByUploaderId(string authorId)
        {
            int feedIndex = 0;
            List<SongInfo> songs = new List<SongInfo>();

            string pageText = GetPageText(GetPageUrl(feedIndex, 0, new Dictionary<string, string>() { { AUTHORIDKEY, authorId } }));

            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (Exception ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            //string mapperId = GetAuthorID(authorId);
            //var scrapedResults = ScrapedDataProvider.BeatSaverScrape.Where(s => s.EnhancedInfo.uploaderId.ToString() == authorId.ToLower() || authorNames.Contains(s.authorName));


            int? numSongs = result["total"]?.Value<int>();
            if (numSongs == null) numSongs = 0;
            //Logger.Info($"Found {numSongs} songs by {authorId} on Beat Saver");
            int songCount = 0;
            int pageNum = 0;
            List<Task<string>> pageReadTasks = new List<Task<string>>();
            string url = "";
            do
            {
                songCount = songs.Count;
                url = GetPageUrl(feedIndex, pageNum, new Dictionary<string, string>() { { AUTHORIDKEY, authorId } });
                //Logger.Debug($"Creating task for {url}");
                pageReadTasks.Add(GetPageTextAsync(url));
                pageNum++;
            } while (pageNum * SONGSPERUSERPAGE < numSongs);

            Task.WaitAll(pageReadTasks.ToArray());
            foreach (var job in pageReadTasks)
            {
                songs.AddRange(GetSongsFromPage(job.Result));
            }
            return songs;
        }

        public static List<SongInfo> GetSongsFromPage(string url)
        {
            string pageText = GetPageText(url);
            var songs = new List<SongInfo>();
            foreach (var song in ParseSongsFromPage(pageText))
            {
                songs.Add(ScrapedDataProvider.GetOrCreateSong(song));
            }
            return songs;
        }

        public static List<BeatSaverSong> ParseSongsFromPage(string pageText)
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
            List<BeatSaverSong> songs = new List<BeatSaverSong>();
            BeatSaverSong newSong;
            int? resultTotal = result["totalDocs"]?.Value<int>();
            if (resultTotal == null) resultTotal = 0;

            // Single song in page text.
            if (resultTotal == 0)
            {
                if (result["key"] != null)
                {
                    newSong = ParseSongFromJson(result);
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
            }
            
            foreach (JObject song in songJSONAry)
            {
                newSong = ParseSongFromJson(song);
                if (newSong != null)
                    songs.Add(newSong);
            }
            return songs;
        }

        /// <summary>
        /// Creates a SongInfo from a JObject. Sets the ScrapedAt time for the song.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static BeatSaverSong ParseSongFromJson(JObject song)
        {
            //JSONObject song = (JSONObject) aKeyValue;
            string songIndex = song["key"]?.Value<string>();
            string songName = song["name"]?.Value<string>();
            string author = song["uploader"]?["username"]?.Value<string>();
            string songUrl = "https://beatsaver.com/download/" + songIndex;

            if (BeatSaverSong.TryParseBeatSaver(song, out BeatSaverSong newSong))
            {
                newSong.ScrapedAt = DateTime.Now;
                SongInfo songInfo = ScrapedDataProvider.GetOrCreateSong(newSong);
                
                songInfo.BeatSaverInfo = newSong;
                return newSong;
            }
            else
            {
                if (!(string.IsNullOrEmpty(songIndex)))
                {
                    // TODO: look at this
                    Logger.Warning($"Couldn't parse song {songIndex}, skipping.");// using sparse definition.");
                    //return new SongInfo(songIndex, songName, songUrl, author);
                }
                else
                    Logger.Error("Unable to identify song, skipping");
            }
            return null;
        }

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

        public static List<SongInfo> Search(string criteria, SearchType type)
        {

            if (type == SearchType.key)
            {
                return new List<SongInfo>() { GetSongByKey(criteria) };
            }

            if (type == SearchType.user)
            {
                return GetSongsByUploaderId(criteria);
            }
            StringBuilder url;
            url = new StringBuilder(Feeds[BeatSaverFeeds.SEARCH].BaseUrl);
            url.Replace(SEARCHTYPEKEY, type.ToString());
            url.Replace(SEARCHKEY, criteria);

            string pageText = GetPageText(url.ToString());
            var songs = new List<SongInfo>();
            foreach (var song in ParseSongsFromPage(pageText))
            {
                songs.Add(ScrapedDataProvider.GetOrCreateSong(song));
            }
            return songs;
        }

        public static SongInfo GetSongByKey(string key)
        {

            string url = BEATSAVER_DETAILS_BASE_URL + key;
            string pageText = "";
            BeatSaverSong song = new BeatSaverSong();
            try
            {
                var pageTask = WebUtils.TryGetStringAsync(url);
                pageTask.Wait();
                pageText = pageTask.Result;
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
            catch(AggregateException ae)
            {
                ae.WriteExceptions($"Exception while trying to get details for {key}");
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception getting page", ex);
            }
            song = ParseSongsFromPage(pageText).FirstOrDefault();
            song.ScrapedAt = DateTime.Now;
            return ScrapedDataProvider.GetOrCreateSong(song);
        }

        public static string GetAuthorID(string authorName)
        {
            string mapperId = ScrapedDataProvider.Songs.Values.Where(s => s.BeatSaverInfo.uploader.username.ToLower() == authorName.ToLower()).FirstOrDefault()?.BeatSaverInfo.uploader.id;
            if (!string.IsNullOrEmpty(mapperId))
                return mapperId;
            mapperId = _authors.GetOrAdd(authorName, (a) => {
                int page = 0;
                int? totalResults;
                string searchURL, pageText;
                JObject result;
                JToken matchingSong;
                JToken[] songJSONAry;
                do
                {
                    Logger.Debug($"Checking page {page + 1} for the author ID.");
                    searchURL = Feeds[BeatSaverFeeds.SEARCH].BaseUrl.Replace(SEARCHKEY, a).Replace(PAGEKEY, (page * SONGSPERUSERPAGE).ToString());
                    pageText = GetPageText(searchURL);
                    result = new JObject();
                    try { result = JObject.Parse(pageText); }
                    catch (Exception ex) { Logger.Exception("Unable to parse JSON from text", ex); }
                    totalResults = result["total"]?.Value<int>();
                    if (totalResults == null || totalResults == 0)
                    {
                        Logger.Warning($"No songs by {a} found, is the name spelled correctly?");
                        return "0";
                    }
                    songJSONAry = result["docs"].ToArray();
                    matchingSong = songJSONAry.FirstOrDefault(c => c["uploader"]?.Value<string>()?.ToLower() == a.ToLower());

                    //Logger.Debug($"Creating task for {url}");
                    page++;
                    searchURL = Feeds[BeatSaverFeeds.SEARCH].BaseUrl.Replace(SEARCHKEY, a).Replace(PAGEKEY, (page * SONGSPERUSERPAGE).ToString());
                } while ((matchingSong == null) && page * SONGSPERUSERPAGE < totalResults);


                if (matchingSong == null)
                {
                    Logger.Warning($"No songs by {a} found, is the name spelled correctly?");
                    return "0";
                }
                return matchingSong["uploaderId"].Value<string>();
            });
            return mapperId;
        }

        public static List<string> GetAuthorNamesByID(string mapperId)
        {
            List<string> authorNames = ScrapedDataProvider.Songs.Values.Where(s => s.BeatSaverInfo.uploader.id == mapperId).Select(s => s.BeatSaverInfo.uploader.username).Distinct().ToList();
            if (authorNames.Count > 0)
                return authorNames;
            List<SongInfo> songs = GetSongsByUploaderId(mapperId);
            authorNames = songs.Select(s => s.authorName).Distinct().ToList();
            authorNames.ForEach(n => Logger.Warning($"Found authorName: {n}"));
            return authorNames;
        }

    }

    public class BeatSaverFeedSettings : IFeedSettings
    {
        public int _feedIndex;
        public int MaxPages = 0;
        public string[] Authors;
        public string FeedName { get { return BeatSaverReader.Feeds[Feed].Name; } }
        public BeatSaverFeeds Feed { get { return (BeatSaverFeeds) FeedIndex; } set { _feedIndex = (int) value; } }
        public int FeedIndex { get { return _feedIndex; } }
        public bool UseSongKeyAsOutputFolder { get; set; }
        public bool searchOnline { get; set; }
        public int MaxSongs { get; set; }

        public BeatSaverFeedSettings(int feedIndex)
        {
            searchOnline = false;
            _feedIndex = feedIndex;
            UseSongKeyAsOutputFolder = true;
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
