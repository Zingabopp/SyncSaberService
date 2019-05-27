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
using System.Runtime.Serialization;

namespace SyncSaberService
{
    public class SongInfo
    {
        private static readonly string SONG_DETAILS_URL_BASE = "https://beatsaver.com/api/songs/detail/";
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
        private bool _populated = false;
        public bool Populated { get { return _populated; } }

        /// <summary>
        /// Downloads the page and returns it as a string.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public static bool PopulateFields(SongInfo song)
        {
            if (string.IsNullOrEmpty(song.key))
                return false;
            Task<string> pageReadTask;
            //lock (lockObject)
            pageReadTask = httpClient.GetStringAsync(SONG_DETAILS_URL_BASE + song.key);
            pageReadTask.Wait();
            string pageText = pageReadTask.Result;
            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (Exception ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            JsonConvert.PopulateObject(result["song"].ToString(), song);
           
            return true;
        }

        /// <summary>
        /// Downloads the page and returns it as a string in an asynchronous operation.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<bool> PopulateFieldsAsync()
        {
            if (string.IsNullOrEmpty(key))
                return false;
            Logger.Debug($"Starting PopulateFieldsAsync for {key}");
            bool successful = true;
            string url = SONG_DETAILS_URL_BASE + key;
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
            catch(HttpRequestException)
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
                JsonConvert.PopulateObject(result["song"].ToString(), this);
            }
            Logger.Debug($"Finished PopulateFieldsAsync for {key}");
            return successful;
        }

        public SongInfo(string songIndex, string songName, string songUrl, string _authorName, string feedName = "")
        {
            key = songIndex;
            name = songName;
            authorName = _authorName;
            downloadUrl = songUrl;
            Feed = feedName;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _populated = true;
        }

        public static bool TryParseBeatSaver(JToken token, out SongInfo song)
        {
            string songIndex = token["key"]?.Value<string>();
            if (songIndex == null)
                songIndex = "";
            bool successful = true;
            try
            {
                song = token.ToObject<SongInfo>(new JsonSerializer() {
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
        public int id;
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
        BeatSaverSongDifficultyStats stats;
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
            }

            throw new JsonSerializationException("Object or empty array expected");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

}

