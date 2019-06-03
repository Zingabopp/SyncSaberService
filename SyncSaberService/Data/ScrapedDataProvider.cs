using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using SyncSaberService.Web;

namespace SyncSaberService.Data
{
    public static class ScrapedDataProvider
    {
        private const string SCRAPED_DATA_URL = "https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/combinedScrappedData.json";
        public static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly DirectoryInfo DATA_DIRECTORY = new DirectoryInfo(Path.Combine(ASSEMBLY_PATH, "ScrapedData"));
        public static readonly FileInfo BEATSAVER_SCRAPE_PATH =
            new FileInfo(Path.Combine(ASSEMBLY_PATH, DATA_DIRECTORY.FullName, "combinedScrappedData.json"));
        public static readonly FileInfo SYNCSABER_SCRAPE_PATH =
            new FileInfo(Path.Combine(ASSEMBLY_PATH, DATA_DIRECTORY.FullName, "SyncSaberScrapedData.json"));
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
        private static List<SongInfo> _syncSaberScrape;
        public static List<SongInfo> SyncSaberScrape
        {
            get
            {
                if (_syncSaberScrape == null)
                    _syncSaberScrape = ReadScrapedFile(SYNCSABER_SCRAPE_PATH.FullName);
                return _syncSaberScrape;
            }
        }

        public static List<SongInfo> ReadScrapedFile(string filePath)
        {
            List<SongInfo> results = new List<SongInfo>();

            if (File.Exists(filePath))
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

        public static void UpdateScrapedFile()
        {
            using (StreamWriter file = File.CreateText(SYNCSABER_SCRAPE_PATH.FullName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, SyncSaberScrape);
            }
        }

        public static void FetchUpdatedScrape()
        {

        }

        public static SongInfo GetSongByHash(string hash, bool searchOnline = true)
        {
            SongInfo song = SyncSaberScrape.Where(s => s.hash.ToUpper() == hash.ToUpper()).FirstOrDefault();
            if (song == null && searchOnline)
            {
                Logger.Info($"Song with hash: {hash}, not in scraped data, searching Beat Saver...");
                song = BeatSaverReader.Search(hash, BeatSaverReader.SearchType.hash).FirstOrDefault();
                if(song != null)
                {
                    lock(SyncSaberScrape)
                    {
                        SyncSaberScrape.Add(song);
                    }
                }
            }

            return song;
        }

        public static SongInfo GetSongByKey(string key, bool searchOnline = true)
        {
            SongInfo song = SyncSaberScrape.Where(s => s.key == key).FirstOrDefault();
            if (song == null && searchOnline)
            {
                Logger.Info($"Song with key: {key}, not in scraped data, searching Beat Saver...");
                song = BeatSaverReader.GetSongByKey(key);
                if (song != null)
                {
                    TryAddToScrapedData(song);
                }
            }

            return song;
        }

        public static void TryAddToScrapedData(SongInfo song)
        {
            if (SyncSaberScrape.Where(s => s.hash.ToLower() == song.hash.ToLower()).Count() == 0)
            {
                Logger.Debug($"Adding song {song.key} - {song.songName} by {song.authorName} to ScrapedData");
                lock (ScrapedDataProvider.SyncSaberScrape)
                {

                    ScrapedDataProvider.SyncSaberScrape.Add(song);
                }
            }
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
