using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using SyncSaberLib.Web;

namespace SyncSaberLib.Data
{
    public class BeatSaverScrape : IScrapedDataModel<List<BeatSaverSong>, BeatSaverSong>
    {
        private bool _initialized;
        private readonly object dataLock = new object();
        //[JsonProperty("Data")]
        //public List<SongInfoEnhanced> Data { get; private set; }

        public BeatSaverScrape()
        {
            DefaultPath = Path.Combine(DATA_DIRECTORY.FullName, "BeatSaverScrape.json");
            _initialized = false;
            Data = new List<BeatSaverSong>();

        }

        public override void Initialize(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = DefaultPath;

            //(filePath).Populate(this);
            if (File.Exists(filePath))
                ReadScrapedFile(filePath).Populate(Data);
            //JsonSerializer serializer = new JsonSerializer();
            //if (test.Type == Newtonsoft.Json.Linq.JTokenType.Array)
            //    Data = test.ToObject<List<SongInfo>>();
            _initialized = true;
            CurrentFile = new FileInfo(filePath);
        }
        /*
        public void AddOrUpdate(BeatSaverSong newSong)
        {
            IEnumerable<BeatSaverSong> existing = null;
            lock (dataLock)
            {
                existing = Data.Where(s => s.Equals(newSong));
            }
            if (existing.Count() > 1)
            {
                Logger.Warning("Duplicate hash in BeatSaverScrape, this shouldn't happen");
            }
            if (existing.SingleOrDefault() != null)
            {
                if (existing.Single().ScrapedAt < newSong.ScrapedAt)
                {
                    lock (dataLock)
                    {
                        Data.Remove(existing.Single());
                        Data.Add(newSong);
                    }
                }
            }
        }
        */
    }
}
