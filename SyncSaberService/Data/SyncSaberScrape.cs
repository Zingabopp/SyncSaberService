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
    public class SyncSaberScrape : IScrapedDataModel<List<SongInfo>, SongInfo>
    {
        private static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly DirectoryInfo DATA_DIRECTORY = new DirectoryInfo(Path.Combine(ASSEMBLY_PATH, "ScrapedData"));
        [JsonIgnore]
        private bool _initialized;
        public List<SongInfo> Data { get; private set; }

        [JsonIgnore]
        public bool ReadOnly => throw new NotImplementedException();

        [JsonIgnore]
        public string DefaultPath => Path.Combine(ASSEMBLY_PATH, DATA_DIRECTORY.FullName, "SyncSaberScrapedData.json");

        [JsonIgnore]
        public FileInfo CurrentFile => throw new NotImplementedException();

        public override void Initialize(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = DefaultPath;
            var test = ReadScrapedFile(filePath);
            _initialized = true;
        }


        public override void WriteFile(string filePath)
        {
            throw new NotImplementedException();
        }
    }
}
