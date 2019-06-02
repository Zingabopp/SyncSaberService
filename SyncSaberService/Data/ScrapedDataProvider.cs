using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace SyncSaberService.Data
{
    public static class ScrapedDataProvider
    {
        public static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly DirectoryInfo DATA_DIRECTORY =  new DirectoryInfo(Path.Combine(ASSEMBLY_PATH, "ScrapedData"));
        private static readonly FileInfo BEATSAVER_SCRAPE_PATH =
            new FileInfo(Path.Combine(ASSEMBLY_PATH, DATA_DIRECTORY.FullName, "combinedScrappedData.json"));
        private static List<SongInfo> _beatSaverScrape;
        public static List<SongInfo> BeatSaverScrape
        {
            get
            {
                if (_beatSaverScrape == null)
                    _beatSaverScrape = ReadScrapedFile(BEATSAVER_SCRAPE_PATH.FullName);
                return _beatSaverScrape;
            }
        }

        public static List<SongInfo> ReadScrapedFile(string filePath)
        {
            List<SongInfo> results = null;
            BEATSAVER_SCRAPE_PATH.Refresh();
            if (BEATSAVER_SCRAPE_PATH.Exists)
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    results = (List<SongInfo>) serializer.Deserialize(file, typeof(List<SongInfo>));
                }

            return results;
        }

    }
}
