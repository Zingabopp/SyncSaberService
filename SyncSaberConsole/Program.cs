using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;
using SyncSaberLib;
using SyncSaberLib.Web;
using SyncSaberLib.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncSaberConsole
{
    class Program
    {


        private static void Tests()
        {
            //ScrapedDataProvider.Initialize();

            WebUtils.Initialize(5);


            //ScrapedDataProvider.Initialize();


            //ScrapedDataProvider.Initialize();
            var bsScrape = ScrapedDataProvider.BeatSaverSongs;
            var ssScrape = ScrapedDataProvider.ScoreSaberSongs;
            ScrapedDataProvider.TryGetSongByHash("501f6b1bddb2af72abda0f1e6b7b89cb1eb3db67", out SongInfo deletedSong);
            var job = new DownloadJob(deletedSong, "test.zip", @"ScrapedData\test");
            var jobTask = job.RunJobAsync();
            jobTask.Wait();
            bsScrape.AddOrUpdate(null);
            var resp = WebUtils.httpClient.GetAsync("https://beatsaver.com/api/maps/detail/b");
            Task.WaitAll(resp);

            var rateHeaders = resp.Result.Headers.Where(h => h.Key.StartsWith("Rate-Limit")).ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
            var remoteTime = resp.Result.Headers.Date;
            var rateInfo = WebUtils.ParseRateLimit(rateHeaders);
            Console.WriteLine("Reset Timespan: " + rateInfo.TimeToReset.ToString());
            foreach (var item in resp.Result.Headers)
            {
                Console.WriteLine($"{item.Key}: {string.Join("|", item.Value)}");
            }
            //var test = new SyncSaberScrape();

            //test.Initialize();

            var beatSaverScrape = new BeatSaverScrape();
            beatSaverScrape.Initialize();
            var songs = beatSaverScrape.Data;
            var deleted = songs.Where(s => s.deletedAt > DateTime.MinValue);
            //var pageText = File.ReadAllText("test_multiplesongs_page.txt");
            //var multiSongs = BeatSaverReader.ParseSongsFromPage(pageText);
            //pageText = File.ReadAllText("test_detail_page.txt");
            //var singleSong = BeatSaverReader.ParseSongsFromPage(pageText);
            //var newSongs = BeatSaverReader.ScrapeBeatSaver(500, true, 1);
            var scoreSaberSongs = ScoreSaberReader.ScrapeScoreSaber(5000, 10000, false, 0);
            using (StreamWriter file = File.CreateText(@"ScrapedData\ScoreSaberScrape.json"))
            {
                //JsonSerializer serializer = new JsonSerializer();
                //serializer.Serialize(file, beatSaverSongs);
                file.Write(JsonConvert.SerializeObject(scoreSaberSongs));
            }
            var beatSaverSongs = ScrapedDataProvider.Songs.Values.Select(s => s.BeatSaverInfo).ToArray();
            using (StreamWriter file = File.CreateText(@"ScrapedData\BeatSaverScrape.json"))
            {
                //JsonSerializer serializer = new JsonSerializer();
                //serializer.Serialize(file, beatSaverSongs);
                file.Write(JsonConvert.SerializeObject(beatSaverSongs));
            }
            //ScrapedDataProvider.UpdateScrapedFile();
            //test.Data.AddRange(ScrapedDataProvider.SyncSaberScrape.Take(20));
            //test.WriteFile(Path.Combine(SyncSaberScrape.DATA_DIRECTORY.FullName, "newScrap.json"));

            var trending = ScrapedDataProvider.Songs.Values.Where(s => s.ScoreSaberInfo.Count > 0).OrderByDescending(s => s.ScoreSaberInfo.Values.Select(ss => ss.scores).Aggregate((a, b) => a + b)).Take(100);
            var detTrending = trending.Select(s => (s.ScoreSaberInfo.Values.Select(ss => ss.scores).Aggregate((a, b) => a + b), s)).ToList();

            //var scrapeSongs = BeatSaverReader.ScrapeBeatSaver(500, true, 0);
            //ScoreSaberReader.ScrapeScoreSaber(3000, 1000, false);

            //ScrapedDataProvider.UpdateScrapedFile();

            var reader = new BeatSaverReader();

            //var SSReader = new ScoreSaberReader();

            //var diffs = ssongs.Where(s => s.RankedDifficulties.Count > 0).OrderByDescending(s => s.RankedDifficulties.Max(d => d.Value)).Select(s => s.hash).ToList();
            ////var maybe = diffs.Where(s => s.id == 6602).ToList();
            //var ranked = ssongs.Where(s => s.ScoreSaberInfo.Values.Where(ss => ss.ranked == true).Count() > 0).Select(s => s.hash).ToList();
            //var difference = ranked.Where(r => !diffs.Contains(r)).ToList();
            //var difSongs = ssongs.Where(s => difference.Contains(s.hash)).ToList();
            //var scoreSaberSongs = SSReader.GetTopPPSongs(new ScoreSaberFeedSettings(0) { MaxPages = 1 });


            //var newSongs = reader.GetNewestSongs(new BeatSaverFeedSettings((int) BeatSaverFeeds.LATEST) { MaxPages = 2 });          
        }

        static void Main(string[] args)
        {

            Logger.LogLevel = Config.StrToLogLevel(Config.LoggingLevel);
            Logger.fileWriter.AutoFlush = true;
            Logger.ShortenSourceName = true;
            try
            {
                try
                {
                    Config.Initialize();
                }
                catch (FileNotFoundException ex)
                {
                    Logger.Exception("Error initializing Config", ex);
                }
                ScrapedDataProvider.Initialize();
                //Tests();
                try
                {
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
                }
                catch (Exception ex)
                {
                    Logger.Exception($"Error parsing command line arguments", ex);
                }


                if (!Config.CriticalError)
                {
                    WebUtils.Initialize(Config.MaxConcurrentPageChecks);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    SyncSaber ss = new SyncSaber();

                    /*
                    ss.DownloadSongsFromFeed(BeatSaverReader.NameKey, new BeatSaverFeedSettings(1) {
                        MaxPages = 5
                    });

                    ss.DownloadSongsFromFeed(BeatSaverReader.NameKey, new BeatSaverFeedSettings(0) {
                        Authors = Config.FavoriteMappers.ToArray(),
                    });
                    */
                    Console.WriteLine();
                    if (Config.SyncFavoriteMappersFeed && Config.FavoriteMappers.Count > 0)
                    {
                        Logger.Info($"Downloading songs from FavoriteMappers.ini...");
                        try
                        {
                            ss.DownloadSongsFromFeed(BeatSaverReader.NameKey, new BeatSaverFeedSettings(0) {
                                Authors = Config.FavoriteMappers.ToArray()
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception("Exception downloading BeatSaver authors feed.", ex);
                        }
                    }
                    else
                    {
                        Logger.Warning($"Skipping FavoriteMappers.ini feed, no authors found in {Config.BeatSaberPath + @"\UserData\FavoriteMappers.ini"}");
                    }

                    if (Config.SyncFollowingsFeed)
                    {
                        // Followings
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[BeastSaberFeeds.FOLLOWING].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(0, Web.BeastSaberReader.GetMaxBeastSaberPages(0));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(0) {
                                MaxPages = Config.MaxFollowingsPages
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed: Following", ex);
                        }
                    }
                    // Bookmarks
                    if (Config.SyncBookmarksFeed)
                    {
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[BeastSaberFeeds.BOOKMARKS].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(1, Web.BeastSaberReader.GetMaxBeastSaberPages(1));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(1) {
                                MaxPages = Config.MaxBookmarksPages
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed: Bookmarks", ex);
                        }
                    }
                    if (Config.SyncCuratorRecommendedFeed)
                    {
                        // Curator Recommended
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[BeastSaberFeeds.CURATOR_RECOMMENDED].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(2, Web.BeastSaberReader.GetMaxBeastSaberPages(2));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(2) {
                                MaxPages = Config.MaxCuratorRecommendedPages
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed: Curator Recommended", ex);
                        }
                    }

                    if (Config.SyncTopPPFeed)
                    {
                        // ScoreSaber Top PP
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {ScoreSaberReader.Feeds[ScoreSaberFeeds.TOP_RANKED].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(2, Web.BeastSaberReader.GetMaxBeastSaberPages(2));
                            ss.DownloadSongsFromFeed(ScoreSaberReader.NameKey, new ScoreSaberFeedSettings((int) ScoreSaberFeeds.TOP_RANKED) {
                                MaxPages = Config.MaxScoreSaberPages,
                                searchOnline = false
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading ScoreSaberFeed: Top Ranked", ex);
                        }
                    }
                    /*
                    Console.WriteLine();
                    Logger.Info($"Downloading newest songs on Beat Saver...");
                    try
                    {
                        ss.DownloadSongsFromFeed(BeatSaverReader.NameKey, new BeatSaverFeedSettings(1) {
                            MaxPages = Config.MaxBeatSaverPages
                        });
                    }

                    catch (Exception ex)
                    {
                        Logger.Exception("Exception downloading BeatSaver newest feed.", ex);
                    }
                    */

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
            }
            catch(OutOfDateException ex)
            {
                Logger.Error(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Exception("Uncaught exception in Main()", ex);
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }


    }

}
