using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Http;
using static SyncSaberService.Utilities;
using SyncSaberService.Web;

namespace SyncSaberService
{
    public class SyncSaber
    {
        public static SyncSaber Instance;
        private Dictionary<string, string> _beastSaberFeeds;
        public Dictionary<string, string> BeastSaberFeeds
        {
            get
            {
                if (_beastSaberFeeds == null)
                    _beastSaberFeeds = new Dictionary<string, string>();
                return _beastSaberFeeds;
            }
        }


        public SyncSaber()
        {
            Instance = this;
            if (Config.SyncFollowingsFeed)
            {
                BeastSaberFeeds.Add("followings", "https://bsaber.com/members/" + Config.BeastSaberUsername + "/wall/followings");
            }
            if (Config.SyncBookmarksFeed)
            {
                BeastSaberFeeds.Add("bookmarks", "https://bsaber.com/members/" + Config.BeastSaberUsername + "/bookmarks");
            }
            if (Config.SyncCuratorRecommendedFeed)
            {
                BeastSaberFeeds.Add("curator recommended", "https://bsaber.com/members/curatorrecommended/bookmarks");
            }

            _historyPath = Path.Combine(Config.BeatSaberPath, "UserData", "SyncSaberHistory.txt");
            if (File.Exists(_historyPath + ".bak"))
            {
                if (File.Exists(_historyPath))
                {
                    File.Delete(_historyPath);
                }
                File.Move(_historyPath + ".bak", _historyPath);
            }
            if (File.Exists(_historyPath))
            {
                _songDownloadHistory = File.ReadAllLines(_historyPath).ToList<string>();
            }
            if (!Directory.Exists(Config.BeatSaberPath + "\\CustomSongs"))
            {
                Directory.CreateDirectory(Config.BeatSaberPath + "\\CustomSongs");
            }
        }

        private Playlist GetPlaylistForFeed(string feedName)
        {
            if (feedName == "followings")
            {
                return _followingsSongs;
            }
            if (feedName == "bookmarks")
            {
                return _bookmarksSongs;
            }
            if (!(feedName == "curator recommended"))
            {
                return null;
            }
            return _curatorRecommendedSongs;
        }

        private Playlist GetPlaylistForFeed(int feedToDownload)
        {
            string feedName = _beastSaberFeeds.ElementAt(feedToDownload).Key;
            return GetPlaylistForFeed(feedName);
        }

        private void UpdatePlaylist(Playlist playlist, string songIndex, string songName)
        {
            if (playlist == null)
            {
                Logger.Warning($"playlist is null in UpdatePlaylist for song {songIndex} - {songName}");
                return;
            }
            bool songAlreadyInPlaylist = false;
            foreach (PlaylistSong playlistSong in playlist.Songs)
            {
                string songID = songIndex.Substring(0, songIndex.IndexOf("-"));
                string songVersion = songIndex.Substring(songIndex.IndexOf("-") + 1);
                if (playlistSong.key.StartsWith(songID))
                {
                    if (_beatSaverRegex.IsMatch(playlistSong.key))
                    {
                        string oldVersionId = playlistSong.key.Substring(playlistSong.key.IndexOf("-") + 1);
                        if (Convert.ToInt32(oldVersionId) < Convert.ToInt32(songVersion))
                        {
                            playlistSong.key = songIndex;
                            playlistSong.songName = songName;
                            Logger.Info($"Success updating playlist {playlist.Title}! Updated \"{songName}\" with index {songID} from version {oldVersionId} to {songVersion}");
                        }
                    }
                    else if (_digitRegex.IsMatch(playlistSong.key))
                    {
                        playlistSong.key = songIndex;
                        playlistSong.songName = songName;
                        Logger.Info($"Success updating playlist {playlist.Title}! Song \"{songName}\" with index {songID} was missing version! Adding version {songVersion}");
                    }
                    songAlreadyInPlaylist = true;
                }
            }
            if (!songAlreadyInPlaylist)
            {
                playlist.Add(songIndex, songName);
                Logger.Info($"Success adding new song \"{songName}\" with BeatSaver index {songIndex} to playlist {playlist.Title}!");
            }
        }

