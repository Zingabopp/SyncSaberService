using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SyncSaberLib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberLib.Data.Tests
{
    [TestClass()]
    public class BeatSaverSongTests
    {
        [TestMethod()]
        public void TryParseBeatSaverTest()
        {
            string jsonStr = File.ReadAllText("test_detail_page.json");
            JToken singleSong = JToken.Parse(jsonStr);
            bool successful = BeatSaverSong.TryParseBeatSaver(singleSong, out BeatSaverSong song);
            Console.WriteLine($"{song.name}");
            Assert.IsTrue(successful);
        }
    }
}