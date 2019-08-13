using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using static SyncSaberLib.Utilities;
using SyncSaberLib.Data;

namespace SyncSaberLib.Web
{
    public class DownloadJob
    {
        public const string NOTFOUNDERROR = "The remote server returned an error: (404) Not Found.";
        public const string BEATSAVER_DOWNLOAD_URL_BASE = "https://beatsaver.com/api/download/key/";
        private const string TIMEOUTERROR = "The request was aborted: The request was canceled.";

        public enum JobResult
        {
            NOTSTARTED,
            SUCCESS,
            TIMEOUT,
            NOTFOUND,
            UNZIPFAILED,
            OTHERERROR
        }

        public string TempPath = ".syncsabertemp";
        /// <summary>
        /// Timeout in milliseconds.
        /// </summary>
        private int Timeout
        {
            get
            {
                return 3000;
            }
        }
        private SongInfo _song;
        public SongInfo Song
        {
            get { return _song; }
        }
        private FileInfo _localZip;
        public DirectoryInfo SongDirectory { get; private set; }
        public JobResult Result { get; private set; }

        /// <summary>
        /// Creates a new DownloadJob.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="downloadPath">Temp folder where the zip is downloaded to.</param>
        /// <param name="songDirectory">Folder the contents of the zip file are moved to.</param>
        public DownloadJob(SongInfo song, string downloadPath, string songDirectory)
        {
            _tokenSource = new CancellationTokenSource();
            _song = song;
            
            TempPath = Path.Combine("temp", $"temp-{Song.key}"); // Folder the zip file is extracted to.
            _localZip = new FileInfo(downloadPath);
            SongDirectory = new DirectoryInfo(songDirectory);
            if (!_localZip.Directory.Exists)
                _localZip.Directory.Create();

        }

        public async Task<bool> RunJobAsync()
        {
            //JobResult result = JobResult.SUCCESS;
            bool successful = true;
            Task<bool> dwnl = DownloadFile(BEATSAVER_DOWNLOAD_URL_BASE + Song.key, _localZip.FullName);
            //Task<bool> dwnl = DownloadFile("http://releases.ubuntu.com/18.04.2/ubuntu-18.04.2-desktop-amd64.iso", "test.iso");
            try
            {
                successful = await dwnl.ConfigureAwait(false);
            }catch(AggregateException ae)
            {
                ae.WriteExceptions($"Error downloading song {_song.key} {_song.songName} by {_song.authorName}.\n");
                successful = false;
            }

            if (successful)
            {
                Logger.Debug($"Downloaded {Song.key}-{Song.songName} by {Song.authorName} successfully");
                try
                {
                    successful = await ExtractZipAsync(_localZip.FullName, SongDirectory.FullName, TempPath).ConfigureAwait(false);
                }
                catch (AggregateException ae)
                {
                    ae.WriteExceptions($"Error extracting zip {_localZip.FullName} to {TempPath}, with the files to be moved to {SongDirectory.FullName}.\n");
                    successful = false;
                }
 
                if (successful)
                {
                    Logger.Debug($"Extracted {Song.key}-{Song.songName} by {Song.authorName} successfully");
                }
                else
                {
                    Logger.Error($"Extraction failed for {Song.key}-{Song.songName} by {Song.authorName}");
                    Result = JobResult.UNZIPFAILED;
                }
            }
            else
            {
                Logger.Debug($"Download failed for {Song.key} - {Song.songName} by {Song.authorName}");
                //result = false;
                if (Result == JobResult.SUCCESS)
                {
                    Result = JobResult.OTHERERROR;
                }
            }

            return successful;
        }

        private CancellationTokenSource _tokenSource;