        private void RemoveOldVersions(string songIndex)
        {
            if (!Config.DeleteOldVersions)
            {
                return;
            }
            string[] customSongDirectories = Directory.GetDirectories(Path.Combine(Config.BeatSaberPath, "CustomSongs"));
            string id = songIndex.Substring(0, songIndex.IndexOf("-"));
            string version = songIndex.Substring(songIndex.IndexOf("-") + 1);
            foreach (string directory in customSongDirectories)
            {
                try
                {
                    string directoryName = Path.GetFileName(directory);
                    if (this._beatSaverRegex.IsMatch(directoryName) && directoryName != songIndex)
                    {
                        string directoryId = directoryName.Substring(0, directoryName.IndexOf("-"));
                        if (directoryId == id)
                        {

                            string directoryVersion = directoryName.Substring(directoryName.IndexOf("-") + 1);
                            string directoryToRemove = directory;
                            string currentVersion = songIndex;
                            string oldVersion = directoryName;
                            if (Convert.ToInt32(directoryVersion) > Convert.ToInt32(version))
                            {
                                directoryToRemove = Path.Combine(Config.BeatSaberPath, "CustomSongs", songIndex);
                                currentVersion = directoryName;
                                oldVersion = songIndex;
                            }
                            Logger.Info($"Deleting old song with identifier \"{oldVersion}\" (current version: {currentVersion})");
                            Directory.Delete(directoryToRemove, true);
                        }
                    }
                    else if (this._digitRegex.IsMatch(directoryName) && directoryName == id)
                    {
                        Logger.Info($"Deleting old song with identifier \"{directoryName}\" (current version: {id}-{version})");
                        Directory.Delete(directory, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception("Exception when trying to remove old versions: ", ex);
                }
            }
        }

        public void ClearResultQueues()
        {
            SuccessfulDownloads.Clear();
            FailedDownloads.Clear();
        }

        public void OnJobFinished(DownloadJob job)
        {
            if (job.Result == DownloadJob.JobResult.SUCCESS)
                SuccessfulDownloads.Enqueue(job);
            else
                FailedDownloads.Enqueue(job);
            // TODO: Add fail reason to FailedDownloads items
        }

        public void DownloadAllSongsByAuthors(List<string> mappers)
        {
            var feedReader = new BeatSaverReader();
            foreach (string mapper in mappers)
            {
                DownloadAllSongsByAuthor(mapper, feedReader);
            }
        }

        public void DownloadAllSongsByAuthor(string mapper, IFeedReader feedReader = null)
        {
            DateTime startTime = DateTime.Now;
            Dictionary<int, SongInfo> songs;
            List<Playlist> playlists = new List<Playlist>();
            switch (feedReader.Name)
            {
                case "BeatSaverReader":
                    songs = feedReader.GetSongsFromFeed(new BeatSaverFeedSettings(0, mapper));
                    playlists.Add(GetPlaylistForFeed("followings"));
                    break;
                default:
                    songs = new Dictionary<int, SongInfo>();
                    break;
            }
            Logger.Debug($"Finished checking pages, found {songs.Count} songs");
            int totalSongs = songs.Count;
            DownloadSongs(songs);
            Logger.Debug("Jobs finished, Processing downloads...");
            int downloadCount = SuccessfulDownloads.Count;
            int failedCount = FailedDownloads.Count;
            

            ProcessDownloads(playlists);
            var timeElapsed = (DateTime.Now - startTime);
            Logger.Info($"Downloaded {downloadCount} songs from mapper {mapper} in {FormatTimeSpan(timeElapsed)}. " +
                $"Skipped {totalSongs - downloadCount} songs.");
        }

        public void DownloadBeastSaberFeed(int feedToDownload, int maxPages)
        {
            DownloadBatch jobs = new DownloadBatch();
            jobs.JobCompleted += OnJobFinished;
            ClearResultQueues();
            DateTime startTime = DateTime.Now;
            int downloadCount = 0;
            int totalSongs = 0;

            var bReader = new BeastSaberReader(Config.BeastSaberUsername, Config.BeastSaberPassword, Config.MaxConcurrentPageChecks);
            var queuedSongs = bReader.GetSongsFromFeed(new BeastSaberFeedSettings(feedToDownload, maxPages));
            totalSongs = queuedSongs.Count;
            Logger.Debug($"Finished checking pages, found {queuedSongs.Count} songs");

            string customSongsPath = Path.Combine(Config.BeatSaberPath, "CustomSongs");
            var existingSongs = Directory.GetDirectories(customSongsPath);
            string tempPath = "";
            string outputPath = "";
            foreach (var song in queuedSongs.Values)
            {
                tempPath = Path.Combine(Path.GetTempPath(), song.key + ".zip");
                outputPath = Path.Combine(Config.BeatSaberPath, "CustomSongs", song.key);
                if (existingSongs.Contains(song.key) || _songDownloadHistory.Contains(song.key) || Directory.Exists(outputPath))
                    continue; // We already have the song or don't want it, skip
                DownloadJob job = new DownloadJob(song, tempPath, outputPath);
                jobs.AddJob(job);
            }
            jobs.RunJobs().Wait();
            Logger.Debug("Jobs finished, Processing downloads...");
            downloadCount = SuccessfulDownloads.Count;
            int failedCount = FailedDownloads.Count;

            ProcessDownloads();
            var timeElapsed = (DateTime.Now - startTime);
            Logger.Info($"Downloaded {downloadCount} songs from BeastSaber {_beastSaberFeeds.ElementAt(feedToDownload).Key} feed in {FormatTimeSpan(timeElapsed)}. Skipped {totalSongs - downloadCount - failedCount} songs.");
        }

        private void ProcessDownloads(List<Playlist> playlists = null)
        {
            if (playlists == null)
                playlists = new List<Playlist>();
            playlists.Add(_syncSaberSongs);
            Logger.Debug("Processing downloads...");
            DownloadJob job;
            while (SuccessfulDownloads.TryDequeue(out job))
            {
                if (!_songDownloadHistory.Contains(job.Song.key))
                    _songDownloadHistory.Add(job.Song.key);

                UpdatePlaylist(_syncSaberSongs, job.Song.key, job.Song.name);
                if (!string.IsNullOrEmpty(job.Song.Feed))
                    UpdatePlaylist(GetPlaylistForFeed(job.Song.Feed), job.Song.key, job.Song.name);
                RemoveOldVersions(job.Song.key);
            }
            while (FailedDownloads.TryDequeue(out job))
            {
                if (!_songDownloadHistory.Contains(job.Song.key) && job.Result != DownloadJob.JobResult.TIMEOUT)
                {
                    _songDownloadHistory.Add(job.Song.key);
                }
                Logger.Error($"Failed to download {job.Song.key} by {job.Song.authorName}");
            }

            Utilities.WriteStringListSafe(_historyPath, _songDownloadHistory.Distinct<string>().ToList<string>(), true);
            foreach (Playlist playlist in playlists)
                playlist.WritePlaylist();
        }

        public void DownloadSongs(Dictionary<int, SongInfo> queuedSongs)
        {
            string customSongsPath = Path.Combine(Config.BeatSaberPath, "CustomSongs");
            var existingSongs = Directory.GetDirectories(customSongsPath);
            string tempPath = "";
            string outputPath = "";

            DownloadBatch jobs = new DownloadBatch();
            jobs.JobCompleted += OnJobFinished;
            foreach (var song in queuedSongs.Values)
            {
                tempPath = Path.Combine(Path.GetTempPath(), song.key + ".zip");
                outputPath = Path.Combine(Config.BeatSaberPath, "CustomSongs", song.key);
                if (existingSongs.Contains(song.key) || _songDownloadHistory.Contains(song.key) || Directory.Exists(outputPath))
                    continue; // We already have the song or don't want it, skip
                DownloadJob job = new DownloadJob(song, tempPath, outputPath);
                jobs.AddJob(job);
            }
            jobs.RunJobs().Wait();
        }

        private readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);

        private readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);

