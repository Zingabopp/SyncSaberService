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
using static SyncSaberService.Web.HttpClientWrapper;
//using SimpleJSON;
namespace SyncSaberService.Web
{
    class BeatSaverReader : IFeedReader
    {
        public static string NameKey => "BeatSaverReader";
        public string Name { get { return NameKey; } }
        public static readonly string SourceKey = "BeatSaver";
        public string Source { get { return SourceKey; } }

        private static readonly string AUTHORKEY = "{AUTHOR}";
        private static readonly string AUTHORIDKEY = "{AUTHORID}";
        private static readonly string PAGEKEY = "{PAGE}";
        private const int SONGSPERUSERPAGE = 20;

        private ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        private static Dictionary<int, FeedInfo> _feeds;
        public static Dictionary<int, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<int, FeedInfo>()
                    {
                        { 0, new FeedInfo("author", "https://beatsaver.com/api/songs/byuser/" +  AUTHORIDKEY + "/" + PAGEKEY) },
                        { 1, new FeedInfo("newest", "https://beatsaver.com/api/songs/new/" + PAGEKEY) },
                        { 2, new FeedInfo("top", "https://beatsaver.com/api/songs/top/" + PAGEKEY) },
                        {99, new FeedInfo("search-by-author", "https://beatsaver.com/api/songs/search/user/" + AUTHORKEY) }
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
            }
            return new Playlist[0];
        }


        public string GetPageUrl(int feedIndex, string author = "", int pageIndex = 0)
        {
            string mapperId = string.Empty;
            if(!string.IsNullOrEmpty(author) && author.Length > 3)
                mapperId = GetAuthorID(author);
            return Feeds[feedIndex].BaseUrl.Replace(AUTHORIDKEY, mapperId).Replace(PAGEKEY, (pageIndex * SONGSPERUSERPAGE).ToString());
        }

        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeatSaverFeedSettings</exception>
        /// <returns></returns>
        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings _settings)
        {
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
                    songs.AddRange(GetNewestSongs(settings.MaxPages));
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

        public List<SongInfo> GetNewestSongs(int maxPages)
        {
            int feedIndex = 1;
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
            List<Task<string>> pageReadTasks = new List<Task<string>>();
            string url = "";
            do
            {
                songCount = songs.Count;
                url = GetPageUrl(feedIndex, "", pageNum);
                //Logger.Debug($"Creating task for {url}");
                pageReadTasks.Add(GetPageTextAsync(url));
                pageNum++;

            } while ((pageNum * SONGSPERUSERPAGE < numSongs) && (pageNum < maxPages));

            Task.WaitAll(pageReadTasks.ToArray());
            foreach (var job in pageReadTasks)
            {
                songs.AddRange(GetSongsFromPage(job.Result));
            }
            return songs;
        }

        public List<SongInfo> GetSongsByAuthor(string author)
        {
            int feedIndex = 0;
            List<SongInfo> songs = new List<SongInfo>();
            string pageText = GetPageText(GetPageUrl(feedIndex, author));

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
            if (numSongs == null) numSongs = 0;
            Logger.Info($"Found {numSongs} songs by {author}");
            int songCount = 0;
            int pageNum = 0;
            List<Task<string>> pageReadTasks = new List<Task<string>>();
            string url = "";
            do
            {
                songCount = songs.Count;
                url = GetPageUrl(feedIndex, author, pageNum);
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

        public List<SongInfo> GetSongsFromPage(string pageText)
        {
            //JSONNode result = JSON.Parse(pageText);
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
            foreach (var song in songJSONAry)
            {
                //JSONObject song = (JSONObject) aKeyValue;
                string songIndex = song["key"]?.Value<string>();
                string songName = song["songName"]?.Value<string>();
                string author = song["uploader"]?.Value<string>();
                string songUrl = "https://beatsaver.com/download/" + songIndex;
                /*
                SongInfo newSong;
                try
                {
                    newSong = song.ToObject<SongInfo>();
                    newSong.Feed = "followings";
                } catch(Exception ex)
                {
                    Logger.Exception($"Error deserializing song {songIndex}, using basic info", ex);
                    newSong = new SongInfo(songIndex, songName, songUrl, author, "followings");
                }
                */

                if (SongInfo.TryParseBeatSaver(song, out SongInfo newSong))
                {
                    newSong.Feed = "followings";
                    songs.Add(newSong);
                }
                else
                {
                    if (!(string.IsNullOrEmpty(songIndex)))
                    {
                        Logger.Warning($"Couldn't parse song {songIndex}, using sparse definition.");
                        songs.Add(new SongInfo(songIndex, songName, songUrl, author, "followings"));
                    }
                    else
                        Logger.Error("Unable to identify song, skipping");
                }

            }
            return songs;
        }

        public string GetAuthorID(string authorName)
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
                    searchURL = Feeds[99].BaseUrl.Replace(AUTHORKEY, a).Replace(PAGEKEY, (page * SONGSPERUSERPAGE).ToString());
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
                    searchURL = Feeds[99].BaseUrl.Replace(AUTHORKEY, a).Replace(PAGEKEY, (page * SONGSPERUSERPAGE).ToString());
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

    }

    public class BeatSaverFeedSettings : IFeedSettings
    {
        public int _feedIndex;
        public int MaxPages = 0;
        public string[] Authors;
        public string FeedName
        {
            get
            {
                return BeatSaverReader.Feeds[FeedIndex].Name;
            }
        }
        public int FeedIndex { get { return _feedIndex; } }
        public BeatSaverFeedSettings(int feedIndex)
        {
            _feedIndex = feedIndex;
        }

        public BeatSaverFeedSettings(int feedIndex, int _maxPages)
        {
            _feedIndex = feedIndex;
            MaxPages = _maxPages;
            Authors = new string[0];
        }
        public BeatSaverFeedSettings(int feedIndex, string _author)
        {
            _feedIndex = feedIndex;
            Authors = new string[] { _author };
            MaxPages = 0;
        }
    }
}
