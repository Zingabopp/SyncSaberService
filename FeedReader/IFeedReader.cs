using System;
using System.Collections.Generic;
using System.Text;
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
        /// Retrieves the songs from a feed and returns them as a Dictionary. Key is the song hash, value is the full download URL.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        Dictionary<string, ScrapedSong> GetSongsFromFeed(IFeedSettings settings);

        Task<Dictionary<string, ScrapedSong>> GetSongsFromFeedAsync(IFeedSettings settings);
    }

    public interface IFeedSettings
    {
        string FeedName { get; } // Name of the feed
        int FeedIndex { get; } // Index of the feed 
        int MaxSongs { get; set; } // Max number of songs to retrieve
        bool searchOnline { get; set; } // Search online instead of using local scrapes
        //bool UseSongKeyAsOutputFolder { get; set; } // Use the song key as the output folder name instead of the default
    }

    /// <summary>
    /// Data for a feed.
    /// </summary>
    public struct FeedInfo
    {
        public FeedInfo(string _name, string _baseUrl)
        {
            Name = _name;
            BaseUrl = _baseUrl;
        }
        public string BaseUrl; // Base URL for the feed, has string keys to replace with things like page number/bsaber username
        public string Name; // Name of the feed
    }
}
