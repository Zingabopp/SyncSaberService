using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace SyncSaberLib.Data
{
    public abstract class IScrapedDataModel<T, DataType> 
        where T : IEnumerable<DataType>, new()
    {
        private static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly DirectoryInfo DATA_DIRECTORY = new DirectoryInfo(Path.Combine(ASSEMBLY_PATH, "ScrapedData"));

        public virtual T Data { get; protected set; }
        [JsonIgnore]
        public bool ReadOnly { get; protected set; }
        [JsonIgnore]
        public string DefaultPath { get; protected set; }
        [JsonIgnore]
        public FileInfo CurrentFile { get; protected set; }

        public virtual JToken ReadScrapedFile(string filePath)
        {
            JToken results = null;

            if (File.Exists(filePath))
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    //results = (JObject)serializer.Deserialize(file, typeof(JObject));
                    results = JToken.Parse(file.ReadToEnd());
                }
            return results;
        }
        public virtual void WriteFile(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = CurrentFile.FullName;
            using (StreamWriter file = File.CreateText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this);
            }
        }
        public abstract void Initialize(string filePath = "");
        
    }


    public static class JsonExtensions
    {
        public static void Populate<T>(this JToken value, T target) where T : class
        {
            using (var sr = value.CreateReader())
            {
                JsonSerializer.CreateDefault().Populate(sr, target); // Uses the system default JsonSerializerSettings
            }
        }
    }
}
