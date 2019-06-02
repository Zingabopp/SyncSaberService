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

namespace SyncSaberService.Data
{
    /// <summary>
    /// TODO: Make Scrapped song the base with separate objects (using Lazy<>) for Beat Saver and Score Saber data.
    /// </summary>
    public class SongInfoEnhanced
    {
        private static readonly string SONG_DETAILS_URL_BASE = "https://beatsaver.com/api/songs/detail/";
        private static readonly string SONG_BY_HASH_URL_BASE = "https://beatsaver.com/api/songs/search/hash/";
        private static object lockObject = new object();
        private static HttpClientHandler _httpClientHandler;
        public static HttpClientHandler httpClientHandler
        {
            get
            {
                if (_httpClientHandler == null)
                {
                    _httpClientHandler = new HttpClientHandler();
                    httpClientHandler.MaxConnectionsPerServer = 10;
                    httpClientHandler.UseCookies = true;
                }
                return _httpClientHandler;
            }
        }
        private static HttpClient _httpClient;
        public static HttpClient httpClient
        {
            get
            {
                lock (lockObject)
                {
                    if (_httpClient == null)
                    {
                        _httpClient = new HttpClient(httpClientHandler);
                        lock (_httpClient)
                        {
                            _httpClient.Timeout = new TimeSpan(0, 0, 10);
                        }
                    }
                }
                return _httpClient;
            }
        }
        private bool _songInfoPopulated = false;
        public bool SongInfoPopulated { get { return _songInfoPopulated; } }
        private readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        private readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);

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
            if (song.SongInfoPopulated)
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
            pageReadTask = httpClient.GetStringAsync(url);
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

            song._songInfoPopulated = successful;
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
            if (SongInfoPopulated)
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
                pageText = await httpClient.GetStringAsync(url);
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
            _songInfoPopulated = successful;
            return successful;
        }


        public SongInfoEnhanced()
        {

        }

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
            if(!this.GetType().IsSubclassOf(typeof(SongInfo)))
            {
                //Logger.Warning("SongInfo OnDeserialized");
                _songInfoPopulated = true;
            }
        }

        public static bool TryParseBeatSaver(JToken token, out SongInfoEnhanced song)
        {
            string songIndex = token["key"]?.Value<string>();
            if (songIndex == null)
                songIndex = "";
            bool successful = true;
            try
            {
                song = token.ToObject<SongInfoEnhanced>(new JsonSerializer() {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
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
        public string key;
        [JsonProperty("name")]
        public string name;
        [JsonProperty("description")]
        public string description;
        [JsonProperty("uploader")]
        public string uploader;
        [JsonProperty("uploaderId")]
        public int uploaderId;
        [JsonProperty("songName")]
        public string songName;
        [JsonProperty("songSubName")]
        public string songSubName;
        [JsonProperty("authorName")]
        public string authorName;
        [JsonProperty("bpm")]
        public float bpm;
        [JsonProperty("difficulties")]
        public Dictionary<string, BeatSaverSongDifficulty> difficulties;
        [JsonProperty("downloadCount")]
        public int downloadCount;
        [JsonProperty("playedCount")]
        public int playedCount;
        [JsonProperty("upVotes")]
        public int upVotes;
        [JsonProperty("upVotesTotal")]
        public int upVotesTotal;
        [JsonProperty("downVotes")]
        public int downVotes;
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

    public class EmptyArrayOrDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<string, object>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Object)
            {
                return token.ToObject(objectType);
            }
            else if (token.Type == JTokenType.Array)
            {
                if (!token.HasValues)
                {
                    // create empty dictionary
                    return Activator.CreateInstance(objectType);
                }
                // Handles case where Beat Saver gives the slashstat in the form of an array.
                if (objectType == typeof(Dictionary<string, int>))
                {
                    var retDict = new Dictionary<string, int>();
                    for (int i = 0; i < token.Count(); i++)
                    {
                        retDict.Add(i.ToString(), (int) token.ElementAt(i));
                    }
                    return retDict;
                }
            }
            //throw new JsonSerializationException($"{objectType.ToString()} or empty array expected, received a {token.Type.ToString()}");
            Logger.Warning($"{objectType.ToString()} or empty array expected, received a {token.Type.ToString()}");
            return Activator.CreateInstance(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public static class SongInfoEnhancedExtensions
    {
        public static void PopulateFromBeatSaver(this IEnumerable<SongInfoEnhanced> songs)
        {
            List<Task> populateTasks = new List<Task>();
            for (int i = 0; i < songs.Count(); i++)
            {
                if (!songs.ElementAt(i).SongInfoPopulated)
                    populateTasks.Add(songs.ElementAt(i).PopulateFieldsAsync());
            }

            Task.WaitAll(populateTasks.ToArray());
        }

        public static async Task PopulateFromBeatSaverAsync(this IEnumerable<SongInfoEnhanced> songs)
        {
            List<Task> populateTasks = new List<Task>();
            for (int i = 0; i < songs.Count(); i++)
            {
                if (!songs.ElementAt(i).SongInfoPopulated)
                    populateTasks.Add(songs.ElementAt(i).PopulateFieldsAsync());
            }

            await Task.WhenAll(populateTasks);
            Logger.Warning("Finished PopulateAsync?");
        }
    }

}

