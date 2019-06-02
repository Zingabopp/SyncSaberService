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
                if (_id <= 0 && _beatSaverRegex.IsMatch(key))
                    try { _id = int.Parse(key.Substring(key.IndexOf('-'))); }
                    catch (Exception) { }
                return _id;
            }
        }

        [JsonIgnore]
        private string _key;
        [JsonProperty("key")]
        public string key
        {
            get { return _key; }
            set
            {
                if (string.IsNullOrEmpty(url) && _beatSaverRegex.IsMatch(value))
                    url = DOWNLOAD_URL_BASE + value;
                _key = value;
            }
        }
        [JsonProperty("songName")]
        public string songName;
        [JsonProperty("songSubName")]
        public string songSubName;
        [JsonProperty("authorName")]
        public string authorName;
        [JsonProperty("bpm")]
        public float bpm;
        [JsonProperty("Diffs")]
        public List<ScrappedDifficulties> difficulties;
        [JsonProperty("playedCount")]
        public int playedCount;
        [JsonProperty("upVotes")]
        public int upVotes;
        [JsonProperty("downVotes")]
        public int downVotes;
        [JsonProperty("hash")]
        public string hashMd5;

        [JsonIgnore]
        public string url;

        public struct ScrappedDifficulties
        {
            [JsonProperty("Diff")]
            public string difficulty;
            [JsonProperty("scores")]
            public int scores;
            [JsonProperty("Stars")]
            public float stars;

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
