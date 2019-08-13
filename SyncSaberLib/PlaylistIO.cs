using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using SimpleJSON;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace SyncSaberLib
{
    class PlaylistIO
    {
        public static Playlist ReadPlaylistSongs(Playlist playlist)
        {
            try
            {
                string filePath = Path.Combine(OldConfig.BeatSaberPath, "Playlists", playlist.fileName + (playlist.oldFormat ? ".json" : ".bplist"));
                //var playListJson = JObject.Parse(File.ReadAllText(filePath));
                JsonConvert.PopulateObject(File.ReadAllText(filePath), playlist);
                playlist.fileLoc = null;

                return playlist;
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception parsing playlist:", ex);
            }
            return null;
        }

        public static void WritePlaylist(Playlist playlist)
        {

            if (!Directory.Exists(Path.Combine(OldConfig.BeatSaberPath, "Playlists")))
            {
                Directory.CreateDirectory(Path.Combine(OldConfig.BeatSaberPath, "Playlists"));
            }
            var jsonString = JsonConvert.SerializeObject(playlist);
            File.WriteAllText(Path.Combine(OldConfig.BeatSaberPath, "Playlists", playlist.fileName + (playlist.oldFormat ? ".json" : ".bplist")), jsonString);
        }
    }
}
