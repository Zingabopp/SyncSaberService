using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;

namespace SyncSaberLib.Data
{
    public class ScoreSaberSong : IEquatable<ScoreSaberSong>
    {
        [JsonIgnore]
        public bool Populated { get; private set; }

        public ScoreSaberSong()
        {

        }

        public static bool TryParseScoreSaberSong(JToken token, ref ScoreSaberSong song)
        {
            string songName = token["name"]?.Value<string>();
            if (songName == null)
                songName = "";
            bool successful = true;
            try
            {
                song = token.ToObject<ScoreSaberSong>(new JsonSerializer() {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
                //Logger.Debug(song.ToString());
            }
            catch (Exception ex)
            {
                Logger.Exception($"Unable to create a ScoreSaberSong from the JSON for {songName}\n", ex);
                successful = false;
                song = null;
            }
            return successful;
        }
        [JsonProperty("uid")]
        public int uid { get; set; }
        [JsonProperty("id")]
        public string hash { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("songSubName")]
        public string songSubName { get; set; }
        [JsonProperty("songAuthorName")]
        public string songAuthorName { get; set; }
        [JsonProperty("levelAuthorName")]
        public string levelAuthorName { get; set; }

        [JsonProperty("bpm")]
        public float bpm { get; set; }
        
        [JsonProperty("diff")]
        private string diff { get; set; }
        [JsonIgnore]
        public string difficulty
        {
            get { return ConvertDiff(diff); }
        }
        [JsonProperty("scores")]
        [JsonConverter(typeof(IntegerWithCommasConverter))]
        public int scores { get; set; }
        [JsonProperty("scores_day")]
        public int scores_day { get; set; }
        [JsonProperty("ranked")]
        public bool ranked { get; set; }
        [JsonProperty("stars")]
        public float stars { get; set; }
        [JsonProperty("image")]
        public string image { get; set; }
        [JsonProperty("ScrapedAt")]
        public DateTime ScrapedAt { get; set; }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            //if (!(this is ScoreSaberSong))
            //if (!this.GetType().IsSubclassOf(typeof(SongInfo)))
            //{
            //Logger.Warning("SongInfo OnDeserialized");
            Populated = true;
            //var song = ScrapedDataProvider.SyncSaberScrape.Where(s => s.hash == md5Hash).FirstOrDefault();
            //if (song != null)
            //    if (song.ScoreSaberInfo.AddOrUpdate(uid, this))
            //        Logger.Warning($"Adding the same ScoreSaberInfo {uid}-{difficulty} to song {name}");
        }

        public SongInfo GenerateSongInfo()
        {
            var newSong = new SongInfo(hash);
            /*
            var newSong = new SongInfo() {
                songName = name,
                songSubName = songSubName,
                authorName = levelAuthorName,
                bpm = bpm
            };
            */
            //newSong.ScoreSaberInfo.Add(uid, this);
            return newSong;
        }

        private const string EASYKEY = "_easy_solostandard";
        private const string NORMALKEY = "_normal_solostandard";
        private const string HARDKEY = "_hard_solostandard";
        private const string EXPERTKEY = "_expert_solostandard";
        private const string EXPERTPLUSKEY = "_expertplus_solostandard";
        public static string ConvertDiff(string diffString)
        {
            diffString = diffString.ToLower();
            if (!diffString.Contains("solostandard"))
                return diffString;
            switch (diffString)
            {
                case EXPERTPLUSKEY:
                    return "ExpertPlus";
                case EXPERTKEY:
                    return "Expert";
                case HARDKEY:
                    return "Hard";
                case NORMALKEY:
                    return "Normal";
                case EASYKEY:
                    return "Easy";
                default:
                    return diffString;
            }
        }

        public bool Equals(ScoreSaberSong other)
        {
            return uid == other.uid;
        }
    }
}


//uid	8497
//id	"44C9544A577E5B8DC3876F9F696A7F92"
//name	"Redo"
//songSubName	"Suzuki Konomi"
//author	"Splake"
//bpm	190
//diff	"_Expert_SoloStandard"
//scores	"1,702"
//24hr	8
//ranked	1
//stars	3.03
//image	"/imports/images/songs/44C9544A577E5B8DC3876F9F696A7F92.png"
