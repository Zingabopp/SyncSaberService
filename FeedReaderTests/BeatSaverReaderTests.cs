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
        #region Web
        [TestMethod]
        public void GetSongsFromFeed_Authors_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var authorList = new string[] { "BlackBlazon", "greatyazer" };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Author) { Authors = authorList, MaxSongs = 59 };
            var songsByAuthor = reader.GetSongsFromFeed(settings);
            var detectedAuthors = songsByAuthor.Values.Select(s => s.MapperName.ToLower()).Distinct();
            foreach (var song in songsByAuthor)
            {
                Assert.IsTrue(song.Value.DownloadUri != null);
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
            Assert.IsTrue(blazonSong.DownloadUri != null);
            // GreatYazer check
            var songHash = "bf8c016dc6b9832ece3030f05277bbbe67db790d".ToUpper();
            var yazerSong = songsByAuthor[songHash];
            Assert.IsTrue(yazerSong != null);
            Assert.IsTrue(yazerSong.DownloadUri != null);
        }

        [TestMethod]
        public void GetSongsFromFeed_Newest_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Latest) { MaxSongs = 50 };
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
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Hot) { MaxSongs = 50 };
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
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Plays) { MaxSongs = 50 };
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
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Downloads) { MaxSongs = 50 };
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
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Search) { MaxSongs = 50, SearchCriteria = "Believer" };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count > 0);
            foreach (var song in songList.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        #endregion

        [TestMethod]
        public void ParseSongsFromPage_Test()
        {
            string pageText = File.ReadAllText(@"Data\BeatSaverListPage.json");
            Uri uri = null;
            var songs = BeatSaverReader.ParseSongsFromPage(pageText, uri);
            Assert.IsTrue(songs.Count == 10);
            foreach (var song in songs)
            {
                Assert.IsFalse(song.DownloadUri == null);
                Assert.IsFalse(string.IsNullOrEmpty(song.Hash));
                Assert.IsFalse(string.IsNullOrEmpty(song.MapperName));
                Assert.IsFalse(string.IsNullOrEmpty(song.RawData));
                Assert.IsFalse(string.IsNullOrEmpty(song.SongName));
            }
            var firstSong = JObject.Parse(songs.First().RawData);
            string firstHash = firstSong["hash"]?.Value<string>();
            Assert.IsTrue( firstHash == "27639680f92a9588b7cce843fc7aaa0f5dc720f8");
            string firstUploader = firstSong["uploader"]?["username"]?.Value<string>();
            Assert.IsTrue(firstUploader == "latte");
        }
    }
}
