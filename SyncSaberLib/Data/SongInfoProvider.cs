using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberLib.Data
{
    public static class SongInfoProvider
    {
        public static BeatSaverScrape BeatSaverSongs { get; set; }
        public static ScoreSaberScrape ScoreSaberSongs { get; set; }

        public static void Initialize()
        {

        }
    }
}
