using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeedReader;

namespace SyncSaberLib.Config
{
    /// <summary>
    /// Interface for a specific FeedReader configuration.
    /// </summary>
    interface IReaderConfig
    {
        string ReaderName { get; set; }
        string ReaderDescription { get; set; }
        Dictionary<int, string> AvailableFeeds { get; set; }
        bool Enabled { get; set; }


    }
}
