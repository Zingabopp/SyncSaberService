using Microsoft.VisualStudio.TestTools.UnitTesting;
using SyncSaberService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberService.Tests
{
    [TestClass()]
    public class SongInfoTests
    {
        [TestMethod()]
        public void SongInfoTest_ValidID()
        {
            SongInfo testSong = new SongInfo("1234-23", "Test Song", "http://testurl.com", "TestAuthor");
            Assert.IsTrue(testSong.id == 1234);
            Assert.IsTrue(testSong.SongVersion == 23);
        }

        [TestMethod()]
        public void SongInfoTest_InvalidID_SingleNum()
        {
            SongInfo testSong = new SongInfo("1234", "Test Song", "http://testurl.com", "TestAuthor");
            Assert.IsTrue(testSong.id == 0);
            Assert.IsTrue(testSong.SongVersion == 0);
        }

        [TestMethod()]
        public void SongInfoTest_InvalidID_HasLetters()
        {
            SongInfo testSong = new SongInfo("1234s-23", "Test Song", "http://testurl.com", "TestAuthor");
            Assert.IsTrue(testSong.id == 0);
            Assert.IsTrue(testSong.SongVersion == 0);
        }

        [TestMethod()]
        public void SongInfoTest_InvalidID_EmptyString()
        {
            SongInfo testSong = new SongInfo("", "Test Song", "http://testurl.com", "TestAuthor");
            Assert.IsTrue(testSong.id == 0);
            Assert.IsTrue(testSong.SongVersion == 0);
        }

        [TestMethod()]
        public void SongInfoTest_InvalidID_NoID()
        {
            SongInfo testSong = new SongInfo("-123", "Test Song", "http://testurl.com", "TestAuthor");
            Assert.IsTrue(testSong.id == 0);
            Assert.IsTrue(testSong.SongVersion == 0);
        }

        [TestMethod()]
        public void SongInfoTest_InvalidID_NoVersion()
        {
            SongInfo testSong = new SongInfo("1234-", "Test Song", "http://testurl.com", "TestAuthor");
            Assert.IsTrue(testSong.id == 0);
            Assert.IsTrue(testSong.SongVersion == 0);
        }
    }
}