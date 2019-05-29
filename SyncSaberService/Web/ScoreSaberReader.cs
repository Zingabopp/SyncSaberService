using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberService.Web
{
    class ScoreSaberReader : IFeedReader
    {
        /// API Examples:
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=50&page=1&ranked=1 // Sorted by PP
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=10&page=1&search=honesty&ranked=1
        #region Constants
        public static readonly string NameKey = "ScoreSaberReader";
        public static readonly string SourceKey = "ScoreSaber";
        private static readonly string USERNAMEKEY = "{USERNAME}";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        private static readonly string CATKEY = "{CAT}";
        private static readonly string RANKEDKEY = "{RANKKEY}";
        private static readonly string LIMITKEY = "{LIMIT}";
        private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private static readonly Uri FeedRootUri = new Uri("https://bsaber.com");
        #endregion

        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }

        private static Dictionary<int, FeedInfo> _feeds;
        public static Dictionary<int, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<int, FeedInfo>()
                    {
                        { 0, new FeedInfo("top ranked", $"https://scoresaber.com/api.php?function=get-leaderboards&cat={CATKEY}&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") }
                    };
                }
                return _feeds;
            }
        }

        public Dictionary<int, SongInfo> GetSongsFromFeed(IFeedSettings settings)
        {
            throw new NotImplementedException();
        }

        public List<SongInfo> GetSongsFromPage(string pageText)
        {
            throw new NotImplementedException();
        }

        public Playlist[] PlaylistsForFeed(int feedIndex)
        {
            throw new NotImplementedException();
        }

        public void PrepareReader()
        {
            throw new NotImplementedException();
        }
    }

    public class ScoreSaberReaderSettings : IFeedSettings
    {
        public string FeedName => "ScoreSaber";

        public int FeedIndex => throw new NotImplementedException();
    }
}
