using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleJSON;
using System.IO;


namespace SyncSaberLib
{
    class PlaylistIO
    {
        public static Playlist ReadPlaylistSongs(Playlist playlist)
        {
            try
            {
                JSONNode jsonnode = JSON.Parse(File.ReadAllText(Config.BeatSaberPath + "\\Playlists\\" + playlist.fileName + (playlist.oldFormat ? ".json" : ".bplist")));
                playlist.Image = jsonnode["image"];
                playlist.Title = jsonnode["playlistTitle"];
                playlist.Author = jsonnode["playlistAuthor"];
                playlist.Songs = new List<PlaylistSong>();
                foreach (KeyValuePair<string, JSONNode> aKeyValue in jsonnode["songs"].AsArray)
                {
                    JSONNode jsonnode2 = aKeyValue;
                    playlist.Songs.Add(new PlaylistSong(jsonnode2["key"], jsonnode2["songName"]));
                }
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
            JSONNode jsonnode = new JSONObject();
            jsonnode.Add("playlistTitle", new JSONString(playlist.Title));
            jsonnode.Add("playlistAuthor", new JSONString(playlist.Author));
            jsonnode.Add("image", new JSONString(playlist.Image));
            JSONArray jsonarray = new JSONArray();
            try
            {
                foreach (PlaylistSong playlistSong in playlist.Songs)
                {
                    JSONObject jsonobject = new JSONObject();
                    jsonobject.Add("key", new JSONString(playlistSong.key));
                    jsonobject.Add("songName", new JSONString(playlistSong.songName));
                    jsonarray.Add(jsonobject);
                }
                jsonnode.Add("songs", jsonarray);
            }
            catch (Exception ex)
            {
                Logger.Exception("Error parsing playlist songs:", ex);
            }
            jsonnode.Add("fileLoc", new JSONString("1"));
            if (!Directory.Exists(Config.BeatSaberPath + "\\Playlists"))
            {
                Directory.CreateDirectory(Config.BeatSaberPath + "\\Playlists");
            }
            File.WriteAllText(Config.BeatSaberPath + "\\Playlists\\" + playlist.fileName + (playlist.oldFormat ? ".json" : ".bplist"), jsonnode.ToString());
        }
    }
}
