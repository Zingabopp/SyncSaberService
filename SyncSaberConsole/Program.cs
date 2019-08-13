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
        private static void DoFullScrape()
        {
            BeatSaverReader.ScrapeBeatSaver(400, false);
            ScrapedDataProvider.BeatSaverSongs.WriteFile();
            ScoreSaberReader.ScrapeScoreSaber(5000, 13000, false);
            ScrapedDataProvider.ScoreSaberSongs.WriteFile();
        }

        private static void Tests()
        {
            //ScrapedDataProvider.Initialize();

            WebUtils.Initialize(5);

            ScrapedDataProvider.BeatSaverSongs.WriteFile();
            var bsReader = new BeatSaverReader();
            //var bsSongs = bsReader.GetSongsFromFeed(new BeatSaverFeedSettings((int)BeatSaverFeeds.LATEST) { MaxPages = 10, searchOnline = true });
            var authorSongs = BeatSaverReader.GetSongsByAuthor("ruckasdfus");

            var song = ScrapedDataProvider.Songs.Values.Where(s => s.ScoreSaberInfo.Any(d => d.Value.uid == 155732)).FirstOrDefault();

            var found = ScrapedDataProvider.TryGetSongByKey("3f57", out SongInfo badSong, false);
            var CustomSongsPath = Path.Combine(OldConfig.BeatSaberPath, "Beat Saber_Data", "CustomLevels");
            var tempFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), badSong.key + ".zip"));
            var outputFolder = new DirectoryInfo(Path.Combine(CustomSongsPath, $"{badSong.key} ({Utilities.MakeSafeFilename(badSong.songName)} - {Utilities.MakeSafeFilename(badSong.authorName)})"));
            var job = new DownloadJob(badSong, tempFolder.FullName, outputFolder.FullName);
            job.RunJobAsync().Wait();
            var br = new BeastSaberReader("Zingabopp", 3);
            var text = WebUtils.GetPageText("https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=Zingabopp&page=1");
            var bSongs = br.GetSongsFromPage(text);


            //ScrapedDataProvider.Initialize();


            //ScrapedDataProvider.Initialize();
            var bsScrape = ScrapedDataProvider.BeatSaverSongs;
            var ssScrape = ScrapedDataProvider.ScoreSaberSongs;
            ScrapedDataProvider.TryGetSongByHash("501f6b1bddb2af72abda0f1e6b7b89cb1eb3db67", out SongInfo deletedSong);
            //var job = new DownloadJob(deletedSong, "test.zip", @"ScrapedData\test");
            //var jobTask = job.RunJobAsync();
            //jobTask.Wait();
            bsScrape.AddOrUpdate(null);
            var resp = WebUtils.HttpClient.GetAsync("https://beatsaver.com/api/maps/detail/b");
            Task.WaitAll(resp);

            var rateHeaders = resp.Result.Headers.Where(h => h.Key.StartsWith("Rate-Limit")).ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
            var remoteTime = resp.Result.Headers.Date;
            var rateInfo = WebUtils.ParseRateLimit(rateHeaders);
            Console.WriteLine("Reset Timespan: " + rateInfo.TimeToReset.ToString());
            foreach (var item in resp.Result.Headers)
            {
                Console.WriteLine($"{item.Key}: {string.Join("|", item.Value)}");
            }

            var trending = ScrapedDataProvider.Songs.Values.Where(s => s.ScoreSaberInfo.Count > 0).OrderByDescending(s => s.ScoreSaberInfo.Values.Select(ss => ss.scores).Aggregate((a, b) => a + b)).Take(100);
            var detTrending = trending.Select(s => (s.ScoreSaberInfo.Values.Select(ss => ss.scores).Aggregate((a, b) => a + b), s)).ToList();


        }


        static void Main(string[] args)
        {
            try
            {
                Logger.ShortenSourceName = true;
                try
                {
                    OldConfig.Initialize();
                    Logger.LogLevel = OldConfig.StrToLogLevel(OldConfig.LoggingLevel);
                    if (Logger.LogLevel < LogLevel.Info)
                        Logger.ShortenSourceName = false;
                }
                catch (FileNotFoundException ex)
                {
                    Logger.Exception("Error initializing Config", ex);
                }
                Logger.Info($"Using Beat Saber directory: {OldConfig.BeatSaberPath}");
                ScrapedDataProvider.Initialize();
                Logger.Info($"Scrapes loaded, {ScrapedDataProvider.BeatSaverSongs.Data.Count} BeatSaverSongs and {ScrapedDataProvider.ScoreSaberSongs.Data.Count} ScoreSaber difficulties loaded");
                //DoFullScrape();

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
                                OldConfig.BeatSaberPath = bsDir.FullName;
                                Logger.Info($"Updated Beat Saber directory path to {OldConfig.BeatSaberPath}");
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


                if (!OldConfig.CriticalError)
                {
                    WebUtils.Initialize(OldConfig.MaxConcurrentPageChecks);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    SyncSaber ss = new SyncSaber();
                    ss.ScrapeNewSongs();
                    Console.WriteLine();
                    if (OldConfig.SyncFavoriteMappersFeed && OldConfig.FavoriteMappers.Count > 0)
                    {
                        Logger.Info($"Downloading songs from FavoriteMappers.ini...");
                        try
                        {
                            ss.DownloadSongsFromFeed(BeatSaverReader.NameKey, new BeatSaverFeedSettings(0)
                            {
                                Authors = OldConfig.FavoriteMappers.ToArray()
                            });
                        }
                        catch (AggregateException ae)
                        {
                            ae.WriteExceptions($"Exceptions downloading songs from FavoriteMappers.ini.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception("Exception downloading songs from FavoriteMappers.ini.", ex);
                        }
                    }
                    else
                    {
                        if (OldConfig.SyncFavoriteMappersFeed)
                            Logger.Warning($"Skipping FavoriteMappers.ini feed, no authors found in {Path.Combine(OldConfig.BeatSaberPath, "UserData", "FavoriteMappers.ini")}");
                    }

                    if (OldConfig.SyncFollowingsFeed)
                    {
                        // Followings
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[BeastSaberFeeds.FOLLOWING].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(0, Web.BeastSaberReader.GetMaxBeastSaberPages(0));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(0)
                            {
                                MaxPages = OldConfig.MaxFollowingsPages
                            });
                        }
                        catch (AggregateException ae)
                        {
                            ae.WriteExceptions($"Exceptions downloading songs from BeastSaberFeed: Following.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed: Following", ex);
                        }
                    }
                    // Bookmarks
                    if (OldConfig.SyncBookmarksFeed)
                    {
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[BeastSaberFeeds.BOOKMARKS].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(1, Web.BeastSaberReader.GetMaxBeastSaberPages(1));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(1)
                            {
                                MaxPages = OldConfig.MaxBookmarksPages
                            });
                        }
                        catch (AggregateException ae)
                        {
                            ae.WriteExceptions($"Exceptions downloading songs from BeastSaberFeed: Bookmarks.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed: Bookmarks", ex);
                        }
                    }
                    if (OldConfig.SyncCuratorRecommendedFeed)
                    {
                        // Curator Recommended
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[BeastSaberFeeds.CURATOR_RECOMMENDED].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(2, Web.BeastSaberReader.GetMaxBeastSaberPages(2));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(2)
                            {
                                MaxPages = OldConfig.MaxCuratorRecommendedPages
                            });
                        }
                        catch (AggregateException ae)
                        {
                            ae.WriteExceptions($"Exceptions downloading songs from BeastSaberFeed: Curator Recommended.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed: Curator Recommended", ex);
                        }
                    }

                    if (OldConfig.SyncTopPPFeed)
                    {
                        // ScoreSaber Top PP
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {ScoreSaberReader.Feeds[ScoreSaberFeeds.TOP_RANKED].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(2, Web.BeastSaberReader.GetMaxBeastSaberPages(2));
                            ss.DownloadSongsFromFeed(ScoreSaberReader.NameKey, new ScoreSaberFeedSettings((int)ScoreSaberFeeds.TOP_RANKED)
                            {
                                MaxSongs = 1000,//Config.MaxScoreSaberSongs,
                                SongsPerPage = 10,
                                searchOnline = true
                            });
                        }
                        catch (AggregateException ae)
                        {
                            ae.WriteExceptions($"Exceptions downloading songs from ScoreSaberFeed: Top Ranked.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading ScoreSaberFeed: Top Ranked.", ex);
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
                    Logger.Info($"Finished downloading songs in {(int)processingTime.TotalMinutes} min {processingTime.Seconds} sec");
                }
                else
                {
                    foreach (string e in OldConfig.Errors)
                        Logger.Error($"Invalid setting: {e} = {OldConfig.Setting[e]}");
                }


                ScrapedDataProvider.BeatSaverSongs.WriteFile();
                ScrapedDataProvider.ScoreSaberSongs.WriteFile();
            }
            catch (OutOfDateException ex)
            {
                Logger.Error(ex.Message);
            }
            catch (AggregateException ae)
            {
                ae.WriteExceptions($"Uncaught exceptions in Main()");
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
