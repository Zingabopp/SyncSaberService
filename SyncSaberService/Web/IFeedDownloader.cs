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

namespace SyncSaberService.Web
{
    public interface IFeedReader
    {
        string Name { get; }
        List<SongInfo> GetSongsFromPage(string pageText);
        Dictionary<int, FeedInfo> Feeds { get; }
        Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings settings);
    }

    public interface IFeedSettings
    {

    }
    public struct FeedInfo
    {
        public FeedInfo(string _name, string _baseUrl)
        {
            Name = _name;
            BaseUrl = _baseUrl;
        }
        public string BaseUrl;
        public string Name;
    }
}
