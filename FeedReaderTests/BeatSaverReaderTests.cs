using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;
using Newtonsoft.Json.Linq;
using System;

namespace FeedReaderTests
{
    [TestClass]
    public class BeatSaverReaderTests
    {
        static BeatSaverReaderTests()
        {
            if (!WebUtils.IsInitialized)
                WebUtils.Initialize();
        }

        [TestMethod]
        public void GetSongsFromFeed_Authors_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.LATEST) { MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            var authorList = new string[] { "BlackBlazon", "greatyazer" };
            settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.AUTHOR) { Authors = authorList, MaxSongs = 50 };
            var songsByAuthor = reader.GetSongsFromFeed(settings);
            var detectedAuthors = songsByAuthor.Values.Select(s => s.MapperName.ToLower()).Distinct();
            foreach (var song in songsByAuthor)
            {
                Assert.IsTrue(!string.IsNullOrEmpty(song.Value.DownloadUrl));
                Assert.IsTrue(authorList.Any(a => a.ToLower() == song.Value.MapperName.ToLower()));

            }
            foreach (var author in authorList)
            {
                Assert.IsTrue(songsByAuthor.Any(s => s.Value.MapperName.ToLower() == author.ToLower()));
            }

            // BlackBlazon check
            var blazonHash = "58de2d709a45b68fdb1dbbfefb187f59f629bfc5".ToUpper();
            var blazonSong = songsByAuthor[blazonHash];
            Assert.IsTrue(blazonSong != null);
            Assert.IsTrue(!string.IsNullOrEmpty(blazonSong.DownloadUrl));
            // GreatYazer check
            var songHash = "bf8c016dc6b9832ece3030f05277bbbe67db790d".ToUpper();
            var yazerSong = songsByAuthor[songHash];
            Assert.IsTrue(yazerSong != null);
            Assert.IsTrue(!string.IsNullOrEmpty(yazerSong.DownloadUrl));
            foreach (var song in songList.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void GetSongsFromFeed_Newest_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.LATEST) { MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == settings.MaxSongs);
            foreach (var song in songList.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void GetSongsFromFeed_Hot_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.HOT) { MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == settings.MaxSongs);
            foreach (var song in songList.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        [TestMethod]
        public void GetSongsFromFeed_Plays_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.PLAYS) { MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == settings.MaxSongs);
            foreach (var song in songList.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        [TestMethod]
        public void GetSongsFromFeed_Downloads_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.DOWNLOADS) { MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == settings.MaxSongs);
            foreach (var song in songList.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void GetSongsFromFeed_Search_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.SEARCH) { MaxSongs = 50, SearchCriteria = "Believer" };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count > 0);
            foreach (var song in songList.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
    }
}
