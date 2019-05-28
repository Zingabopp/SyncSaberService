using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace SyncSaberService.Web
{
    public interface IFeedReader
    {
        string Name { get; } // Name of the reader
        string Source { get; } // Name of the site
        List<SongInfo> GetSongsFromPage(string pageText);
        //Dictionary<int, FeedInfo> Feeds { get; }
        Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings settings);
        Playlist[] PlaylistsForFeed(int feedIndex);
    }

    public interface IFeedSettings
    {
        string FeedName { get; }
        int FeedIndex { get; }
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
