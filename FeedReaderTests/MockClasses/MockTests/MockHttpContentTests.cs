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
            var dataDirectory = @"Data\BeastSaber";
            var jsonFile = Path.Combine(dataDirectory, "bookmarked_by_zingabopp1.json");
            using (var mockContent = new MockHttpContent(jsonFile))
            {
                var expectedString = File.ReadAllText(mockContent.FileSourcePath);
                var actualString = mockContent.ReadAsStringAsync().Result;
                Assert.AreEqual(expectedString, actualString);
            }
        }

        [TestMethod]
        public void ReadAsStreamAsync_Test()
        {
            var dataDirectory = @"Data\BeastSaber";
            var jsonFile = Path.Combine(dataDirectory, "bookmarked_by_zingabopp1.json");
            using (var mockContent = new MockHttpContent(jsonFile))
            using (var actualStream = mockContent.ReadAsStreamAsync().Result)
            using (var expectedStream = File.OpenRead(jsonFile))
            {
                Assert.AreEqual(expectedStream.Length, actualStream.Length);
            }
        }

        [TestMethod]
        public void ReadAsByteArrayAsync_Test()
        {
            var dataDirectory = @"Data\BeastSaber";
            var jsonFile = Path.Combine(dataDirectory, "bookmarked_by_zingabopp1.json");
            using (var mockContent = new MockHttpContent(jsonFile))
            {
                var expectedArray = File.ReadAllBytes(mockContent.FileSourcePath);
                var actualArray = mockContent.ReadAsByteArrayAsync().Result;
                bool notEqual = false;
                Assert.AreEqual(expectedArray.LongLength, actualArray.LongLength);
                Assert.IsTrue(actualArray.LongLength > 0);
                for (long i = 0; i < expectedArray.LongLength; i++)
                {
                    if (expectedArray[i] != actualArray[i])
                        notEqual = true;
                }
                Assert.IsFalse(notEqual);
            }
        }

        [TestMethod]
        public void ReadAsFile_Test()
        {
            var dataDirectory = @"Data\BeastSaber";
            var jsonFile = Path.Combine(dataDirectory, "bookmarked_by_zingabopp1.json");
            using (var mockContent = new MockHttpContent(jsonFile))
            {
                var expectedString = File.ReadAllText(mockContent.FileSourcePath);
                var dirPath = new DirectoryInfo(DownloadPath);
                var destPath = Path.Combine(dirPath.FullName, Path.GetFileName(mockContent.FileSourcePath));
                mockContent.ReadAsFileAsync(destPath, true).Wait();
                var actualString = mockContent.ReadAsStringAsync().Result;
                Assert.AreEqual(expectedString, actualString);
                AssertAsync.ThrowsExceptionAsync<InvalidOperationException>(async () => await mockContent.ReadAsFileAsync(destPath, false).ConfigureAwait(false)).Wait();
            }
        }

        [TestMethod]
        public void ContentType_Test()
        {
            var dataDirectory = @"Data\BeastSaber";
            var jsonFile = Path.Combine(dataDirectory, "bookmarked_by_zingabopp1.json");
            var xmlFile = Path.Combine(dataDirectory, "followings1.xml");
            using (var mockContent = new MockHttpContent(jsonFile))
            {
                var expectedContentType = @"application/json";
                Assert.AreEqual(expectedContentType, mockContent.ContentType);
            }
            using (var mockContent = new MockHttpContent(xmlFile))
            {
                var expectedContentType = @"text/xml";
                Assert.AreEqual(expectedContentType, mockContent.ContentType);
            }
        }
    }
}
