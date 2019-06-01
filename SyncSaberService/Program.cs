using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;
using SyncSaberService.Web;
using SyncSaberService.Data;

namespace SyncSaberService
{
    class Program
    {
        private static void Tests()
        {
            Web.HttpClientWrapper.Initialize(5);

            DownloadJob testJob = new DownloadJob(new SongInfo("111-111", "testName", "", "testAuthor"), "temp", "CustomSongs");

            var testTask = testJob.RunJobAsync();
            testTask.Wait();
            var searchTest = BeatSaverReader.Search("6A097D39A5FA94F3B736E6EEF5A519A2", BeatSaverReader.SearchType.hash);
            var testReader = new ScoreSaberReader();
            var sssongs = testReader.GetSSSongsFromPage(HttpClientWrapper.GetPageText("https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=5&page=39&ranked=1"));
            foreach (var sssong in sssongs)
            {
                sssong.PopulateFields();
            }
            var songs = testReader.GetSongsFromFeed(new ScoreSaberFeedSettings(0) {
                MaxPages = 10
            });
            
            SongInfo song = new SongInfo("18750-20381", "test", "testUrl", "testAuthor");
            song.PopulateFields();
            var test = song["key"];
            var test2 = song["id"];
            var test3 = song["uploaderId"];
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
                    Web.HttpClientWrapper.Initialize(Config.MaxConcurrentPageChecks);
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
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[0].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(0, Web.BeastSaberReader.GetMaxBeastSaberPages(0));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(0) {
                                MaxPages = Config.MaxFollowingsPages
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed (0)", ex);
                        }
                    }
                    // Bookmarks
                    if (Config.SyncBookmarksFeed)
                    {
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[1].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(1, Web.BeastSaberReader.GetMaxBeastSaberPages(1));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(1) {
                                MaxPages = Config.MaxBookmarksPages
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed (1)", ex);
                        }
                    }
                    if (Config.SyncCuratorRecommendedFeed)
                    {
                        // Curator Recommended
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {BeastSaberReader.Feeds[2].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(2, Web.BeastSaberReader.GetMaxBeastSaberPages(2));
                            ss.DownloadSongsFromFeed(BeastSaberReader.NameKey, new BeastSaberFeedSettings(2) {
                                MaxPages = Config.MaxCuratorRecommendedPages
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading BeastSaberFeed (2)", ex);
                        }
                    }

                    if (Config.SyncTopPPFeed)
                    {
                        // ScoreSaber Top PP
                        Console.WriteLine();
                        Logger.Info($"Downloading songs from {ScoreSaberReader.Feeds[0].Name} feed...");
                        try
                        {
                            //ss.DownloadBeastSaberFeed(2, Web.BeastSaberReader.GetMaxBeastSaberPages(2));
                            ss.DownloadSongsFromFeed(ScoreSaberReader.NameKey, new ScoreSaberFeedSettings(0) {
                                MaxPages = Config.MaxScoreSaberPages
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception($"Exception downloading ScoreSaberFeed (0)", ex);
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
            catch (Exception ex)
            {
                Logger.Exception("Uncaught exception in Main()", ex);
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }


    }
}
