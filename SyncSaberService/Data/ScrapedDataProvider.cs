using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace SyncSaberService.Data
{
    public static class ScrapedDataProvider
    {
        public static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly DirectoryInfo DATA_DIRECTORY =  new DirectoryInfo(Path.Combine(ASSEMBLY_PATH, "ScrapedData"));
        public static readonly FileInfo BEATSAVER_SCRAPE_PATH =
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

        public static List<SongInfo> ReadDefaultScraped()
        {
            return ReadScrapedFile(BEATSAVER_SCRAPE_PATH.FullName);
        }

        public static async Task<List<SongInfo>> ReadDefaultScrapedAsync()
        {
            return await Task.Run(() => ReadDefaultScraped());
        }

    }
    // From: https://stackoverflow.com/questions/43747477/how-to-parse-huge-json-file-as-stream-in-json-net?rq=1
    public static class JsonReaderExtensions
    {
        public static IEnumerable<T> SelectTokensWithRegex<T>(
            this JsonReader jsonReader, Regex regex)
        {
            JsonSerializer serializer = new JsonSerializer();
            while (jsonReader.Read())
            {
                if (regex.IsMatch(jsonReader.Path)
                    && jsonReader.TokenType != JsonToken.PropertyName)
                {
                    yield return serializer.Deserialize<T>(jsonReader);
                }
            }
        }
    }
}
