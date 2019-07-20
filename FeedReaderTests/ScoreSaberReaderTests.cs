using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;
using Newtonsoft.Json.Linq;

namespace FeedReaderTests
{
    [TestClass]
    public class ScoreSaberReaderTests
    {
        static ScoreSaberReaderTests()
        {
            if (!WebUtils.IsInitialized)
                WebUtils.Initialize();
        }

        [TestMethod]
        public void GetSongsFromFeed_Trending()
        {
            var reader = new ScoreSaberReader();
            int maxSongs = 100;
            var settings = new ScoreSaberFeedSettings((int)ScoreSaberFeeds.TRENDING) { MaxSongs = maxSongs, SongsPerPage = 40, RankedOnly = true };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Keys.Any(k => string.IsNullOrEmpty(k)));
        }

        [TestMethod]
        public void GetSongsFromPageText()
        {
            var reader = new ScoreSaberReader() { StoreRawData = true };
            var pageText = File.ReadAllText("Data\\ScoreSaberPage.json");
            var songList = reader.GetSongsFromPageText(pageText, "");
            Assert.IsTrue(songList.Count == 50);
            var firstHash = "0597F8F7D8E396EBFEF511DC9EC98B69635CE532";
            Assert.IsTrue(songList.First().Hash == firstHash);
            var firstRawData = JToken.Parse(songList.First().RawData);
            Assert.IsTrue(firstRawData["uid"]?.Value<int>() == 143199);
            var lastHash = "F369747C6B54914DEAA163AAE85816BA5A8C1845";
            Assert.IsTrue(songList.Last().Hash == lastHash);
        }

    }
}
