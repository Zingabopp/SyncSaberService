using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FeedReader
{
    public interface IFeedReader
    {
        string Name { get; } // Name of the reader
        string Source { get; } // Name of the site
        bool Ready { get; } // Reader is ready
        bool StoreRawData { get; set; } // Save the raw data in ScrapedSong

        /// <summary>
        /// Anything that needs to happen before the Reader is ready.
        /// </summary>
        void PrepareReader();

        /// <summary>
        /// Retrieves the songs from a feed and returns them as a Dictionary. Key is the song hash.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        Dictionary<string, ScrapedSong> GetSongsFromFeed(IFeedSettings settings);

        Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings);
        Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings, CancellationToken cancellationToken);
    }

    public interface IFeedSettings
    {
        string FeedName { get; } // Name of the feed
        int FeedIndex { get; } // Index of the feed 

        /// <summary>
        /// Max number of songs to retrieve, 0 for unlimited.
        /// </summary>
        int MaxSongs { get; set; }

        /// <summary>
        /// Page of the feed to start on, default is 1. For all feeds, setting '1' here is the same as starting on the first page.
        /// </summary>
        int StartingPage { get; set; }
    }

    /// <summary>
    /// Data for a feed.
    /// </summary>
    public struct FeedInfo : IEquatable<FeedInfo>
    {
        public FeedInfo(string name, string baseUrl)
        {
            Name = name;
            BaseUrl = baseUrl;
        }
        public string BaseUrl { get; set; } // Base URL for the feed, has string keys to replace with things like page number/bsaber username
        public string Name { get; set; } // Name of the feed

        #region EqualsOperators
        public override bool Equals(object obj)
        {
            if (!(obj is FeedInfo))
                return false;
            return Equals((FeedInfo)obj);
        }
        public bool Equals(FeedInfo other)
        {
            if (Name != other.Name)
                return false;
            return BaseUrl == other.BaseUrl;
        }

        public static bool operator ==(FeedInfo feedInfo1, FeedInfo feedInfo2)
        {
            return feedInfo1.Equals(feedInfo2);
        }
        public static bool operator !=(FeedInfo feedInfo1, FeedInfo feedInfo2)
        {
            return !feedInfo1.Equals(feedInfo2);
        }

        public override int GetHashCode() => (Name, BaseUrl).GetHashCode();
        #endregion
    }
}