        private readonly string _historyPath;

        private ConcurrentQueue<DownloadJob> _successfulDownloads;
        private ConcurrentQueue<DownloadJob> SuccessfulDownloads
        {
            get
            {
                if (_successfulDownloads == null)
                    _successfulDownloads = new ConcurrentQueue<DownloadJob>();
                return _successfulDownloads;
            }
        }

        private ConcurrentQueue<DownloadJob> _failedDownloads;
        private ConcurrentQueue<DownloadJob> FailedDownloads
        {
            get
            {
                if (_failedDownloads == null)
                    _failedDownloads = new ConcurrentQueue<DownloadJob>();
                return _failedDownloads;
            }
        }

        private readonly List<Task<bool>> _runningJobs = new List<Task<bool>>();

        private List<string> _songDownloadHistory = new List<string>();

        private readonly Playlist _syncSaberSongs = new Playlist("SyncSaberPlaylist", "SyncSaber Playlist", "brian91292", "1");

        private readonly Playlist _curatorRecommendedSongs = new Playlist("SyncSaberCuratorRecommendedPlaylist", "BeastSaber Curator Recommended", "brian91292", "1");

        private readonly Playlist _followingsSongs = new Playlist("SyncSaberFollowingsPlaylist", "BeastSaber Followings", "brian91292", "1");

        private readonly Playlist _bookmarksSongs = new Playlist("SyncSaberBookmarksPlaylist", "BeastSaber Bookmarks", "brian91292", "1");


    }

}
