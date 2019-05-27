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
        private static readonly string AUTHORKEY = "{AUTHOR}";
        private static readonly string AUTHORIDKEY = "{AUTHORID}";
        private static readonly string PAGEKEY = "{PAGE}";
        private const int SONGSPERUSERPAGE = 20;
        public static string NameKey => "BeatSaverReader";
        public string Name { get { return NameKey; } }
        private ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        private Dictionary<int, FeedInfo> _feeds;
        public Dictionary<int, FeedInfo> Feeds
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

        public string GetPageUrl(int feedIndex, string author = "", int pageIndex = 0)
        {
            string mapperId = string.Empty;

            mapperId = _authors.GetOrAdd(author, (a) => {
                string searchURL = Feeds[99].BaseUrl.Replace(AUTHORKEY, a);
                string pageText = GetPageText(searchURL);
                //JSONNode result = JSON.Parse(pageText);
                JObject result = new JObject() ;
                try
                {
                    result = JObject.Parse(pageText);

                }catch(Exception ex)
                {
                    Logger.Exception("Unable to parse JSON from text", ex);
                }
                var totalResults = result["total"]?.Value<int>();
                if (totalResults == null || totalResults == 0)
                {
                    Logger.Warning($"No songs by {a} found, is the name spelled correctly?");
                    return "0";
                }
                //var songJSONAry = result["songs"].AsArray;
                var songJSONAry = result["songs"].ToArray();
                //var matchingSong = songJSONAry.Children.FirstOrDefault(c => c["uploader"].Value.ToLower() == a.ToLower());
                var matchingSong = songJSONAry.FirstOrDefault(c => c["uploader"]?.Value<string>()?.ToLower() == a.ToLower());
                if (matchingSong == null)
                {
                    Logger.Warning($"No songs by {a} found, is the name spelled correctly?");
                    return "0";
                }
                return matchingSong["uploaderId"].Value<string>();
            });

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
            string pageText = GetPageText(GetPageUrl(settings.FeedIndex, settings.Author));
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
            string mapperId = string.Empty;
            int? numSongs = result["total"]?.Value<int>();
            if (numSongs == null) numSongs = 0;
            Logger.Info($"Found {numSongs} songs by {settings.Author}");
            int songCount = 0;
            int pageNum = 0;
            List<Task<string>> pageReadTasks = new List<Task<string>>();
            string url = "";
            do
            {
                songCount = songs.Count;
                url = GetPageUrl(settings.FeedIndex, settings.Author, pageNum);
                Logger.Debug($"Creating task for {url}");
                pageReadTasks.Add(GetPageTextAsync(url));
                pageNum++;

            } while (pageNum * SONGSPERUSERPAGE < numSongs);
         
            Task.WaitAll(pageReadTasks.ToArray());
            foreach (var job in pageReadTasks)
            {
                songs.AddRange(GetSongsFromPage(job.Result));
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
            if(songJSONAry == null)
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
    }

    public class BeatSaverFeedSettings : IFeedSettings
    {
        public int FeedIndex;
        public int MaxPages;
        public string Author;

        public BeatSaverFeedSettings(int _feedIndex, int _maxPages)
        {
            FeedIndex = _feedIndex;
            MaxPages = _maxPages;
            Author = "";
        }
        public BeatSaverFeedSettings(int _feedIndex, string _author)
        {
            FeedIndex = _feedIndex;
            Author = _author;
            MaxPages = 0;
        }
    }
}
