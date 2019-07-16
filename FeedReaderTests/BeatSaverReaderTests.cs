using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;
using Newtonsoft.Json.Linq;

namespace FeedReaderTests
{
    [TestClass]
    public class BeatSaverReaderTests
    {
        [TestMethod]
        public void GetSongsFromPageText()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.LATEST) { MaxPages = 2, MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);

            settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.AUTHOR) { Authors = new string[] { "BlackBlazon" }, MaxPages = 2, MaxSongs = 50 };
            var songsByAuthor = reader.GetSongsFromFeed(settings);
        }

    }
}
