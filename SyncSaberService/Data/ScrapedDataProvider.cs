using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace SyncSaberService.Data
{
    public static class ScrapedDataProvider
    {
        public static List<ScrappedSong> BeatSaverScrape { get; private set; }

        public static void ReadScrapedFile(string filePath)
        {
            
            using (StreamReader file = File.OpenText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                BeatSaverScrape = (List<ScrappedSong>) serializer.Deserialize(file, typeof(List<ScrappedSong>));
            }
        }

    }
}
