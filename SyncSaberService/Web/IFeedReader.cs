using System.Collections;
using System.Collections.Generic;
using SyncSaberService.Data;
using System.Linq;

namespace SyncSaberService.Web
{
    public interface IFeedReader
    {
        string Name { get; } // Name of the reader
        string Source { get; } // Name of the site
        bool Ready { get; }
        void PrepareReader();
        //List<SongInfo> GetSongsFromPage(string pageText);
        //Dictionary<int, FeedInfo> Feeds { get; }
        Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings settings);
        Playlist[] PlaylistsForFeed(int feedIndex);
    }

    public interface IFeedSettings
    {
        string FeedName { get; }
        int FeedIndex { get; }
        int MaxSongs { get; set; }
        bool searchOnline { get; set; }
        bool UseSongKeyAsOutputFolder { get; set; }
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