        public async Task<bool> DownloadFile(string url, string path)
        {
            bool successful = true;
            Result = JobResult.SUCCESS;

            FileInfo zipFile = new FileInfo(path);

            var token = _tokenSource.Token;

            Task downloadAsync = WebUtils.DownloadFileAsync(url, path, true);
            try
            {
                await downloadAsync.ConfigureAwait(false);
            }
            catch (Exception) { }
            
            if (downloadAsync.IsFaulted || !File.Exists(path))
            {
                successful = false;
                if (downloadAsync.Exception != null)
                {
                    if (downloadAsync.Exception.InnerException.Message.Contains("404"))
                    {
                        Logger.Error($"{url} was not found on Beat Saver.");
                        Result = JobResult.NOTFOUND;
                    }
                    else if (downloadAsync.Exception.InnerException.Message == TIMEOUTERROR)
                    {
                        Logger.Error($"Download of {url} timed out.");
                        Result = JobResult.TIMEOUT;
                    }
                    else
                    {
                        Logger.Exception($"Error downloading {url}", downloadAsync.Exception.InnerException);
                        Result = JobResult.OTHERERROR;
                    }
                }
                else
                {
                    Result = JobResult.OTHERERROR;
                }
            }

            zipFile.Refresh();
            if (!(Result == JobResult.SUCCESS) && zipFile.Exists)
            {
                Logger.Warning($"Failed download, deleting {zipFile.FullName}");
                try
                {
                    var time = Stopwatch.StartNew();
                    bool waitTimeout = false;
                    while (!(IsFileLocked(zipFile.FullName) || !waitTimeout))
                        waitTimeout = time.ElapsedMilliseconds < 3000;
                    if (waitTimeout)
                        Logger.Warning($"Timeout waiting for {zipFile.FullName} to be released for deletion.");
                    File.Delete(zipFile.FullName);
                }
                catch (System.IO.IOException)
                {
                    Logger.Warning("File is in use and can't be deleted");
                }

                successful = false;
            }
            return successful;

        }



        public async Task<bool> ExtractZipAsync(string zipPath, string extractPath, string tempPath)
        {
            bool extracted = true;
            if (File.Exists(zipPath))
            {
                DirectoryInfo tempDir = new DirectoryInfo(tempPath);
                try
                {
                    if (tempDir.Exists)
                    {
                        Logger.Trace($"Creating temp directory: {tempDir.FullName}");
                        tempDir.Create();
                    }

                    Logger.Trace($"Extracting files from {zipPath} to {tempDir.FullName}");
                    await Task.Run(() => {
                        var time = Stopwatch.StartNew();
                        bool waitTimeout = false;
                        while (IsFileLocked(zipPath) && !waitTimeout)
                            waitTimeout = time.ElapsedMilliseconds < 1000;
                        if (waitTimeout)
                            Logger.Warning($"Timeout waiting for {zipPath} to be released for extraction.");
                        using (ZipArchive zipFile = ZipFile.OpenRead(zipPath))
                            zipFile.ExtractToDirectory(tempDir.FullName, true);
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Exception(("An error occured while trying to extract \"" + zipPath + "\"!"), ex);

                    return false;
                }
                try
                {
                    var time = Stopwatch.StartNew();
                    bool waitTimeout = false;
                    while (!(IsFileLocked(zipPath) || !waitTimeout))
                        waitTimeout = time.ElapsedMilliseconds < 3000;
                    if (waitTimeout)
                        Logger.Warning($"Timeout waiting for {zipPath} to be released for extraction.");
                    File.Delete(zipPath);
                }
                catch (System.IO.IOException)
                {
                    Logger.Warning("File is in use and can't be deleted");
                }
                try
                {
                    if (extracted)
                    {
                        DirectoryInfo extractDir = new DirectoryInfo(extractPath);
                        if (!extractDir.Exists)
                        {
                            Logger.Trace($"Creating directory for zip extraction: {extractDir.FullName}");
                            extractDir.Create();
                        }
                        Logger.Trace($"Moving files from {tempDir.FullName} to {extractDir.FullName}");

                        
                        Utilities.MoveFilesRecursively(tempDir, extractDir);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception("An exception occured while trying to move files into their final directory!", ex);
                    extracted = false;
                }
                Utilities.EmptyDirectory(tempDir.FullName, true);
            }
            return extracted;
        }

        
    }

    public static class ZipArchiveExtensions
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryFullPath, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryFullPath);
                return;
            }
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryFullPath, file.FullName);
                string directory = Path.GetDirectoryName(completeFileName);
                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("Trying to extract file outside of destination directory.");
                }
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (file.Name != "")
                    file.ExtractToFile(completeFileName, true);
            }
        }
    }
}
