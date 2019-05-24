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
            Logger.LogLevel = LogLevel.Debug;
            Logger.ShortenSourceName = true;
            
            Config.Initialize();

            //var testReader = new Web.BeastSaverReader(Config.BeastSaberUsername, Config.BeastSaberPassword);
            //var testList = testReader.GetSongsFromFeed(0, 0, 3);

            if (!Config.CriticalError)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                SyncSaber ss = new SyncSaber();

                SyncSaber.FeedPageInfo testPage = new SyncSaber.FeedPageInfo {
                    feedToDownload = 2,
                    feedUrl = "https://bsaber.com/members/zingabopp/wall/followings/feed/?acpage=3",
                    pageIndex = 3
                };
                //int count = ss.CheckFeed(testPage);
                var test = HttpRequestHeader.Cookie.ToString();
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(0).Value} feed...");
                ss.DownloadBeastSaberFeeds(0);
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(1).Value} feed...");
                ss.DownloadBeastSaberFeeds(1);
                Logger.Info($"Downloading songs from {ss.BeastSaberFeeds.ElementAt(2).Value} feed...");
                ss.DownloadBeastSaberFeeds(2);
                foreach(string mapper in Config.FavoriteMappers)
                {
                    //ss.DownloadAllSongsByAuthor(mapper);
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
