using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SimpleJSON;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Http;
using static SyncSaberService.Utilities;

namespace SyncSaberService.Downloaders
{
    public interface IFeedReader
    {
        string GetPageUrl(int feedIndex, int page);
        string GetPageText(string url);
        SongInfo[] GetSongsFromPage(string pageText);
        Dictionary<int, string> FeedUrls { get; }
    }
}
