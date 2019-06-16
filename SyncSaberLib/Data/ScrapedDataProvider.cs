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
    public static class ScrapedDataProvider
    {
        private static bool _initialized = false;
        private const string SCRAPED_DATA_URL = "https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/combinedScrappedData.json";
        public static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly DirectoryInfo DATA_DIRECTORY = new DirectoryInfo(Path.Combine(ASSEMBLY_PATH, "ScrapedData"));

        public static BeatSaverScrape BeatSaverSongs { get; set; }
        public static ScoreSaberScrape ScoreSaberSongs { get; set; }
        public static Dictionary<string, SongInfo> Songs { get; set; }
        public static void Initialize()
        {
            if (!DATA_DIRECTORY.Exists)
                DATA_DIRECTORY.Create();
            DATA_DIRECTORY.Refresh();
            BeatSaverSongs = new BeatSaverScrape();
            BeatSaverSongs.Initialize();
            ScoreSaberSongs = new ScoreSaberScrape();
            ScoreSaberSongs.Initialize();
            Songs = new Dictionary<string, SongInfo>();
            foreach (var song in BeatSaverSongs.Data)
            {
                var newSong = new SongInfo(song.hash);
                newSong.BeatSaverInfo = song;
                if (Songs.AddOrUpdate(song.hash.ToUpper(), newSong))
                    Logger.Warning($"Repeated hash while creating SongInfo Dictionary, this should not happen. {song.name} by {song.metadata.levelAuthorName}");
            }
            foreach (var diff in ScoreSaberSongs.Data)
            {
                if (diff.hash.Count() < 40)
                    continue; // Using the old hash, skip
                if (Songs.ContainsKey(diff.hash))
                    Songs[diff.hash].ScoreSaberInfo.AddOrUpdate(diff.uid, diff);
                else
                {
                    var newSong = new SongInfo(diff.hash);
                    newSong.ScoreSaberInfo.AddOrUpdate(diff.uid, diff);
                    Songs.AddOrUpdate(diff.hash, newSong);
                }
            }
            _initialized = true;
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


        public static void FetchUpdatedScrape()
        {

        }

        /// <summary>
        /// Attempts to find a song with the provided hash. If there's no matching
        /// song in the ScrapedData and searchOnline is true, it searches Beat Saver. If a match is found
        /// online, it adds the SongInfo to the ScrapedData. Returns true if a SongInfo is found.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="song"></param>
        /// <param name="searchOnline"></param>
        /// <returns></returns>
        public static bool TryGetSongByHash(string hash, out SongInfo song, bool searchOnline = true)
        {
            hash = hash.ToUpper();
            song = Songs.ContainsKey(hash) ? Songs[hash] : null;
            if (song == null && searchOnline)
            {
                Logger.Info($"Song with hash: {hash}, not in scraped data, searching Beat Saver...");
                song = BeatSaverReader.Search(hash, BeatSaverReader.SearchType.hash).FirstOrDefault();
                if (song != null)
                {
                    TryAddToScrapedData(song);
                }
                else
                    Logger.Warning($"Unable to find song with hash {hash} on Beat Saver, skipping.");
            }

            return song != null;
        }

        /// <summary>
        /// Attempts to find a song with the provided Beat Saver song ID. If there's no matching
        /// song in the ScrapedData and searchOnline is true, it searches Beat Saver. If a match is found
        /// online, it adds the SongInfo to the ScrapedData. Returns true if a SongInfo is found.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="song"></param>
        /// <param name="searchOnline"></param>
        /// <returns></returns>
        public static bool TryGetSongByKey(string key, out SongInfo song, bool searchOnline = true)
        {
            song = Songs.Values.Where(s => s.key == key).FirstOrDefault();
            if (song == null && searchOnline)
            {
                Logger.Info($"Song with key: {key}, not in scraped data, searching Beat Saver...");
                song = BeatSaverReader.GetSongByKey(key);
                if (song != null)
                {
                    TryAddToScrapedData(song);
                }
                else
                    Logger.Warning($"Unable to find song with key {key} on Beat Saver, skipping.");
            }

            return song != null;
        }

        /// <summary>
        /// Adds the provided SongInfo to the ScrapedData if song isn't already in there.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static bool TryAddToScrapedData(SongInfo song)
        {
            if (Songs.Values.Where(s => s.hash.ToLower() == song.hash.ToLower()).Count() == 0)
            {
                //Logger.Debug($"Adding song {song.key} - {song.songName} by {song.authorName} to ScrapedData");
                lock (Songs)
                {
                    Songs.Add(song.hash, song);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to find a SongInfo matching the provided SongInfoEnhanced.
        /// It creates a new SongInfo, attaches the provided SongInfoEnhanced, and adds it to the ScrapedData if no match is found.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="searchOnline"></param>
        /// <returns></returns>
        public static SongInfo GetOrCreateSong(BeatSaverSong song, bool searchOnline = true)
        {
            bool foundOnline = TryGetSongByHash(song.hash, out SongInfo songInfo, searchOnline);
            if (songInfo == null)
            {
                songInfo = song.GenerateSongInfo();
                TryAddToScrapedData(songInfo);
            }
            songInfo.BeatSaverInfo = song;
            return songInfo;
        }

        public static SongInfo GetOrCreateSong(ScoreSaberSong song, bool searchOnline = true)
        {
            bool foundOnline = TryGetSongByHash(song.hash, out SongInfo songInfo, searchOnline);
            if (songInfo == null)
            {
                songInfo = song.GenerateSongInfo();
                TryAddToScrapedData(songInfo);
            }
            songInfo.ScoreSaberInfo.AddOrUpdate(song.uid, song);
            return songInfo;
        }

        public static SongInfo GetSong(ScoreSaberSong song, bool searchOnline = true)
        {
            bool foundOnline = TryGetSongByHash(song.hash, out SongInfo songInfo, searchOnline);
            if (songInfo != null)
                songInfo.ScoreSaberInfo.AddOrUpdate(song.uid, song);
            return songInfo;
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
