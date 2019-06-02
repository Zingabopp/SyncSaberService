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
    public class SongInfo
    {
        // Link: https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/combinedScrappedData.json
        private readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        private readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);
        private const string DOWNLOAD_URL_BASE = "http://beatsaver.com/download/";

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
        [JsonProperty("Diffs")]
        public List<ScrappedDifficulties> diffs { get; set; }
        [JsonProperty("playedCount")]
        public int playedCount { get; set; }
        [JsonProperty("upVotes")]
        public int upVotes { get; set; }
        [JsonProperty("downVotes")]
        public int downVotes { get; set; }
        [JsonProperty("hash")]
        public string hash { get; set; }
        #endregion
        /*
        #region SongInfoEnhanced Data
        [JsonProperty("name")]
        public string name { get { return EnhancedInfo.name; } set { EnhancedInfo.name = value; } }
        [JsonProperty("description")]
        public string description { get { return EnhancedInfo.description; } set { EnhancedInfo.description = value; } }
        [JsonProperty("uploader")]
        public string uploader { get { return EnhancedInfo.uploader; } set { EnhancedInfo.uploader = value; } }
        [JsonProperty("uploaderId")]
        public int uploaderId { get { return EnhancedInfo.uploaderId; } set { EnhancedInfo.uploaderId = value; } }
        [JsonProperty("difficulties")]
        public Dictionary<string, BeatSaverSongDifficulty> difficulties { get { return EnhancedInfo.difficulties; } set { EnhancedInfo.difficulties = value; } }
        [JsonProperty("downloadCount")]
        public int downloadCount { get { return EnhancedInfo.downloadCount; } set { EnhancedInfo.downloadCount = value; } }
        [JsonProperty("upVotesTotal")]
        public int upVotesTotal { get { return EnhancedInfo.upVotesTotal; } set { EnhancedInfo.upVotesTotal = value; } }
        [JsonProperty("downVotesTotal")]
        public int downVotesTotal { get { return EnhancedInfo.downVotesTotal; } set { EnhancedInfo.downVotesTotal = value; } }
        [JsonProperty("rating")]
        public float rating { get { return EnhancedInfo.rating; } set { EnhancedInfo.rating = value; } }
        [JsonProperty("version")]
        public string version { get { return EnhancedInfo.version; } set { EnhancedInfo.version = value; } }
        [JsonProperty("createdAt")]
        public CreationTime createdAt { get { return EnhancedInfo.createdAt; } set { EnhancedInfo.createdAt = value; } }
        [JsonProperty("linkUrl")]
        public string linkUrl { get { return EnhancedInfo.linkUrl; } set { EnhancedInfo.linkUrl = value; } }
        [JsonProperty("downloadUrl")]
        public string downloadUrl { get { return EnhancedInfo.downloadUrl; } set { EnhancedInfo.downloadUrl = value; } }
        [JsonProperty("coverUrl")]
        public string coverUrl { get { return EnhancedInfo.coverUrl; } set { EnhancedInfo.coverUrl = value; } }
        [JsonProperty("hashMd5")]
        public string hashMd5 { get { return EnhancedInfo.hashMd5; } set { EnhancedInfo.hashMd5 = value; } }
        [JsonProperty("hashSha1")]
        public string hashSha1 { get { return EnhancedInfo.hashSha1; } set { EnhancedInfo.hashSha1 = value; } }

        #endregion

        #region ScoreSaberInfo
        [JsonProperty("uid")]
        public string uid { get { return ScoreSaberInfo.uid; } set { ScoreSaberInfo.uid = value; } }
        [JsonProperty("id")]
        public string md5Hash { get { return ScoreSaberInfo.md5Hash; } set { ScoreSaberInfo.md5Hash = value; } }
        [JsonProperty("name")]
        public string ssName { get { return ScoreSaberInfo.name; } set { ScoreSaberInfo.name = value; } }
        [JsonProperty("songSubName")]
        public string ssSubName { get { return ScoreSaberInfo.songSubName; } set { ScoreSaberInfo.songSubName = value; } }
        [JsonProperty("author")]
        public string author { get { return ScoreSaberInfo.author; } set { ScoreSaberInfo.author = value; } }
        //[JsonProperty("bpm")]
        //public float bpm { get { return ScoreSaberInfo.bpm; } set { ScoreSaberInfo.bpm = value; } }
        [JsonProperty("diff")]
        public string difficulty { get { return ScoreSaberInfo.difficulty; } set { ScoreSaberInfo.difficulty = value; } }
        [JsonProperty("scores")]
        public string scores { get { return ScoreSaberInfo.scores; } set { ScoreSaberInfo.scores = value; } }
        [JsonProperty("24hr")]
        public int hr24 { get { return ScoreSaberInfo.hr24; } set { ScoreSaberInfo.hr24 = value; } }
        [JsonProperty("ranked")]
        public bool ranked { get { return ScoreSaberInfo.ranked; } set { ScoreSaberInfo.ranked = value; } }
        [JsonProperty("stars")]
        public float stars { get { return ScoreSaberInfo.stars; } set { ScoreSaberInfo.stars = value; } }
        [JsonProperty("image")]
        public string image { get { return ScoreSaberInfo.image; } set { ScoreSaberInfo.image = value; } }
        #endregion
    */
        public struct ScrappedDifficulties
        {
            [JsonProperty("Diff")]
            public string difficulty;
            [JsonProperty("scores")]
            public int scores;
            [JsonProperty("Stars")]
            public float stars;
        }

        private SongInfoEnhanced _enhancedSongInfo;
        public SongInfoEnhanced EnhancedInfo
        {
            get
            {
                if (_enhancedSongInfo == null)
                    _enhancedSongInfo = new SongInfoEnhanced();
                return _enhancedSongInfo;
            }
        }

        private ScoreSaberSong _scoreSaberInfo;
        public ScoreSaberSong ScoreSaberInfo
        {
            get
            {
                if (_scoreSaberInfo == null)
                    _scoreSaberInfo = new ScoreSaberSong();
                return _scoreSaberInfo;
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
    }



}
