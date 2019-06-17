using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncSaberLib
{
    [Serializable]
    public class PlaylistSong
    {
        public PlaylistSong(string _hash, string _songIndex, string _songName)
        {
            hash = _hash;
            key = _songIndex;
            songName = _songName;
        }

        [JsonProperty("key")]
        public string key;

        [JsonProperty("hash")]
        public string hash;

        [JsonProperty("songName")]
        public string songName;
    }
}
