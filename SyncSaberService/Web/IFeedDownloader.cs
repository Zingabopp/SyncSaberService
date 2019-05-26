using System.Collections.Generic;

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
