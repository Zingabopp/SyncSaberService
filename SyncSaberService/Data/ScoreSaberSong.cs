using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using SyncSaberService.Web;

namespace SyncSaberService.Data
{
    public class ScoreSaberSong
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
        public string md5Hash { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("songSubName")]
        public string songSubName { get; set; }
        [JsonProperty("author")]
        public string author { get; set; }
        [JsonProperty("bpm")]
        public float bpm { get; set; }
        [JsonIgnore]
        private string _diff;
        [JsonProperty("diff")]
        public string difficulty
        {
            get { return _diff; }
            set { _diff = ConvertDiff(value); }
        }
        [JsonProperty("scores")]
        [JsonConverter(typeof(IntegerWithCommasConverter))]
        public int scores { get; set; }
        [JsonProperty("24hr")]
        public int hr24 { get; set; }
        [JsonProperty("ranked")]
        public bool ranked { get; set; }
        [JsonProperty("stars")]
        public float stars { get; set; }
        [JsonProperty("image")]
        public string image { get; set; }
        /*
        public SongInfo ToSongInfo()
        {
            if (!Populated)
            {
                Logger.Warning("Trying to create SongInfo from an unpopulated ScoreSaberSong");
                return null;
            }
            if (Song == null)
                Song = ScrapedDataProvider.GetSongByHash(md5Hash, true);


            if (Song == null)
            {
                Logger.Info($"Couldn't find song {name} by {author}, generating new song info...");
                Song = new SongInfo() {
                    songName = name,
                    songSubName = songSubName,
                    authorName = author,
                    bpm = bpm,
                    hash = md5Hash
                };
            }
            int intUid = int.Parse(uid);
            if (!(Song.ScoreSaberInfo.ContainsKey(intUid) && Song.ScoreSaberInfo[intUid] != this))
            {
                Song.ScoreSaberInfo.AddOrUpdate(intUid, this);
            }
            return Song;
        }
        */
        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            //if (!(this is ScoreSaberSong))
            //if (!this.GetType().IsSubclassOf(typeof(SongInfo)))
            //{
            //Logger.Warning("SongInfo OnDeserialized");
            Populated = true;
            var song = ScrapedDataProvider.SyncSaberScrape.Where(s => s.hash == md5Hash).FirstOrDefault();
            if (song != null)
                if (song.ScoreSaberInfo.AddOrUpdate(uid, this))
                    Logger.Warning($"Adding the same ScoreSaberInfo {uid}-{difficulty} to song {name}");
        }

        public SongInfo GenerateSongInfo()
        {
            var newSong = new SongInfo() {
                songName = name,
                songSubName = songSubName,
                authorName = author,
                bpm = bpm
            };
            newSong.ScoreSaberInfo.Add(uid, this);
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

        /*
        public SongInfo GetSongInfo()
        {
            try
            {
                song = BeatSaverReader.Search(md5Hash, BeatSaverReader.SearchType.hash).FirstOrDefault();
            } catch (JsonException ex)
            {
                Logger.Exception("Error trying to get SongInfo from Beat Saver.", ex);
            }
            return song;
        }
        */
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
