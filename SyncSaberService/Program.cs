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

            Config.Initialize();

            if (!Config.CriticalError)
            {
                Web.HttpClientWrapper.Initialize(Config.MaxConcurrentPageChecks);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                SyncSaber ss = new SyncSaber();

                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(0).Value} feed...");
                try
                {
                    ss.DownloadBeastSaberFeed(0, 50);
                }
                catch (Exception ex)
                {
                    Logger.Exception($"Exception downloading BeastSaberFeed (0)", ex);
                }
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(1).Value} feed...");
                try
                {
                    ss.DownloadBeastSaberFeed(1, 50);
                }
                catch (Exception ex)
                {
                    Logger.Exception($"Exception downloading BeastSaberFeed (1)", ex);
                }
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(2).Value} feed...");
                try
                {
                    ss.DownloadBeastSaberFeed(2, 50);
                }
                catch (Exception ex)
                {
                    Logger.Exception($"Exception downloading BeastSaberFeed (2)", ex);
                }

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
                Console.WriteLine($"\nFinished downloading songs in {(int) processingTime.TotalMinutes} min {processingTime.Seconds} sec");
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
