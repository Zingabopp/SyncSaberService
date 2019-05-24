using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SimpleJSON;
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

        public int ProcessFeedPage(string pageText, FeedPageInfo info)
        {
            int downloadCountForPage = 0;
            int totalSongsForPage = 0;
            XmlDocument xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.LoadXml(pageText);
            }
            catch (Exception ex)
            {
                Logger.Exception($"Exception loading XML from {info.feedUrl}: ", ex);
                _downloaderRunning = false;
                return downloadCountForPage;
            }
            XmlNodeList xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/rss/channel/item");
            foreach (object obj in xmlNodeList)
            {
                XmlNode node = (XmlNode) obj;
                if (node["DownloadURL"] == null || node["SongTitle"] == null)
                {
                    Logger.Debug("Not a song! Skipping!");
                }
                else
                {
                    string songName = node["SongTitle"].InnerText;
                    string innerText = node["DownloadURL"].InnerText;
                    if (innerText.Contains("dl.php"))
                    {
                        Logger.Warning("Skipping BeastSaber download with old url format!");
                        totalSongsForPage++;
                    }
                    else
                    {
                        string songIndex = innerText.Substring(innerText.LastIndexOf('/') + 1);
                        string mapper = GetMapperFromBsaber(node.InnerText);
                        string songUrl = "https://beatsaver.com/download/" + songIndex;
                        SongInfo currentSong = new SongInfo(songIndex, songName, songUrl, mapper, _beastSaberFeeds.ElementAt(info.feedToDownload).Key);
                        string currentSongDirectory = Path.Combine(Config.BeatSaberPath, "CustomSongs", songIndex);
                        //bool downloadFailed = false;
                        if (Config.AutoDownloadSongs && !(this._songDownloadHistory.Contains(songIndex) || Directory.Exists(currentSongDirectory)))
                        {
                            Logger.Info($"Queued {songIndex} - {songName} for download");
                            string localPath = Path.Combine(Path.GetTempPath(), songIndex + ".zip");
                            DownloadJob job = new DownloadJob(currentSong, localPath, currentSongDirectory);
                            _songDownloadQueue.Enqueue(job);
                            //jobs.AddJob(job);
                            downloadCountForPage++;
                        }
                        totalSongsForPage++;
                    }
                }
            }
            if(totalSongsForPage == 0)
            {
                Logger.Debug($"Page {info.pageIndex} has no songs");
                lock(EarliestEmptyPage)
                {
                    if (( EarliestEmptyPage.number) > info.pageIndex)
                    {
                        Logger.Debug($"Page {info.pageIndex} is less than the EarlistEmptyPage {EarliestEmptyPage.number}");
                        EarliestEmptyPage.number = info.pageIndex;
                    }
                }
            }
            Logger.Info($"Finished page {info.pageIndex}, queued {downloadCountForPage} songs out of {totalSongsForPage}");
            return totalSongsForPage;
        }

        public int CheckFeed(SyncSaber.FeedPageInfo info)
        {
            
            Logger.Info($"Checking page {info.pageIndex.ToString()} of {_beastSaberFeeds.ElementAt(info.feedToDownload).Key} feed from BeastSaber!");
            Uri feedUri = new Uri(info.feedUrl);
            HttpWebRequest newrequest = (HttpWebRequest) WebRequest.Create(feedUri);
            newrequest.Proxy = null;
            //newrequest.CookieContainer = Cookies;
            newrequest.Headers.Add(HttpRequestHeader.Cookie, Cookies.GetCookieHeader(feedUri));
            WebClient client = new WebClient();
            var header = Cookies.GetCookieHeader(new Uri(info.feedUrl));
            client.Headers.Add(HttpRequestHeader.Cookie, CookieHeader);
            string pageText = client.DownloadString(info.feedUrl);

            return ProcessFeedPage(pageText, info);
            
        }

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

            this._historyPath = Path.Combine(Config.BeatSaberPath, "UserData", "SyncSaberHistory.txt");
            if (File.Exists(this._historyPath + ".bak"))
            {
                if (File.Exists(this._historyPath))
                {
                    File.Delete(this._historyPath);
                }
                File.Move(this._historyPath + ".bak", this._historyPath);
            }
            if (File.Exists(this._historyPath))
            {
                this._songDownloadHistory = File.ReadAllLines(this._historyPath).ToList<string>();
            }
            if (!Directory.Exists(Config.BeatSaberPath + "\\CustomSongs"))
            {
                Directory.CreateDirectory(Config.BeatSaberPath + "\\CustomSongs");
            }
            var cookieInit = Cookies;
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

        public void ClearDownloadQueue()
        {
            _songDownloadQueue.Clear();
        }

        public void OnJobFinished(DownloadJob job)
        {
            if (job.Result == DownloadJob.JobResult.SUCCESS)
                SuccessfulDownloads.Enqueue(job);
            else
                FailedDownloads.Enqueue(job);
            // TODO: Add fail reason to FailedDownloads items
        }

        public void DownloadAllSongsByAuthor(string author)
        {
            DownloadBatch jobs = new DownloadBatch();
            jobs.JobCompleted += OnJobFinished;
            ClearResultQueues();
            Logger.Info("Downloading all songs from " + author);
            this._downloaderRunning = true;
            DateTime startTime = DateTime.Now;
            TimeSpan idleTime = default(TimeSpan);
            string mapperId = string.Empty;
            //using (UnityWebRequest www = UnityWebRequest.Get("https://beatsaver.com/api/songs/search/user/" + author))
            WebClient client = new WebClient();
            var cookInit = Cookies;

            string resp = "";
            using (StreamReader reader = new StreamReader(client.OpenRead("https://beatsaver.com/api/songs/search/user/" + author)))
            {
                resp = reader.ReadToEnd();
            }
            JSONNode result = JSON.Parse(resp);

            if (result["total"].AsInt == 0)
            {
                this._downloaderRunning = false;
                return;
            }
            foreach (JSONObject song in result["songs"].AsArray)
            {
                mapperId = song["uploaderId"].Value;
                break; // TODO: Getting the mapper ID like this is dumb...
            }
            if (mapperId == string.Empty)
            {
                Logger.Error("Failed to find mapper \"" + author);
                this._downloaderRunning = false;
                return;
            }

            int downloadCount = 0;
            int currentSongIndex = 0;
            int totalSongs = 1;
            Logger.Info("Checking for new songs from \"" + author + "\"");
            while (currentSongIndex < totalSongs)
            {
                using (StreamReader reader = new StreamReader(client.OpenRead("https://beatsaver.com/api/songs/search/user/" + author)))
                {
                    resp = reader.ReadToEnd();
                }
                result = JSON.Parse(resp);
                if (result["total"].AsInt == 0)
                {
                    this._downloaderRunning = false;
                    Logger.Debug($"No songs found for {author}");
                    return;
                }

                totalSongs = result["total"].AsInt;

                foreach (JSONObject song in result["songs"].AsArray)
                {
                    //JSONObject song = (JSONObject) aKeyValue;
                    string songIndex = song["version"].Value;
                    string songName = song["songName"].Value;
                    string songUrl = "https://beatsaver.com/download/" + songIndex;
                    SongInfo currentSong = new SongInfo(songIndex, songName, songUrl, author, "followings");
                    string currentSongDirectory = Path.Combine(Config.BeatSaberPath, "CustomSongs", songIndex);
                    //bool downloadFailed = false;
                    if (Config.AutoDownloadSongs && !this._songDownloadHistory.Contains(songIndex) && !Directory.Exists(currentSongDirectory))
                    {

                        Logger.Info($"Queued {songIndex} - {songName} for download");
                        string localPath = Path.Combine(Path.GetTempPath(), songIndex + ".zip");
                        DownloadJob job = new DownloadJob(currentSong, localPath, currentSongDirectory);
                        jobs.AddJob(job);
                    }
                    currentSongIndex++;
                }

            }

            jobs.RunJobs().Wait();
            Logger.Debug("Jobs finished, Processing downloads...");
            downloadCount = SuccessfulDownloads.Count;
            int failedCount = FailedDownloads.Count;
            totalSongs = SuccessfulDownloads.Count + FailedDownloads.Count;

            ProcessDownloads();

            TimeSpan timeElapsed = (DateTime.Now - startTime);

            Logger.Info($"Downloaded {downloadCount} from mapper {author} in {FormatTimeSpan(timeElapsed)}. Failed to download {failedCount} songs.");
            _downloaderRunning = false;
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
                if (!_songDownloadHistory.Contains(job.Song.Index))
                    _songDownloadHistory.Add(job.Song.Index);

                UpdatePlaylist(_syncSaberSongs, job.Song.Index, job.Song.Name);
                if (!string.IsNullOrEmpty(job.Song.Feed))
                    UpdatePlaylist(GetPlaylistForFeed(job.Song.Feed), job.Song.Index, job.Song.Name);
                RemoveOldVersions(job.Song.Index);
            }
            while (FailedDownloads.TryDequeue(out job))
            {
                //failedCount++;
                if (!_songDownloadHistory.Contains(job.Song.Index) && job.Result != DownloadJob.JobResult.TIMEOUT)
                {
                    _songDownloadHistory.Add(job.Song.Index);
                }
                Logger.Error($"Failed to download {job.Song.Index} by {job.Song.Author}");
            }

            Utilities.WriteStringListSafe(_historyPath, _songDownloadHistory.Distinct<string>().ToList<string>(), true);
            foreach (Playlist playlist in playlists)
                playlist.WritePlaylist();
        }

        private WebClient _client;
        private WebClient Client
        {
            get
            {
                if (_client == null)
                    _client = new WebClient();
                return _client;
            }
        }

        private CookieContainer _cookies;
        
        private CookieContainer Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = Utilities.LoginBSaber(Config.BeastSaberUsername, Config.BeastSaberPassword);
                return _cookies;
            }
        }

        private string _cookieHeader = "";
        private string CookieHeader
        {
            get
            {
                if (_cookieHeader == "")
                    _cookieHeader = Cookies.GetCookieHeader(new Uri("https://bsaber.com"));
                return _cookieHeader;
            }
        }

        

        private static string GetMapperFromBsaber(string innerText)
        {
            string prefix = "Mapper: ";
            string suffix = "</p>";
            int startIndex = innerText.IndexOf(prefix) + prefix.Length;
            int endIndex = innerText.IndexOf(suffix, startIndex);
            if (endIndex > startIndex && startIndex >= 0)
                return innerText.Substring(startIndex, endIndex - startIndex);
            else
                return "";
        }

        public async Task<int> CheckFeedPage(FeedPageInfo info, CookieContainer cook)
        {
            int downloadCountForPage = 0;
            int totalSongsForPage = 0;
            Logger.Info($"Checking page {info.pageIndex.ToString()} of {_beastSaberFeeds.ElementAt(info.feedToDownload).Key} feed from BeastSaber!");
            HttpWebRequest newrequest = (HttpWebRequest) WebRequest.Create(info.feedUrl);
            newrequest.Proxy = null;
            newrequest.CookieContainer = cook;
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Cookie, cook.GetCookieHeader(new Uri(info.feedUrl)));
            Logger.Debug($"Page {info.pageIndex}: Got Cookies, starting web request");
            downloadCountForPage = ProcessFeedPage(client.DownloadString(info.feedUrl), info);
            Logger.Info($"Finished page {info.pageIndex}, queued {downloadCountForPage}");// songs out of {totalSongsForPage}");
            return downloadCountForPage;
        }

        public struct FeedPageInfo
        {
            public int feedToDownload;
            public string feedUrl;
            public int pageIndex;
        }
        private static IntegerWrapper EarliestEmptyPage = new IntegerWrapper(9999);

        public void DownloadBeastSaberFeed(int feedToDownload, int maxPages)
        {
            DownloadBatch jobs = new DownloadBatch();
            jobs.JobCompleted += OnJobFinished;
            ClearResultQueues();
            _downloaderRunning = true;
            DateTime startTime = DateTime.Now;
            int downloadCount = 0;
            int totalSongs = 0;
            int pageIndex = 0;
            var cook = Cookies;
            IFeedReader bReader = new BeastSaverReader(Config.BeastSaberUsername, Config.BeastSaberPassword, Config.MaxConcurrentDownloads);
            var queuedSongs = bReader.GetSongsFromFeed(feedToDownload, maxPages);
            
            Logger.Debug($"Finished checking pages, queueing {queuedSongs.Count} songs");
            while (!_songDownloadQueue.IsEmpty)
            {
                DownloadJob job;
                if (_songDownloadQueue.TryDequeue(out job))
                    jobs.AddJob(job);
            }
            jobs.RunJobs().Wait();
            Logger.Debug("Jobs finished, Processing downloads...");
            downloadCount = SuccessfulDownloads.Count;
            int failedCount = FailedDownloads.Count;
            totalSongs = SuccessfulDownloads.Count + FailedDownloads.Count;

            ProcessDownloads();
            var timeElapsed = (DateTime.Now - startTime);
            Logger.Info(string.Format("Downloaded {0} songs from BeastSaber {1} feed in {2}. Checked {3} page{4}, skipped {5} songs.", new object[]
            {
                downloadCount,
                this._beastSaberFeeds.ElementAt(feedToDownload).Key,
                FormatTimeSpan(timeElapsed),
                pageIndex,
                (pageIndex != 1) ? "s" : "",
                totalSongs - downloadCount
            }));
            this._downloaderRunning = false;
        }

        private int GetMaxBeastSaberPages(int feedToDownload)
        {
            string key = this._beastSaberFeeds.ElementAt(feedToDownload).Key;
            if (key == "followings")
            {
                return Config.MaxFollowingsPages;
            }
            if (key == "bookmarks")
            {
                return Config.MaxBookmarksPages;
            }
            if (!(key == "curator recommended"))
            {
                return 0;
            }
            return Config.MaxCuratorRecommendedPages;
        }
        /*
        public async void WorkDownloadQueue(int taskLimit)
        {

            var actionBlock = new ActionBlock<DownloadJob>(job => {
                Logger.Debug($"Running job {job.Song.Index} in ActionBlock");
                Task<JobResult> newTask = job.RunJob();
                newTask.Wait();
                TaskComplete(job.Song.Index);
            }, new ExecutionDataflowBlockOptions {
                BoundedCapacity = 500,
                MaxDegreeOfParallelism = taskLimit
            });
            while(_songDownloadQueue.Count > 0)
            {
                var job = _songDownloadQueue.Pop();
                Logger.Debug($"Adding job for {job.Song.Index}");
                await actionBlock.SendAsync(job);
            }
            
            actionBlock.Complete();
            await actionBlock.Completion;
            Logger.Debug($"Actionblock complete");
            
        }

        public void TaskComplete(string songIndex)
        {
            Logger.Info($"Completed processing {songIndex}");
        }
        */
        private readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);

        private readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);

        private bool _downloaderRunning;
        private string _historyPath;

        private readonly Stack<string> _authorDownloadQueue = new Stack<string>();

        private readonly ConcurrentQueue<DownloadJob> _songDownloadQueue = new ConcurrentQueue<DownloadJob>();

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

    public class IntegerWrapper
    {
        private int _number;
        public int number
        {
            get { return _number; }
            set
            {
                Logger.Debug($"Settings current number {_number} to {value}");
                _number = value;
            }
        }
        public IntegerWrapper(int initial)
        {
            number = initial;
        }
    }
}
