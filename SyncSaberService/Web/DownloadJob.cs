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
using System.Diagnostics;
using static SyncSaberService.Utilities;

namespace SyncSaberService.Web
{
    public class DownloadJob
    {
        public const string NOTFOUNDERROR = "The remote server returned an error: (404) Not Found.";

        public enum JobResult
        {
            NOTSTARTED,
            SUCCESS,
            TIMEOUT,
            NOTFOUND,
            UNZIPFAILED,
            OTHER
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
        private DirectoryInfo _songDir;
        private JobResult _result;
        public JobResult Result
        {
            get { return _result; }
        }

        public DownloadJob(SongInfo song, string downloadPath, string songDirectory)
        {
            _tokenSource = new CancellationTokenSource();
            _song = song;
            TempPath = $"temp\\temp-{Song.Index}";
            _localZip = new FileInfo(downloadPath);
            _songDir = new DirectoryInfo(songDirectory);
            if (!_localZip.Directory.Exists)
                _localZip.Directory.Create();

        }

        public async Task<bool> RunJob()
        {
            //JobResult result = JobResult.SUCCESS;
            bool successful = true;
            Task<bool> dwnl = DownloadFile("https://beatsaver.com/download/" + Song.Index, _localZip.FullName);
            successful = await dwnl;

            if (successful)
            {
                Logger.Debug($"Downloaded {Song.Index} successfully");
                successful = await ExtractZip(_localZip.FullName, _songDir.FullName, TempPath);

                if (successful)
                {
                    Logger.Debug($"Extracted {Song.Index} successfully");
                }
                else
                {
                    Logger.Error($"Extraction failed for {Song.Index}");
                    _result = JobResult.UNZIPFAILED;
                }
            }
            else
            {
                Logger.Debug($"Download failed for {Song.Index}");
                //result = false;
                if (_result == JobResult.SUCCESS)
                {
                    _result = JobResult.OTHER;
                }
            }

            return successful;
        }
        private System.Timers.Timer webTimer;
        private CancellationTokenSource _tokenSource;

        private void WebTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.Debug($"WebTimer Elapsed for job {Song.Index}, canceling download...");
            _tokenSource.Cancel();
        }
        public void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //Logger.Trace($"DownloadProgressChanged for {Song.Index}, reseting Timeout");
            webTimer.Stop();
            webTimer.Start();
        }
        private const string TIMEOUTERROR = "The request was aborted: The request was canceled.";
        public async Task<bool> DownloadFile(string url, string path)
        {
            bool successful = true;
            _result = JobResult.SUCCESS;

            FileInfo zipFile = new FileInfo(path);
            WebClient client = new WebClient();

            
            var token = _tokenSource.Token;

            webTimer = new System.Timers.Timer(Config.DownloadTimeout*1000);
            webTimer.Elapsed += WebTimer_Elapsed;
            client.DownloadProgressChanged += OnDownloadProgressChanged;
            token.Register(() => client.CancelAsync());
            webTimer.Start();
            Task downloadAsync = client.DownloadFileTaskAsync(new Uri(url), path);
            try
            {
                await downloadAsync;
                webTimer.Stop();
            }
            catch (Exception)
            {
                // Used to catch the cancellation exception.
                // TODO: Catch specific exceptions.
            }
            finally
            {
                webTimer.Stop();
                client.DownloadProgressChanged -= OnDownloadProgressChanged;
            }

            //if (await Task.WhenAny(dwnl, Task.Delay(Config.Timeout)) == dwnl)
            //{
            zipFile.Refresh();
            if (downloadAsync.IsFaulted || !File.Exists(path))
            {
                successful = false;
                if (downloadAsync.Exception != null)
                {
                    if (downloadAsync.Exception.InnerException.Message == NOTFOUNDERROR)
                    {
                        Logger.Error($"{url} was not found on Beat Saver.");
                        _result = JobResult.NOTFOUND;
                    }
                    else if (downloadAsync.Exception.InnerException.Message == TIMEOUTERROR)
                    {
                        Logger.Error($"Download of {url} timed out.");
                        _result = JobResult.TIMEOUT;
                    }
                    else
                    {
                        Logger.Exception($"Error downloading {url}", downloadAsync.Exception.InnerException);
                        _result = JobResult.OTHER;
                    }
                }
                else
                {
                    _result = JobResult.OTHER;
                }
            }
            //}
            /*else
            {
                client.CancelAsync();
                _result = JobResult.TIMEOUT;
                successful = false;
                Logger.Error($"Timeout occured when downloading {url}");
            }*/
            //} catch (Exception ex)
            //{
            //    successful = false;
            //    Logger.Exception($"Failed to download file {url}", ex);
            //}
            zipFile.Refresh();
            if (!(_result == JobResult.SUCCESS) && zipFile.Exists)
            {
                Logger.Warning($"Failed download, deleting {zipFile.FullName}");
                try
                {
                    var time = Stopwatch.StartNew();
                    while (!(IsFileReady(zipFile.FullName) || !(time.ElapsedMilliseconds < 3000)))
                    { }
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



        public async Task<bool> ExtractZip(string zipPath, string extractPath, string tempPath)
        {
            bool extracted = true;
            if (File.Exists(zipPath))
            {
                DirectoryInfo tempDir = new DirectoryInfo(tempPath);
                ZipArchive zipFile = ZipFile.OpenRead(zipPath);
                try
                {
                    if (tempDir.Exists)
                    {
                        Logger.Trace($"Creating temp directory: {tempDir.FullName}");
                        tempDir.Create();
                    }

                    Logger.Trace($"Extracting files from {zipPath} to {tempDir.FullName}");
                    await Task.Run(() => {
                        zipFile.ExtractToDirectory(tempDir.FullName, true);
                    });
                    zipFile.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Exception(("An error occured while trying to extract \"" + zipPath + "\"!"), ex);

                    return false;
                }
                try
                {
                    var time = Stopwatch.StartNew();
                    while (!(IsFileReady(zipPath) || !(time.ElapsedMilliseconds < 3000)))
                    { }
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

        public static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
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
