using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SyncSaberService
{
    public class Playlist
    {
        public Playlist(string playlistFileName, string playlistTitle, string playlistAuthor, string image)
        {
            this.fileName = playlistFileName;
            this.Title = playlistTitle;
            this.Author = playlistAuthor;
            this.Image = image;
            this.Songs = new List<PlaylistSong>();
            this.fileLoc = "";
            this.ReadPlaylist();
        }

        public void Add(string songIndex, string songName)
        {
            this.Songs.Add(new PlaylistSong(songIndex, songName));
        }

        public void WritePlaylist()
        {
            PlaylistIO.WritePlaylist(this);
        }

        public bool ReadPlaylist()
        {
            string oldFormatPath = Config.BeatSaberPath + "\\Playlists\\" + this.fileName + ".json";
            string newFormatPath = Config.BeatSaberPath + "\\Playlists\\" + this.fileName + ".bplist";
            this.oldFormat = !File.Exists(newFormatPath);
            Logger.Info(string.Concat(new string[]
            {
                "Playlist \"",
                this.Title,
                "\" found in ",
                this.oldFormat ? "old" : "new",
                " playlist format."
            }), "C:\\Users\\brian\\Documents\\GitHub\\SyncSaber\\SyncSaber\\Playlist.cs", "ReadPlaylist", 126);
            if (File.Exists(this.oldFormat ? oldFormatPath : newFormatPath))
            {
                Playlist playlist = PlaylistIO.ReadPlaylistSongs(this);
                if (playlist != null)
                {
                    this.Title = playlist.Title;
                    this.Author = playlist.Author;
                    this.Image = playlist.Image;
                    this.Songs = playlist.Songs;
                    this.fileLoc = playlist.fileLoc;
                    Logger.Info("Success loading playlist!", "C:\\Users\\brian\\Documents\\GitHub\\SyncSaber\\SyncSaber\\Playlist.cs", "ReadPlaylist", 139);
                    return true;
                }
            }
            return false;
        }

        public string Title;

        public string Author;

        public string Image;

        public List<PlaylistSong> Songs;

        public string fileLoc;

        public string fileName;

        public bool oldFormat = true;
    }
}
