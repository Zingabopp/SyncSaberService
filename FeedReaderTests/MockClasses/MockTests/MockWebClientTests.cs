using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using FeedReader;
using WebUtilities;
using Newtonsoft.Json.Linq;

namespace FeedReaderTests.MockClasses.MockTests
{
    [TestClass]
    public class MockWebClientTests
    {
        [TestMethod]
        public void GetAsync_PageNotFound()
        {
            using (var mockClient = new MockWebClient())
            using (var realClient = new HttpClientWrapper())
            {
                var testUrl = "https://bsaber.com/wp-jsoasdfn/bsabasdfer-api/songs/";
                WebUtils.Initialize();
                using (var realResponse = realClient.GetAsync(testUrl).Result)
                using (var mockResponse = mockClient.GetAsync(testUrl).Result)
                {
                    Assert.AreEqual(realResponse.IsSuccessStatusCode, mockResponse.IsSuccessStatusCode);
                    Assert.AreEqual(realResponse.StatusCode, mockResponse.StatusCode);
                    Assert.AreEqual(realResponse.Content.ContentType, mockResponse.Content.ContentType);
                }
            }
        }
    }
}
