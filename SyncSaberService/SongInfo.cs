using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberService
{
    public class SongInfo
    {
        public SongInfo(string songIndex, string songName, string songUrl, string authorName, string feedName = "")
        {
            Index = songIndex;
            Name = songName;
            Author = authorName;
            URL = songUrl;
            Feed = feedName;
        }

        public string Index
        {
            get { return _songIndex; }
            set { _songIndex = value; }
        }
        public string Name
        {
            get { return _songName; }
            set { _songName = value; }
        }
        public string Author
        {
            get { return _authorName; }
            set { _authorName = value; }
        }
        public string URL
        {
            get { return _songUrl; }
            set { _songUrl = value; }
        }
        public string Feed
        {
            get { return _feedName; }
            set { _feedName = value; }
        }

        private string _songUrl, _songIndex, _songName, _authorName, _feedName;
    }
}
