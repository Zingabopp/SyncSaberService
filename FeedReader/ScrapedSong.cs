using System;
using System.Collections.Generic;
using System.Text;

namespace FeedReader
{
    public class ScrapedSong
    {
        private string _hash;
        public string Hash
        {
            get { return _hash; }
            set { _hash = value?.ToUpper(); }
        }
        public string DownloadUrl { get; set; }

        public string RawData { get; set; }

        public ScrapedSong() { }
        public ScrapedSong(string hash)
        {
            Hash = hash;
        }
    }
}