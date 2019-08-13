using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncSaberLib
{

    [Serializable]
    public class Playlist
    {
        public Playlist(string playlistFileName, string playlistTitle, string playlistAuthor, string image)
        {
            fileName = playlistFileName;
            Title = playlistTitle;
            Author = playlistAuthor;
            Image = image;
            Songs = new List<PlaylistSong>();
            fileLoc = "";
            ReadPlaylist();
        }

        public void TryAdd(string songHash, string songIndex, string songName)
        {
            if (!Songs.Exists(s => !string.IsNullOrEmpty(s.hash) && s.hash.ToUpper() == songHash.ToUpper()))
            {
                Songs.Add(new PlaylistSong(songHash, songIndex, songName));
                // Remove any duplicate song that doesn't have a hash
                var oldSongs = Songs.Where(s => string.IsNullOrEmpty(s.hash) && !string.IsNullOrEmpty(s.key) && s.key.ToLower() == songIndex.ToLower()).ToArray();
                foreach (var song in oldSongs)
                {
                    Songs.Remove(song);
                }
            }
        }

        public void WritePlaylist()
        {
            PlaylistIO.WritePlaylist(this);
        }

        public bool ReadPlaylist()
        {
            string oldFormatPath = Path.Combine(OldConfig.BeatSaberPath, "Playlists", fileName + ".json");
            string newFormatPath = Path.Combine(OldConfig.BeatSaberPath, "Playlists", fileName + ".bplist");
            oldFormat = !File.Exists(newFormatPath);
            Logger.Info($"Playlist {Title} found in {(oldFormat ? "old" : "new")} playlist format.");
            if (File.Exists(oldFormat ? oldFormatPath : newFormatPath))
            {

                PlaylistIO.ReadPlaylistSongs(this);
                /*
                if (playlist != null)
                {
                    Title = playlist.Title;
                    Author = playlist.Author;
                    Image = playlist.Image;
                    Songs = playlist.Songs;
                    fileLoc = playlist.fileLoc;
                    Logger.Info("Success loading playlist!");
                    return true;
                }*/
            }
            return false;
        }

        [JsonProperty("playlistTitle")]
        public string Title;
        [JsonProperty("playlistAuthor")]
        public string Author;

        [JsonProperty("image")]
        public string Image;

        [JsonProperty("songs")]
        public List<PlaylistSong> Songs;

        [JsonProperty("fileLoc")]
        public string fileLoc;
        [JsonIgnore]
        public string fileName;
        [JsonIgnore]
        public bool oldFormat = true;
    }
}
