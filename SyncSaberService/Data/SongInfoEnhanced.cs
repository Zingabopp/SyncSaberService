using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using SyncSaberService.Web;


namespace SyncSaberService.Data
{
    /// <summary>
    /// TODO: Make Scrapped song the base with separate objects (using Lazy<>) for Beat Saver and Score Saber data.
    /// </summary>
    public class SongInfoEnhanced
    {
        private const string SONG_DETAILS_URL_BASE = "https://beatsaver.com/api/songs/detail/";
        private const string SONG_BY_HASH_URL_BASE = "https://beatsaver.com/api/songs/search/hash/";

        [JsonIgnore]
        public bool Populated { get; private set; }
        private static readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        private static readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);

        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(SongInfo);
                object retVal;
                FieldInfo test = myType.GetField(propertyName);
                if (test != null)
                {
                    retVal = test.GetValue(this);
                }
                else
                {
                    PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                    retVal = myPropInfo.GetValue(this);
                }

                Type whatType = retVal.GetType();
                return retVal;
            }
            set
            {
                Type myType = typeof(SongInfo);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);
            }
        }

        /// <summary>
        /// Downloads the page and returns it as a string.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public static bool PopulateFields(SongInfoEnhanced song)
        {
            if (song.Populated)
                return true;
            bool successful = true;
            SongGetMethod searchMethod;
            string url;
            if (!string.IsNullOrEmpty(song.key))
            {
                url = SONG_DETAILS_URL_BASE + song.key;
                searchMethod = SongGetMethod.SongIndex;
            }
            else if (!string.IsNullOrEmpty(song.hashMd5))
            {
                url = SONG_BY_HASH_URL_BASE + song.hashMd5;
                searchMethod = SongGetMethod.Hash;
            }
            else
                return false;
            Task<string> pageReadTask;
            //lock (lockObject)
            pageReadTask = WebUtils.httpClient.GetStringAsync(url);
            pageReadTask.Wait();
            string pageText = pageReadTask.Result;
            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (Exception ex)
            {
                successful = false;
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            if (searchMethod == SongGetMethod.SongIndex)
                JsonConvert.PopulateObject(result["song"].ToString(), song);
            else if (searchMethod == SongGetMethod.Hash)
                JsonConvert.PopulateObject(result["songs"].First.ToString(), song);

            song.Populated = successful;
            return successful;
        }

        public bool PopulateFields()
        {
            return SongInfoEnhanced.PopulateFields(this);
        }

        enum SongGetMethod
        {
            SongIndex,
            Hash
        }

        /// <summary>
        /// Downloads the page and returns it as a string in an asynchronous operation.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<bool> PopulateFieldsAsync()
        {
            if (Populated)
                return true;
            SongGetMethod searchMethod;
            string url;
            if (!string.IsNullOrEmpty(key))
            {
                url = SONG_DETAILS_URL_BASE + key;
                searchMethod = SongGetMethod.SongIndex;
            }
            else if (!string.IsNullOrEmpty(hashMd5))
            {
                url = SONG_BY_HASH_URL_BASE + hashMd5;
                searchMethod = SongGetMethod.Hash;
            }
            else
                return false;
            Logger.Debug($"Starting PopulateFieldsAsync for {key}");
            bool successful = true;

            string pageText = "";
            try
            {
                pageText = await WebUtils.httpClient.GetStringAsync(url);
            }
            catch (TaskCanceledException)
            {
                Logger.Error($"Timeout occurred while trying to populate fields for {key}");
                return false;
            }
            catch (HttpRequestException)
            {
                Logger.Error($"HttpRequestException while trying to populate fields for {key}");
                return false;
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
            lock (this)
            {
                if (searchMethod == SongGetMethod.SongIndex)
                    JsonConvert.PopulateObject(result["song"].ToString(), this);
                else if (searchMethod == SongGetMethod.Hash)
                    JsonConvert.PopulateObject(result["songs"].First.ToString(), this);
            }
            Logger.Debug($"Finished PopulateFieldsAsync for {key}");
            Populated = successful;
            return successful;
        }

        public SongInfoEnhanced() { }

        public SongInfoEnhanced(string songIndex, string songName, string songUrl, string _authorName, string feedName = "")
        {
            key = songIndex;
            name = songName;
            authorName = _authorName;
            downloadUrl = songUrl;
            Feed = feedName;
        }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            //if (!(this is ScoreSaberSong))
            //if(!this.GetType().IsSubclassOf(typeof(SongInfo)))
            //{
            //    //Logger.Warning("SongInfo OnDeserialized");
            Populated = true;
            /*
            if (_songInfo == null)
            {
                if (_beatSaverRegex.IsMatch(key))
                    _songInfo = ScrapedDataProvider.GetSongByKey(key, false);
                if (_songInfo == null)
                    _songInfo = ScrapedDataProvider.GetSongByHash(hashMd5, false);

                if (_songInfo == null)
                {
                    //Logger.Info($"Couldn't find song {key} - {name} by {authorName}, generating new song info...");
                    _songInfo = new SongInfo() {
                        key = key,
                        songName = songName,
                        songSubName = songSubName,
                        authorName = authorName,
                        bpm = bpm,
                        playedCount = playedCount,
                        upVotes = upVotes,
                        downVotes = downVotes,
                        hash = hashMd5,
                    };
                    _songInfo.EnhancedInfo = this;
                    ScrapedDataProvider.TryAddToScrapedData(_songInfo);
                }
            }
            */
        }



        public static bool TryParseBeatSaver(JToken token, out SongInfoEnhanced song)
        {
            string songIndex = token["key"]?.Value<string>();
            if (songIndex == null)
                songIndex = "";
            bool successful = true;
            SongInfoEnhanced enhancedSong;
            try
            {
                enhancedSong = token.ToObject<SongInfoEnhanced>(new JsonSerializer() {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

                song = enhancedSong;
                //song.EnhancedInfo = enhancedSong;
                //Logger.Debug(song.ToString());
            }
            catch (Exception ex)
            {
                Logger.Exception($"Unable to create a SongInfo from the JSON for {songIndex}\n", ex);
                successful = false;
                song = null;
            }
            return successful;
        }
        /*
        public SongInfo ToSongInfo()
        {
            if (!Populated)
            {
                Logger.Warning("Trying to create SongInfo from an unpopulated SongInfoEnhanced");
                return null;
            }
            if (_songInfo == null)
            {
                if (_beatSaverRegex.IsMatch(key))
                    _songInfo = ScrapedDataProvider.GetSongByKey(key, false);
                if (_songInfo == null)
                    _songInfo = ScrapedDataProvider.GetSongByHash(hashMd5, false);

                if (_songInfo == null)
                {
                    Logger.Info($"Couldn't find song {key} - {name} by {authorName}, generating new song info...");
                    _songInfo = new SongInfo() {
                        key = key,
                        songName = songName,
                        songSubName = songSubName,
                        authorName = authorName,
                        bpm = bpm,
                        playedCount = playedCount,
                        upVotes = upVotes,
                        downVotes = downVotes,
                        hash = hashMd5,
                    };
                    _songInfo.EnhancedInfo = this;
                    ScrapedDataProvider.TryAddToScrapedData(_songInfo);
                    return _songInfo;
                }
                _songInfo.EnhancedInfo = this;
            }
            return _songInfo;
        }
        */

        public SongInfo GenerateSongInfo()
        {
            var newSong = new SongInfo() {
                key = key,
                songName = songName,
                songSubName = songSubName,
                authorName = authorName,
                bpm = bpm,
                playedCount = playedCount,
                upVotes = upVotes,
                downVotes = downVotes,
                hash = hashMd5,
            };
            //newSong.EnhancedInfo = this;
            return newSong;
        }

        //[JsonIgnore]
        //private SongInfo _songInfo { get; set; }
        [JsonIgnore]
        public string Feed;
        [JsonIgnore]
        public int SongVersion
        {
            get
            {
                bool success = int.TryParse(key.Substring(key.IndexOf('-') + 1), out int result);
                return success ? result : 0;
            }
        }
        [JsonProperty("id")]
        private int _id;
        [JsonIgnore]
        public int id
        {
            get
            {
                if (!(_id > 0))
                    if (key != null && _beatSaverRegex.IsMatch(key))
                        _id = int.Parse(key.Substring(0, key.IndexOf('-')));
                return _id;
            }
            set { _id = value; }
        }

        [JsonProperty("key")]
        public string key; // in Scraped
        [JsonProperty("name")]
        public string name;
        [JsonProperty("description")]
        public string description;
        [JsonProperty("uploader")]
        public string uploader;
        [JsonProperty("uploaderId")]
        public int uploaderId;
        [JsonProperty("songName")]
        public string songName; // in Scraped
        [JsonProperty("songSubName")]
        public string songSubName; // in Scraped
        [JsonProperty("authorName")]
        public string authorName; // in Scraped
        [JsonProperty("bpm")]
        public float bpm; // in Scraped
        [JsonProperty("difficulties")]
        public Dictionary<string, BeatSaverSongDifficulty> difficulties;
        [JsonProperty("downloadCount")]
        public int downloadCount;
        [JsonProperty("playedCount")]
        public int playedCount; // in Scraped
        [JsonProperty("upVotes")]
        public int upVotes; // in Scraped
        [JsonProperty("upVotesTotal")]
        public int upVotesTotal;
        [JsonProperty("downVotes")]
        public int downVotes; // in Scraped
        [JsonProperty("downVotesTotal")]
        public int downVotesTotal;
        [JsonProperty("rating")]
        public float rating;
        [JsonProperty("version")]
        public string version;
        [JsonProperty("createdAt")]
        public CreationTime createdAt;
        [JsonProperty("linkUrl")]
        public string linkUrl;
        [JsonProperty("downloadUrl")]
        public string downloadUrl;
        [JsonProperty("coverUrl")]
        public string coverUrl;
        [JsonProperty("hashMd5")]
        public string hashMd5;
        [JsonProperty("hashSha1")]
        public string hashSha1;

        public override string ToString()
        {
            StringBuilder retStr = new StringBuilder();
            retStr.Append("SongInfo:");
            retStr.AppendLine("   Index: " + key);
            retStr.AppendLine("   Name: " + name);
            retStr.AppendLine("   Author: " + authorName);
            retStr.AppendLine("   URL: " + downloadUrl);
            retStr.AppendLine("   Feed: " + Feed);
            return retStr.ToString();
        }
    }

    public class BeatSaverSongDifficulty
    {
        [JsonProperty("difficulty")]
        public string difficulty;
        [JsonProperty("rank")]
        public int rank;
        [JsonProperty("audioPath")]
        public string audioPath;
        [JsonProperty("jsonPath")]
        public string jsonPath;
        [JsonProperty("stats")]
        BeatSaverSongDifficultyStats stats { get; set; }
    }

    public class BeatSaverSongDifficultyStats
    {
        [JsonProperty("time")]
        public double time;
        [JsonProperty("slashstat")]
        [JsonConverter(typeof(EmptyArrayOrDictionaryConverter))]
        public Dictionary<string, int> slashstat;
        [JsonProperty("events")]
        public int events;
        [JsonProperty("notes")]
        public int notes;
        [JsonProperty("obstacles")]
        public int obstacles;
    }

    public struct CreationTime
    {
        [JsonProperty("date")]
        public DateTime date;
        [JsonProperty("timezone_type")]
        public int timezone_type;
        [JsonProperty("timezone")]
        public string timezone;
    }
    

    public static class SongInfoEnhancedExtensions
    {
        public static void PopulateFromBeatSaver(this IEnumerable<SongInfoEnhanced> songs)
        {
            List<Task> populateTasks = new List<Task>();
            for (int i = 0; i < songs.Count(); i++)
            {
                if (!songs.ElementAt(i).Populated)
                    populateTasks.Add(songs.ElementAt(i).PopulateFieldsAsync());
            }

            Task.WaitAll(populateTasks.ToArray());
        }

        public static async Task PopulateFromBeatSaverAsync(this IEnumerable<SongInfoEnhanced> songs)
        {
            List<Task> populateTasks = new List<Task>();
            for (int i = 0; i < songs.Count(); i++)
            {
                if (!songs.ElementAt(i).Populated)
                    populateTasks.Add(songs.ElementAt(i).PopulateFieldsAsync());
            }

            await Task.WhenAll(populateTasks);
            Logger.Warning("Finished PopulateAsync?");
        }
    }

}

