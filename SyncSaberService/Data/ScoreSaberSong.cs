using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SyncSaberService.Web;

namespace SyncSaberService.Data
{
    class ScoreSaberSong
    {
        [JsonProperty("uid")]
        public int uid;
        [JsonProperty("id")]
        public string md5Hash;
        [JsonProperty("name")]
        public string name;
        [JsonProperty("songSubName")]
        public string songSubName;
        [JsonProperty("author")]
        public string author;
        [JsonProperty("bpm")]
        public float bpm;
        [JsonProperty("diff")]
        public string difficulty;
        [JsonProperty("scores")]
        public string scores;
        [JsonProperty("24hr")]
        public int hr24;
        [JsonProperty("ranked")]
        public bool ranked;
        [JsonProperty("stars")]
        public float stars;
        [JsonProperty("image")]
        public string image;

        [JsonIgnore]
        public SongInfo song;

        public SongInfo GetSongInfo()
        {
            throw new NotImplementedException();
        }
    }
}

/// 198	
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
