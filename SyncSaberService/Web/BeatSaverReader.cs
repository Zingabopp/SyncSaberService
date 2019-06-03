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
using SyncSaberService.Data;
using static SyncSaberService.Utilities;
using static SyncSaberService.Web.WebUtils;

namespace SyncSaberService.Web
{
    class BeatSaverReader : IFeedReader
    {
        public static string NameKey => "BeatSaverReader";
        public string Name { get { return NameKey; } }
        public static readonly string SourceKey = "BeatSaver";
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }
        private static readonly string AUTHORKEY = "{AUTHOR}";
        private static readonly string AUTHORIDKEY = "{AUTHORID}";
        private static readonly string PAGEKEY = "{PAGE}";
        private static readonly string SEARCHTYPEKEY = "{TYPE}";
        private static readonly string SEARCHKEY = "{SEARCH}";
        private const int SONGSPERUSERPAGE = 20;
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";
        private const string BEATSAVER_DETAILS_BASE_URL = "https://beatsaver.com/api/songs/detail/";

        private static ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        private static Dictionary<BeatSaverFeeds, FeedInfo> _feeds;
        public static Dictionary<BeatSaverFeeds, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeatSaverFeeds, FeedInfo>()
                    {
                        { (BeatSaverFeeds)0, new FeedInfo("author", "https://beatsaver.com/api/songs/byuser/" +  AUTHORIDKEY + "/" + PAGEKEY) },
                        { (BeatSaverFeeds)1, new FeedInfo("newest", "https://beatsaver.com/api/songs/new/" + PAGEKEY) },
                        { (BeatSaverFeeds)2, new FeedInfo("top", "https://beatsaver.com/api/songs/top/" + PAGEKEY) },
                        { (BeatSaverFeeds)98, new FeedInfo("search", $"https://beatsaver.com/api/songs/search/{SEARCHTYPEKEY}/{SEARCHKEY}") },
                        { (BeatSaverFeeds)99, new FeedInfo("search-by-author", "https://beatsaver.com/api/songs/search/user/" + AUTHORKEY) }
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
            return url.Replace(PAGEKEY, (pageIndex * SONGSPERUSERPAGE).ToString()).ToString();
        }

        public static List<SongInfo> ScrapeBeatSaver(int requestDelay, int maxPages = 0)
        {
            int feedIndex = (int)BeatSaverFeeds.NEWEST;
            bool useMaxPages = maxPages != 0;
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
            string url = "";
            bool continueLooping = true;
            do
            {
                songCount = songs.Count;
                url = GetPageUrl(feedIndex, pageNum);
                //Logger.Debug($"Creating task for {url}");
                songs.AddRange(GetSongsFromPage(url));
                pageNum++;
                if ((pageNum * SONGSPERUSERPAGE >= numSongs))
                    continueLooping = false;
                if (useMaxPages && (pageNum >= maxPages))
                    continueLooping = false;
            } while (continueLooping);
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

            switch (settings.FeedIndex)
            {
                // Author
                case 0:
                    foreach (var author in settings.Authors)
                    {
                        songs.AddRange(GetSongsByAuthor(author));
                    }
                    break;
                // Newest
                case 1:
                    songs.AddRange(GetNewestSongs(settings));
                    break;
                // Top
                case 2:
                    break;
                default:
                    break;
            }

            Dictionary<int, SongInfo> retDict = new Dictionary<int, SongInfo>();
            foreach (var song in songs)
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
            Logger.Info($"Found {numSongs} songs by {authorId} on Beat Saver");
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
            return ParseSongsFromPage(pageText);
        }

        public static List<SongInfo> ParseSongsFromPage(string pageText)
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
            List<SongInfo> songs = new List<SongInfo>();
            int? resultTotal = result["total"]?.Value<int>();
            if (resultTotal == null) resultTotal = 0;
            if (resultTotal == 0)
            {
                return songs;
            }
            var songJSONAry = result["songs"]?.ToArray();
            if (songJSONAry == null)
            {
                Logger.Error("Invalid page text: 'songs' field not found.");
            }
            foreach (JObject song in songJSONAry)
            {
                //JSONObject song = (JSONObject) aKeyValue;
                string songIndex = song["key"]?.Value<string>();
                string songName = song["songName"]?.Value<string>();
                string author = song["uploader"]?.Value<string>();
                string songUrl = "https://beatsaver.com/download/" + songIndex;

                if (SongInfoEnhanced.TryParseBeatSaver(song, out SongInfo newSong))
                {
                    //newSong.Feed = "followings"; // TODO: What?
                    songs.Add(newSong);
                }
                else
                {
                    if (!(string.IsNullOrEmpty(songIndex)))
                    {
                        Logger.Warning($"Couldn't parse song {songIndex}, using sparse definition.");
                        songs.Add(new SongInfo(songIndex, songName, songUrl, author));
                    }
                    else
                        Logger.Error("Unable to identify song, skipping");
                }

            }
            return songs;
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
            return ParseSongsFromPage(pageText);
        }

        public static SongInfo GetSongByKey(string key)
        {
            string url = BEATSAVER_DETAILS_BASE_URL + key;
            string pageText = "";
            SongInfoEnhanced song = new SongInfoEnhanced();
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
            catch (Exception ex) // TODO: Throws AggregateException when page not found.
            {
                Logger.Exception("Exception getting page", ex);
            }
            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (JsonReaderException ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }

            JsonConvert.PopulateObject(result["song"].ToString(), song);
            return song.ToSongInfo();
        }

        public static string GetAuthorID(string authorName)
        {
            string mapperId = _authors.GetOrAdd(authorName, (a) => {
                int page = 0;
                int? totalResults;
                string searchURL, pageText;
                JObject result;
                JToken matchingSong;
                JToken[] songJSONAry;
                do
                {
                    Logger.Debug($"Checking page {page + 1} for the author ID.");
                    searchURL = Feeds[BeatSaverFeeds.SEARCH_BY_AUTHOR].BaseUrl.Replace(AUTHORKEY, a).Replace(PAGEKEY, (page * SONGSPERUSERPAGE).ToString());
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
                    songJSONAry = result["songs"].ToArray();
                    matchingSong = songJSONAry.FirstOrDefault(c => c["uploader"]?.Value<string>()?.ToLower() == a.ToLower());

                    //Logger.Debug($"Creating task for {url}");
                    page++;
                    searchURL = Feeds[BeatSaverFeeds.SEARCH_BY_AUTHOR].BaseUrl.Replace(AUTHORKEY, a).Replace(PAGEKEY, (page * SONGSPERUSERPAGE).ToString());
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
            List<SongInfo> songs = GetSongsByUploaderId(mapperId);
            List<string> authorNames = songs.Select(s => s.authorName).Distinct().ToList();
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

        public BeatSaverFeedSettings(int feedIndex)
        {
            _feedIndex = feedIndex;
            UseSongKeyAsOutputFolder = true;
        }
    }

    public enum BeatSaverFeeds
    {
        AUTHOR = 0,
        NEWEST = 1,
        TOP = 2,
        SEARCH = 98,
        SEARCH_BY_AUTHOR = 99
    }
}
