using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace FeedReaderTests.MockClasses.MockTests
{
    [TestClass]
    public class MockHttpContentTests
    {
        public static readonly string DownloadPath = "MockDownloads";
        [TestMethod]
        public void ReadAsStringAsync_Test()
        {
            using (var mockContent = new MockHttpContent(@"https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=Zingabopp&page=2&count=15"))
            {
                var expectedString = File.ReadAllText(mockContent.FileSourcePath);
                var actualString = mockContent.ReadAsStringAsync().Result;
                Assert.AreEqual(expectedString, actualString);
            }
        }

        [TestMethod]
        public void ReadAsFile_Test()
        {
            using (var mockContent = new MockHttpContent(@"https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=Zingabopp&page=2&count=15"))
            {
                var expectedString = File.ReadAllText(mockContent.FileSourcePath);
                var dirPath = new DirectoryInfo(DownloadPath);
                var destPath = Path.Combine(dirPath.FullName, Path.GetFileName(mockContent.FileSourcePath));
                mockContent.ReadAsFileAsync(destPath, true).Wait();
                var actualString = mockContent.ReadAsStringAsync().Result;
                Assert.AreEqual(expectedString, actualString);
                AssertAsync.ThrowsExceptionAsync<InvalidOperationException>(async () => await mockContent.ReadAsFileAsync(destPath, false)).Wait();
            }
        }

        [TestMethod]
        public void ContentType_Test()
        {
            using (var mockContent = new MockHttpContent(@"https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=Zingabopp&page=2&count=15"))
            {
                var expectedContentType = @"application/json";
                Assert.AreEqual(mockContent.ContentType, expectedContentType);
            }
            using (var mockContent = new MockHttpContent(@"https://bsaber.com/members/zingabopp/wall/followings/feed/?acpage=1&count=20"))
            {
                var expectedContentType = @"text/xml";
                Assert.AreEqual(mockContent.ContentType, expectedContentType);
            }

        }
    }
}
