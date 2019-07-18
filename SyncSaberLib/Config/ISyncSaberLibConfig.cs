using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// Songs for SideQuest:
/// AppData\Roaming\SideQuest\bsaber
/// Quest Folder:
/// This PC > Quest > Internal Shared Storage > BeatOnData > CustomSongs
namespace SyncSaberLib.Config
{
    /// <summary>
    /// Overall configuration for SyncSaberLib.
    /// </summary>
    interface ISyncSaberLibConfig
    {
        IEnumerable<IFeedConfig> FeedConfigs { get; set; }
        Dictionary<string, object> CustomSettings { get; set; }
        string ConfigPath { get; set; }
        string ScrapedDataDirectory { get; set; }
        string BeatSaberDirectory { get; set; }
        string SongDirectoryPath { get; set; }
        string LoggingLevel { get; set; }
        bool DeleteOldVersions { get; set; }
        int DownloadTimeout { get; set; }
        int MaxConcurrentDownloads { get; set; }
        int MaxConcurrentPageChecks { get; set; }
        

        bool SaveChanges();

    }
}
