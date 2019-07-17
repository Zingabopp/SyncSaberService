using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;
using Newtonsoft.Json.Linq;

namespace FeedReaderTests
{
    [TestClass]
    public class BeastSaberReaderTests
    {
        static BeastSaberReaderTests()
        {
            if (!WebUtils.IsInitialized)
                WebUtils.Initialize();
        }

        [TestMethod]
        public void GetSongsFromPage_XML_Test()
        {

            var reader = new BeastSaberReader("Zingabopp", 3) { StoreRawData = true };
            var text = File.ReadAllText("Data\\BeastSaberXMLPage.xml");
            var songList = reader.GetSongsFromPageText(text, BeastSaberReader.ContentType.XML);
            Assert.IsTrue(songList.Count == 50);
            var firstHash = "74575254ae759f3f836eb521b4b80093ca52cd3d".ToUpper();
            var firstKey = "56ff";
            var firstLevelAuthor = "Rustic";
            var firstTitle = "Xilent – Code Blood";
            var firstDownloadUrl = "https://beatsaver.com/api/download/key/56ff";
            var firstUrl = "https://beatsaver.com/api/download/key/56ff";
            var firstSong = songList.First();
            Assert.IsTrue(firstSong.Hash == firstHash);
            Assert.IsTrue(firstSong.DownloadUrl == firstUrl);
            // Raw Data test
            JToken firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["Hash"]?.Value<string>().ToUpper() == firstHash);
            Assert.IsTrue(firstRawData["SongKey"]?.Value<string>() == firstKey);
            Assert.IsTrue(firstRawData["LevelAuthorName"]?.Value<string>() == firstLevelAuthor);
            Assert.IsTrue(firstRawData["SongTitle"]?.Value<string>() == firstTitle);
            Assert.IsTrue(firstRawData["DownloadURL"]?.Value<string>() == firstDownloadUrl);



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
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.BOOKMARKS) { MaxSongs = maxSongs };
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
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.FOLLOWING) { MaxSongs = maxSongs };
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
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.CURATOR_RECOMMENDED) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Value.DownloadUrl)));
        }


        [TestMethod]
        public void GetSongsFromFeedAsync_Bookmarks_UnlimitedSongs_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.BOOKMARKS) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_Bookmarks_LimitedSongs_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.BOOKMARKS) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_Followings_UnlimitedSongs_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3) { StoreRawData = true };
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.FOLLOWING) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count != 0);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_Followings_LimitedSongs_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3) { StoreRawData = true };
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.FOLLOWING) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count == maxSongs);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_CuratorRecommended_UnlimitedSongs_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3) { StoreRawData = true };
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.CURATOR_RECOMMENDED) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count != 0);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Value.DownloadUrl)));
            var firstSong = songList.First().Value;
            var firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["hash"]?.Value<string>().ToUpper() == firstSong.Hash);
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_CuratorRecommended_LimitedSongs_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", 3) { StoreRawData = true };
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeeds.CURATOR_RECOMMENDED) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Value.DownloadUrl)));
            var firstSong = songList.First().Value;
            var firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["hash"]?.Value<string>().ToUpper() == firstSong.Hash);
        }

    }
}
