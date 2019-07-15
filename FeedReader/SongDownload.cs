using System;
using System.Collections.Generic;
using System.Text;

namespace FeedReader
{
    public class SongDownload
    {
        private string _hash;
        public string Hash
        {
            get { return _hash; }
            set
            {
                _hash = value?.ToUpper();
            }
        }
        public string DownloadUrl { get; set; }

        public SongDownload() { }
        public SongDownload(string hash, string downloadUrl = "")
            : this()
        {
            Hash = hash;
            DownloadUrl = downloadUrl;
        }
    }
}
