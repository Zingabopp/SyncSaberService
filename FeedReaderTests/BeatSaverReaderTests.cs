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
        static BeatSaverReaderTests()
        {
            if (!WebUtils.IsInitialized)
                WebUtils.Initialize();
        }

        [TestMethod]
        public void GetSongsFromFeed_Authors_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.LATEST) { MaxPages = 2, MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            var authorList = new string[] { "BlackBlazon", "greatyazer" };
            settings = new BeatSaverFeedSettings((int)BeatSaverFeeds.AUTHOR) { Authors = authorList, MaxPages = 2, MaxSongs = 50 };
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
        }

        [TestMethod]
        public void GetSongsFromFeed_Newest_Test()
        {

        }
    }
}
