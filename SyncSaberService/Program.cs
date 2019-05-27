using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace SyncSaberService
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.LogLevel = LogLevel.Info;
            Logger.fileWriter.AutoFlush = true;
            Logger.ShortenSourceName = true;

            try
            {
                Config.Initialize();
            }
            catch (FileNotFoundException)
            { }
            if (args.Length > 0)
            {
                var bsDir = new DirectoryInfo(args[0]);
                if (bsDir.Exists)
                {
                    if (bsDir.GetFiles("Beat Saber.exe").Length > 0)
                    {
                        Logger.Info("Found Beat Saber.exe");
                        Config.BeatSaberPath = bsDir.FullName;
                        Logger.Info($"Updated Beat Saber directory path to {Config.BeatSaberPath}");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                    else
                        Logger.Warning($"Provided directory does not appear to be Beat Saber's root folder, ignoring it");
                }
            }
            

            if (!Config.CriticalError)
            {
                Web.HttpClientWrapper.Initialize(Config.MaxConcurrentPageChecks);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                SyncSaber ss = new SyncSaber();
                Console.WriteLine();
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(0).Value} feed...");
                try
                {
                    ss.DownloadBeastSaberFeed(0, Web.BeastSaberReader.GetMaxBeastSaberPages(0));
                }
                catch (Exception ex)
                {
                    Logger.Exception($"Exception downloading BeastSaberFeed (0)", ex);
                }
                Console.WriteLine();
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(1).Value} feed...");
                try
                {
                    //ss.DownloadBeastSaberFeed(1, Web.BeastSaberReader.GetMaxBeastSaberPages(1));
                }
                catch (Exception ex)
                {
                    Logger.Exception($"Exception downloading BeastSaberFeed (1)", ex);
                }
                Console.WriteLine();
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(2).Value} feed...");
                try
                {
                   // ss.DownloadBeastSaberFeed(2, Web.BeastSaberReader.GetMaxBeastSaberPages(2));
                }
                catch (Exception ex)
                {
                    Logger.Exception($"Exception downloading BeastSaberFeed (2)", ex);
                }
                Console.WriteLine();
                Logger.Info($"Downloading songs from FavoriteMappers.ini...");
                try
                {
                    ss.DownloadAllSongsByAuthors(Config.FavoriteMappers);
                }
                catch (Exception ex)
                {
                    Logger.Exception("Exception downloading BeatSaver authors feed.", ex);
                }
                sw.Stop();
                var processingTime = new TimeSpan(sw.ElapsedTicks);
                Console.WriteLine();
                Logger.Info($"Finished downloading songs in {(int) processingTime.TotalMinutes} min {processingTime.Seconds} sec");
            }
            else
            {
                foreach (string e in Config.Errors)
                    Logger.Error($"Invalid setting: {e} = {Config.Setting[e]}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }


    }
}
