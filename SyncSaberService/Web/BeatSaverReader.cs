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
    class BeatSaverReader : IFeedReader
    {
        private static readonly string AUTHORKEY = "{AUTHOR}";
        private static readonly string PAGEKEY = "{PAGE}";

        private Dictionary<int, FeedInfo> _feeds;
        public Dictionary<int, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<int, FeedInfo>()
                    {
                        { 0, new FeedInfo("author", "https://beatsaver.com/api/songs/search/user/" +  AUTHORKEY) },
                        { 1, new FeedInfo("newest", "https://beatsaver.com/api/songs/new/" + PAGEKEY) },
                        { 2, new FeedInfo("top", "https://beatsaver.com/api/songs/top/" + PAGEKEY) }
                    };
                }
                return _feeds;
            }
        }



        public string GetPageText(string url)
        {
            HttpClient hClient = new HttpClient();
            var pageReadTask = hClient.GetStringAsync(url); //jobClient.DownloadString(info.feedUrl);
            pageReadTask.Wait();
            string pageText = pageReadTask.Result;
            //Logger.Debug(pageText.Result);
            hClient.Dispose();
            return pageText;
        }

        public string GetPageUrl(int feedIndex, string author = "")
        {
            return Feeds[feedIndex].BaseUrl.Replace(AUTHORKEY, author);
        }
        private static readonly string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";
        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings _settings)
        {
            var settings = _settings as BeatSaverFeedSettings;
            if (settings == null)
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            string pageText = GetPageText(GetPageUrl(settings.FeedIndex, settings.Author));
            var songs = GetSongsFromPage(pageText);
            Dictionary<int, SongInfo> retDict = new Dictionary<int, SongInfo>();
            foreach (var song in songs)
            {
                if (retDict.ContainsKey(song.SongID))
                {
                    if (retDict[song.SongID].SongVersion < song.SongVersion)
                    {
                        Logger.Debug($"Song with ID {song.SongID} already exists, updating");
                        retDict[song.SongID] = song;
                    }
                    else
                    {
                        Logger.Debug($"Song with ID {song.SongID} is already the newest version");
                    }
                }
                else
                {
                    retDict.Add(song.SongID, song);
                }
            }
            return retDict;
        }

        public SongInfo[] GetSongsFromPage(string pageText)
        {
            throw new NotImplementedException();
        }
    }

    public class BeatSaverFeedSettings : IFeedSettings
    {
        public int FeedIndex;
        public int MaxPages;
        public string Author;

        public BeatSaverFeedSettings(int _feedIndex, int _maxPages)
        {
            FeedIndex = _feedIndex;
            MaxPages = _maxPages;
            Author = "";
        }
        public BeatSaverFeedSettings(int _feedIndex, string _author)
        {
            FeedIndex = _feedIndex;
            Author = _author;
            MaxPages = 0;
        }
    }
}
