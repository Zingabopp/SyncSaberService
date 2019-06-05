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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncSaberService
{
    class Program
    {
        private static void Tests()
        {
            ScrapedDataProvider.Initialize();
            var thing = new SongInfo();
            Web.WebUtils.Initialize(5);
            var reader = new BeatSaverReader();
            var ssongs = ScrapedDataProvider.SyncSaberScrape.OrderByDescending(s => s.id).ToList();
            var beatSaverTop = BeatSaverReader.GetSongsFromPage("https://beatsaver.com/api/songs/top/");
            var believer = ScrapedDataProvider.SyncSaberScrape.Where(s => s.songName.ToLower().Contains("believer"));
            //var SSReader = new ScoreSaberReader();
            //ScoreSaberReader.ScrapeScoreSaber(3000, 500);
            var diffs = ssongs.Where(s => s.RankedDifficulties.Count > 0).OrderByDescending(s => s.RankedDifficulties.Max(d => d.Value)).Select(s => s.hash).ToList();
            //var maybe = diffs.Where(s => s.id == 6602).ToList();
            var ranked = ssongs.Where(s => s.ScoreSaberInfo.Values.Where(ss => ss.ranked == true).Count() > 0).Select(s => s.hash).ToList();
            var difference = ranked.Where(r => !diffs.Contains(r)).ToList();
            var difSongs = ssongs.Where(s => difference.Contains(s.hash)).ToList();
            //var scoreSaberSongs = SSReader.GetTopPPSongs(new ScoreSaberFeedSettings(0) { MaxPages = 1 });
            //var scrapeSongs = BeatSaverReader.ScrapeBeatSaver(500, 0);
            ScrapedDataProvider.UpdateScrapedFile();
            var newSongs = reader.GetNewestSongs(new BeatSaverFeedSettings((int) BeatSaverFeeds.NEWEST) { MaxPages = 2 });
            var authors = BeatSaverReader.GetAuthorNamesByID("1089");
            ScrapedDataProvider.TryGetSongByHash("ea6b61e0af09755d77b8c9c2f006bd39", out SongInfo testSong);
            ScrapedDataProvider.TryGetSongByHash("ea6b61e0af09755d77b8c9c2f006bd39", out testSong);
            Stopwatch timer = new Stopwatch();
            Thread.Sleep(500);
            timer.Start();
            var scrapedTask = ScrapedDataProvider.ReadDefaultScrapedAsync();
            timer.Stop();
            Logger.Warning(timer.ElapsedMilliseconds.ToString());
            scrapedTask.Wait();
            var testList = scrapedTask.Result.GroupBy(s => s.authorName);
            var duplicates = scrapedTask.Result.Where(g => g.hash.ToUpper().Contains("02A0C85355C635850A1F7B6B"));
            var testDict = testList.ToDictionary(grouping => grouping.Key, group => group.ToList());
            Logger.Warning(duplicates.FirstOrDefault().Identifier);
            //using(StreamReader file = File.OpenText(@"C:\Users\Jared\source\repos\SyncSaberService\SyncSaberService\bin\Debug\ScrapedData\combinedScrappedData.json"))
            //{
            //    JsonSerializer serializer = new JsonSerializer();
            //    scrapedDict = (List<SongInfo>) serializer.Deserialize(file, typeof(List<SongInfo>));
            //}
            DownloadJob testJob = new DownloadJob(new SongInfo("111-111", "testName", "", "testAuthor"), "temp", "CustomSongs");

            var testTask = testJob.RunJobAsync();
            testTask.Wait();
            var searchTest = BeatSaverReader.Search("6A097D39A5FA94F3B736E6EEF5A519A2", BeatSaverReader.SearchType.hash);
            var testReader = new ScoreSaberReader();
            var sssongs = ScoreSaberReader.GetSSSongsFromPage(WebUtils.GetPageText("https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=5&page=39&ranked=1"));
            foreach (var sssong in sssongs)
            {
                //sssong.PopulateFields();
            }
            var songs = testReader.GetSongsFromFeed(new ScoreSaberFeedSettings((int)ScoreSaberFeeds.TOP_RANKED) {
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
                Tests();
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
                    Web.WebUtils.Initialize(Config.MaxConcurrentPageChecks);
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
                            ss.DownloadSongsFromFeed(ScoreSaberReader.NameKey, new ScoreSaberFeedSettings((int)ScoreSaberFeeds.TOP_RANKED) {
                                MaxPages = Config.MaxScoreSaberPages
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
            catch (Exception ex)
            {
                Logger.Exception("Uncaught exception in Main()", ex);
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }


    }
}
