using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberService
{
    public class PlaylistSong
    {
        public PlaylistSong(string songIndex, string songName)
        {
            this.key = songIndex;
            this.songName = songName;
        }

        public string key;

        public string songName;
    }
}
