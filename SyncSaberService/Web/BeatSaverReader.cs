using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SimpleJSON;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Http;
using static SyncSaberService.Utilities;
using static SyncSaberService.Web.HttpClientWrapper;

namespace SyncSaberService.Web
{
    class BeatSaverReader : IFeedReader
    {
        private static readonly string AUTHORKEY = "{AUTHOR}";
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
                        { 0, new FeedInfo("author", "https://beatsaver.com/api/songs/byuser/" +  AUTHORKEY + "/" + PAGEKEY) },
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
                JSONNode result = JSON.Parse(pageText);
                Logger.Info($"Getting mapper ID, a = {a}");
                if (result["total"].AsInt == 0)
                {
                    return searchURL;
                }
                var songJSONAry = result["songs"].AsArray;
                return songJSONAry.Children.First(c => c["uploader"].Value.ToLower() == a.ToLower())["uploaderId"].Value;
            });

            return Feeds[feedIndex].BaseUrl.Replace(AUTHORKEY, mapperId).Replace(PAGEKEY, (pageIndex * SONGSPERUSERPAGE).ToString());
        }

        private static readonly string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";

        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings _settings)
        {
            var settings = _settings as BeatSaverFeedSettings;
            if (settings == null)
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            List<SongInfo> songs = new List<SongInfo>();
            string pageText = GetPageText(GetPageUrl(settings.FeedIndex, settings.Author));
            JSONNode result = JSON.Parse(pageText);
            string mapperId = string.Empty;
            int numSongs = result["total"].AsInt;
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
                if (retDict.ContainsKey(song.SongID))
                {
                    if (retDict[song.SongID].SongVersion < song.SongVersion)
                    {
                        Logger.Debug($"Song with ID {song.SongID} already exists, updating");
                        retDict[song.SongID] = song;
                    }
                    else
                    {
                        Logger.Debug($"Song with ID {song.SongID} is already the newest version");
                    }
                }
                else
                {
                    retDict.Add(song.SongID, song);
                }
            }
            return retDict;
        }

        public List<SongInfo> GetSongsFromPage(string pageText)
        {
            JSONNode result = JSON.Parse(pageText);
            List<SongInfo> songs = new List<SongInfo>();
            if (result["total"].AsInt == 0)
            {
                return songs;
            }
            var songJSONAry = result["songs"].Linq.ToArray();

            foreach (JSONObject song in songJSONAry)
            {
                //JSONObject song = (JSONObject) aKeyValue;
                string songIndex = song["version"].Value;
                string songName = song["songName"].Value;
                string author = song["uploader"].Value;
                string songUrl = "https://beatsaver.com/download/" + songIndex;
                songs.Add(new SongInfo(songIndex, songName, songUrl, author, "followings"));
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
