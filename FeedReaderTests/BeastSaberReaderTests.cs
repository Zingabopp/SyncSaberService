using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;

namespace FeedReaderTests
{
    [TestClass]
    public class BeastSaberReaderTests
    {
        [TestMethod]
        public void GetSongsFromPage_XML_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            var text = File.ReadAllText("Data\\BeastSaberXMLPage.xml");
            var songList = reader.GetSongsFromPageText(text, BeastSaberReader.ContentType.XML);
            Assert.IsTrue(songList.Count == 50);
            var firstHash = "74575254ae759f3f836eb521b4b80093ca52cd3d".ToUpper();
            var firstUrl = "https://beatsaver.com/api/download/key/56ff";
            Assert.IsTrue(songList.First().Hash == firstHash);
            Assert.IsTrue(songList.First().DownloadUrl == firstUrl);
            var lastHash = "e3487474b70d969927e459a1590e93b7ad25a436".ToUpper();
            var lastUrl = "https://beatsaver.com/api/download/key/5585";
            Assert.IsTrue(songList.Last().Hash == lastHash);
            Assert.IsTrue(songList.Last().DownloadUrl == lastUrl);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Hash)));
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.DownloadUrl)));
        }

        [TestMethod]
        public void GetSongsFromPage_JSON_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            var text = File.ReadAllText("Data\\BeastSaberJsonPage.json");
            var songList = reader.GetSongsFromPageText(text, BeastSaberReader.ContentType.JSON);
            Assert.IsTrue(songList.Count == 20);
            var firstHash = "a3bbbe2d6f64dfe8324c7098d5c35281d21fd20f".ToUpper();
            var firstUrl = "https://beatsaver.com/api/download/key/5679";
            Assert.IsTrue(songList.First().Hash == firstHash);
            Assert.IsTrue(songList.First().DownloadUrl == firstUrl);
            var lastHash = "20b9326bd71db4454aba08df06b035ea536322a9".ToUpper();
            var lastUrl = "https://beatsaver.com/api/download/key/55d1";
            Assert.IsTrue(songList.Last().Hash == lastHash);
            Assert.IsTrue(songList.Last().DownloadUrl == lastUrl);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Hash)));
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.DownloadUrl)));
        }

        [TestMethod]
        public void GetSongsFromFeed_Bookmarks_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.BOOKMARKS) { MaxSongs = maxSongs, searchOnline = true };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeed_Followings_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.FOLLOWING) { MaxSongs = maxSongs, searchOnline = true };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeed_CuratorRecommended_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.CURATOR_RECOMMENDED) { MaxSongs = maxSongs, searchOnline = true };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Value)));
        }


        [TestMethod]
        public void GetSongsFromFeedAsync_Bookmarks_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.BOOKMARKS) { MaxSongs = maxSongs, searchOnline = true };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_Followings_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.FOLLOWING) { MaxSongs = maxSongs, searchOnline = true };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count == maxSongs);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_CuratorRecommended_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.CURATOR_RECOMMENDED) { MaxSongs = maxSongs, searchOnline = true };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Value)));
        }

    }
}
