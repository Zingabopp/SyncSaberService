using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;
using Newtonsoft.Json.Linq;

namespace FeedReaderTests.MockClasses.MockTests
{
    [TestClass]
    public class MockStaticTests
    {
        [TestMethod]
        public void GetFileForUrl_BeastSaber_Bookmarked()
        {
            string testUrl = @"https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=Zingabopp&page=2&count=15";
            string fileMatch = "bookmarked_by_zingabopp2.json";
            var file = new FileInfo(MockHttpResponse.GetFileForUrl(testUrl));
            Assert.AreEqual(file.Name, fileMatch);

            testUrl = @"https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=Zingabopp&page=3&count=15";
            fileMatch = "bookmarked_by_zingabopp3_empty.json";
            file = new FileInfo(MockHttpResponse.GetFileForUrl(testUrl));
            Assert.AreEqual(file.Name, fileMatch);
        }

        [TestMethod]
        public void GetFileForUrl_BeastSaber_Followings()
        {
            string testUrl = @"https://bsaber.com/members/zingabopp/wall/followings/feed/?acpage=1&count=20";
            string fileMatch = "followings1.xml";
            var file = new FileInfo(MockHttpResponse.GetFileForUrl(testUrl));
            Assert.AreEqual(file.Name, fileMatch);

            testUrl = @"https://bsaber.com/members/zingabopp/wall/followings/feed/?acpage=8";
            fileMatch = "followings8_partial.xml";
            file = new FileInfo(MockHttpResponse.GetFileForUrl(testUrl));
            Assert.AreEqual(file.Name, fileMatch);
        }

        [TestMethod]
        public void GetFileForUrl_BeastSaber_Curator()
        {
            string testUrl = @"https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=1&count=50";
            string fileMatch = "bookmarked_by_curator1.json";
            var file = new FileInfo(MockHttpResponse.GetFileForUrl(testUrl));
            Assert.AreEqual(file.Name, fileMatch);

            testUrl = @"https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=4";
            fileMatch = "bookmarked_by_curator4_partial.json";
            file = new FileInfo(MockHttpResponse.GetFileForUrl(testUrl));
            Assert.AreEqual(file.Name, fileMatch);
        }
    }
}
