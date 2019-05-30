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
using static SyncSaberService.Web.HttpClientWrapper;

namespace SyncSaberService.Web
{
    class ScoreSaberReader : IFeedReader
    {
        /// API Examples:
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=50&page=1&ranked=1 // Sorted by PP
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=10&page=1&search=honesty&ranked=1
        #region Constants
        public static readonly string NameKey = "ScoreSaberReader";
        public static readonly string SourceKey = "ScoreSaber";
        private static readonly string USERNAMEKEY = "{USERNAME}";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        private static readonly string CATKEY = "{CAT}";
        private static readonly string RANKEDKEY = "{RANKKEY}";
        private static readonly string LIMITKEY = "{LIMIT}";
        private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private static readonly Uri FeedRootUri = new Uri("https://bsaber.com");
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a ScoreSaberFeedSettings.";
        #endregion

        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }

        private static Dictionary<int, FeedInfo> _feeds;
        public static Dictionary<int, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<int, FeedInfo>()
                    {
                        { 0, new FeedInfo("top ranked", $"https://scoresaber.com/api.php?function=get-leaderboards&cat={CATKEY}&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") }
                    };
                }
                return _feeds;
            }
        }

        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings _settings)
        {
            PrepareReader();
            if (!(_settings is ScoreSaberFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            List<SongInfo> songs = new List<SongInfo>();

            switch (settings.FeedIndex)
            {
                // Author
                case 0:
                    songs.AddRange(GetTopPPSongs(settings));
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
                    else if(retDict[song.id].SongVersion == song.SongVersion)
                    {
                        Logger.Debug($"Tried to add a song we already got");
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




            /*
            var pageText = Web.HttpClientWrapper.GetPageText("https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=50&page=1&ranked=1");
            List<ScoreSaberSong> songs = GetSSSongsFromPage(pageText);

            Dictionary<int, SongInfo> retDict = new Dictionary<int, SongInfo>();
            SongInfo tempSong;
            foreach (var song in songs)
            {
                tempSong = song.GetSongInfo();
                if (tempSong != null && !string.IsNullOrEmpty(tempSong.key))
                {
                    retDict.AddOrUpdate(tempSong.id, tempSong);
                }
                else
                {
                    Logger.Warning($"Could not convert song {song.name} with hash {song.md5Hash} to a SongInfo, skipping...");
                }
            }
            */
            //for (int i = 0; i < songs.Count; i++)
            // retDict.AddOrUpdate(i, songs[i]);
            //return retDict;
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
            int songsPerPage = 40;
            int pageNum = 1;
            bool useMaxPages = settings.MaxPages != 0;
            List<SongInfo> songs = new List<SongInfo>();
            StringBuilder url = new StringBuilder(Feeds[settings.FeedIndex].BaseUrl);
            Dictionary<string, string> urlReplacements = new Dictionary<string, string>() {
                { CATKEY, "3" },
                {LIMITKEY, songsPerPage.ToString() },
                {PAGENUMKEY, pageNum.ToString()},
                {RANKEDKEY, "1" }
            };
            GetPageUrl(ref url, urlReplacements);

            string pageText = GetPageText(url.ToString());
            
            JObject result = new JObject();
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
                url.Clear();
                url.Append(Feeds[settings.FeedIndex].BaseUrl);
                urlReplacements.AddOrUpdate(PAGENUMKEY, pageNum.ToString());
                GetPageUrl(ref url, urlReplacements);
                Logger.Warning($"Adding pageReadTask {url.ToString()}");
                pageReadTasks.Add(GetSongsFromPageAsync(url.ToString()));
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
        public async Task<List<SongInfo>> GetSongsFromPageAsync(string url)
        {
            string pageText = await GetPageTextAsync(url).ConfigureAwait(false);
            testBag.Add(pageText);
            List<SongInfo> songs = GetSongsFromPage(pageText);
            return songs;
        }
        ConcurrentBag<string> testBag = new ConcurrentBag<string>();
        public List<SongInfo> GetSongsFromPage(string pageText)
        {
            var sssongs = GetSSSongsFromPage(pageText);

            List<SongInfo> songs = new List<SongInfo>();
            SongInfo tempSong;
            foreach (var song in sssongs)
            {
                tempSong = song.GetSongInfo();
                if (tempSong != null && !string.IsNullOrEmpty(tempSong.key))
                {
                    songs.Add(tempSong);
                }
                else
                {
                    Logger.Warning($"Could not convert song {song.name} with hash {song.md5Hash} to a SongInfo, skipping...");
                }
            }

            return songs;
        }

        public List<ScoreSaberSong> GetSSSongsFromPage(string pageText)
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
            foreach (var song in songJSONAry)
            {
                //JSONObject song = (JSONObject) aKeyValue;
                //string songIndex = song["key"]?.Value<string>();
                string songHash = song["id"]?.Value<string>();
                string songName = song["name"]?.Value<string>();
                string author = song["author"]?.Value<string>();
                //string songUrl = "https://beatsaver.com/download/" + songIndex;

                if (ScoreSaberSong.TryParseScoreSaberSong(song, out ScoreSaberSong newSong))
                {
                    //newSong.Feed = "followings";
                    songs.Add(newSong);
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

            }
            return songs;
        }

        public Playlist[] PlaylistsForFeed(int feedIndex)
        {
            switch (feedIndex)
            {
                case 0:
                    return new Playlist[] { _topPPSongs };
                default:
                    break;
            }
            return new Playlist[] { };
        }

        public void PrepareReader()
        {

        }

        private readonly Playlist _topPPSongs = new Playlist("ScoreSaberPP", "ScoreSaber Top PP", "SyncSaber", "1");
    }

    public class ScoreSaberFeedSettings : IFeedSettings
    {
        public string FeedName
        {
            get
            {
                return ScoreSaberReader.Feeds[FeedIndex].Name;
            }
        }
        public int FeedIndex { get { return _feedIndex; } }
        public int MaxPages;
        public int _feedIndex;
        public ScoreSaberFeedSettings(int feedIndex)
        {
            _feedIndex = feedIndex;
        }
    }
}
