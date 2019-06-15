using System;
using System.Text.RegularExpressions;
using System.Collections;
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

namespace SyncSaberLib.Data
{
    public class SongInfo
    {
        // Link: https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/combinedScrappedData.json
        private static readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        private static readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);
        public const char IDENTIFIER_DELIMITER = (char) 0x220E;
        private const string DOWNLOAD_URL_BASE = "http://beatsaver.com/download/";

        [JsonIgnore]
        public bool Populated { get; private set; }

        [JsonIgnore]
        private int _id = 0;
        [JsonIgnore]
        public int id
        {
            get
            {
                if (_id <= 0 && _beatSaverRegex.IsMatch(key))
                    _id = int.Parse(key.Substring(0, key.IndexOf('-')));
                return _id;
            }
        }

        private int _version = 0;
        [JsonIgnore]
        public int SongVersion
        {
            get
            {
                if (_version <= 0 && _beatSaverRegex.IsMatch(key))
                    try { _version = int.Parse(key.Substring(key.IndexOf('-') + 1)); }
                    catch (Exception) { }
                return _version;
            }
        }

        [JsonIgnore]
        public string url;
        [JsonIgnore]
        private string _key;

        

        #region Scraped Data
        [JsonProperty("key")]
        public string key
        {
            get { return _key; }
            set
            {
                if (string.IsNullOrEmpty(url) && _beatSaverRegex.IsMatch(value))
                {
                    url = DOWNLOAD_URL_BASE + value;
                    _id = int.Parse(value.Substring(0, value.IndexOf('-')));
                    try { _version = int.Parse(value.Substring(value.IndexOf('-') + 1)); }
                    catch (Exception) { }
                }
                _key = value;
            }
        }
        [JsonProperty("songName")]
        public string songName { get; set; }
        [JsonProperty("songSubName")]
        public string songSubName { get; set; }
        [JsonProperty("authorName")]
        public string authorName { get; set; }
        [JsonProperty("bpm")]
        public float bpm { get; set; }
        [JsonProperty("playedCount")]
        public int playedCount { get; set; }
        [JsonProperty("upVotes")]
        public int upVotes { get; set; }
        [JsonProperty("downVotes")]
        public int downVotes { get; set; }
        [JsonProperty("hash")]
        public string hash { get; set; }


        #endregion
        [JsonIgnore]
        private Dictionary<string, float> _rankedDiffs;
        [JsonIgnore]
        public Dictionary<string, float> RankedDifficulties
        {
            get
            {
                if (_rankedDiffs == null)
                    _rankedDiffs = new Dictionary<string, float>();
                if (ScoreSaberInfo.Count != _rankedDiffs.Count) // If they don't have the same number of difficulties, remake
                {
                    _rankedDiffs = new Dictionary<string, float>();
                    foreach (var key in ScoreSaberInfo.Keys)
                    {
                        if (ScoreSaberInfo[key].ranked)
                        {
                            if (hash.ToUpper() == ScoreSaberInfo[key].md5Hash.ToUpper())
                                _rankedDiffs.AddOrUpdate(ScoreSaberInfo[key].difficulty, ScoreSaberInfo[key].stars);
                            else
                                Logger.Debug($"Ranked version of {key} - {songName} is outdated.\n" +
                                    $"   {hash.ToUpper()} != {ScoreSaberInfo[key].md5Hash.ToUpper()}");
                        }
                    }
                }
                return _rankedDiffs;
            }
        }

        [JsonIgnore]
        private SongInfoEnhanced _enhancedSongInfo;

        public SongInfoEnhanced EnhancedInfo
        {
            get
            {
                return _enhancedSongInfo;
            }
            set { _enhancedSongInfo = value; }
        }

        [JsonIgnore]
        private Dictionary<int, ScoreSaberSong> _scoreSaberInfo;
        public Dictionary<int, ScoreSaberSong> ScoreSaberInfo
        {
            get
            {
                if (_scoreSaberInfo == null)
                    _scoreSaberInfo = new Dictionary<int, ScoreSaberSong>();
                return _scoreSaberInfo;
            }
            set { _scoreSaberInfo = value; }
        }

        [JsonIgnore]
        private string _identifier;
        [JsonIgnore]
        public string Identifier
        {
            get
            {
                if (string.IsNullOrEmpty(_identifier))
                {
                    if (string.IsNullOrEmpty(hash))
                        return string.Empty;
                    if (string.IsNullOrEmpty(songName))
                        return string.Empty;
                    if (string.IsNullOrEmpty(songSubName))
                        return string.Empty;
                    if (string.IsNullOrEmpty(authorName))
                        return string.Empty;
                    if (bpm <= 0)
                        return string.Empty;
                    _identifier = string.Join(IDENTIFIER_DELIMITER.ToString(), new string[] {
                        hash,
                        songName,
                        songSubName,
                        authorName,
                        bpm.ToString()
                    });
                }
                return _identifier;
            }
        }

        public SongInfo() { }
        public SongInfo(string _key, string _songName, string _downloadUrl, string _author)
        {
            key = _key;
            songName = _songName;
            url = _downloadUrl;
            authorName = _author;
        }

        public void PopulateFields()
        {

        }
        /*
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
        */
        public override string ToString()
        {
            StringBuilder retStr = new StringBuilder();
            retStr.Append("SongInfo:");
            retStr.AppendLine("   Index: " + key);
            retStr.AppendLine("   Name: " + songName);
            retStr.AppendLine("   Author: " + authorName);
            return retStr.ToString();
        }

        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(SongInfo);
                object retVal;
                FieldInfo field = myType.GetField(propertyName);
                if (field != null)
                {
                    retVal = field.GetValue(this);
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
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {

        }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            //if (!(this is ScoreSaberSong))
            if (!this.GetType().IsSubclassOf(typeof(SongInfo)))
            {
                //Logger.Warning("SongInfo OnDeserialized");
                Populated = true;
            }
        }
    }



}
