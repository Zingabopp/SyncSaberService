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
    public class ScoreSaberScrape : IScrapedDataModel<List<ScoreSaberSong>, ScoreSaberSong>
    {
        private bool _initialized;
        public ScoreSaberScrape()
        {
            _initialized = false;
            DefaultPath = Path.Combine(DATA_DIRECTORY.FullName, "ScoreSaberScrape.json");
        }

        public override void Initialize(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = DefaultPath;
            Data = new List<ScoreSaberSong>();
            //(filePath).Populate(this);
            if (File.Exists(filePath))
                ReadScrapedFile(filePath).Populate(Data);
            //JsonSerializer serializer = new JsonSerializer();
            //if (test.Type == Newtonsoft.Json.Linq.JTokenType.Array)
            //    Data = test.ToObject<List<SongInfo>>();
            _initialized = true;
            CurrentFile = new FileInfo(filePath);
        }
    }
}
