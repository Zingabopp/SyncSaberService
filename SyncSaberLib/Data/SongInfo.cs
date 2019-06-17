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
    public class SongInfo : IEquatable<SongInfo>
    {
        // Link: https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/combinedScrappedData.json
        private static readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        private static readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);
        public const char IDENTIFIER_DELIMITER = (char) 0x220E;
        private const string DOWNLOAD_URL_BASE = "http://beatsaver.com/download/";


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
                            if (hash.ToUpper() == ScoreSaberInfo[key].hash.ToUpper())
                                _rankedDiffs.AddOrUpdate(ScoreSaberInfo[key].difficulty, ScoreSaberInfo[key].stars);
                            else
                                Logger.Debug($"Ranked version of {key} is outdated.\n" +
                                    $"   {hash.ToUpper()} != {ScoreSaberInfo[key].hash.ToUpper()}");
                        }
                    }
                }
                return _rankedDiffs;
            }
        }

        public BeatSaverSong BeatSaverInfo { get; set; }

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
        private string _hash;
        public string hash
        {
            get
            {
                if (string.IsNullOrEmpty(_hash))
                {
                    if (BeatSaverInfo != null)
                        _hash = BeatSaverInfo.hash.ToUpper();
                    else
                    {
                        var ssSong = ScoreSaberInfo.Values.FirstOrDefault();
                        if (ssSong != null)
                            _hash = ssSong.hash.ToUpper();
                    }
                }
                return _hash;
            }
        }

        public int keyAsInt
        {
            get
            {
                if (BeatSaverInfo != null)
                    return BeatSaverInfo.KeyAsInt;
                return 0;
            }
        }

        public string key
        {
            get
            {
                if (BeatSaverInfo != null)
                    return BeatSaverInfo.key;
                return string.Empty;
            }
        }

        public string songName
        {
            get
            {
                if (BeatSaverInfo != null)
                    return BeatSaverInfo.metadata.songName;
                var ssSong = ScoreSaberInfo.Values.FirstOrDefault();
                if (ssSong != null)
                    return ssSong.name;
                return string.Empty;
            }
        }

        public string authorName
        {
            get
            {
                if (BeatSaverInfo != null)
                    return BeatSaverInfo.uploader.username;
                var ssSong = ScoreSaberInfo.Values.FirstOrDefault();
                if (ssSong != null)
                    return ssSong.levelAuthorName;
                return string.Empty;
            }
        }

        public float bpm
        {
            get
            {
                if (BeatSaverInfo != null)
                    return BeatSaverInfo.metadata.bpm;
                var ssSong = ScoreSaberInfo.Values.FirstOrDefault();
                if (ssSong != null)
                    return ssSong.bpm;
                return 0;
            }
        }


        /*
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
        */
        //public SongInfo() { }
        public SongInfo(string hash)
        {
            _hash = hash.ToUpper();
        }

        public override string ToString()
        {
            return hash;
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

        public bool Equals(SongInfo other)
        {
            return hash.ToUpper() == other.hash.ToUpper();
        }
    }



}
