using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberLib.Config
{
    interface IFeedConfig
    {
        string Name { get; set; }
        int FeedIndex { get; set; }
        string Description { get; set; }
        bool Enabled { get; set; }
        int MaxPages { get; set; }
        int MaxSongs { get; set; }
        int StartingPage { get; set; }
        Dictionary<string, CustomSetting> CustomSettings { get; set; }
    }
}
