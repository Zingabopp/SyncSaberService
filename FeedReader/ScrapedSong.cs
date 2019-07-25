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
        /// <summary>
        /// Full URL to download song.
        /// </summary>
        public Uri DownloadUri { get; set; }
        /// <summary>
        /// What web page this song was scraped from.
        /// </summary>
        public Uri SourceUri { get; set; }
        public string SongName { get; set; }
        public string MapperName { get; set; }
        /// <summary>
        /// Data this song was scraped from in JSON form.
        /// </summary>
        public string RawData { get; set; }

        public ScrapedSong() { }
        public ScrapedSong(string hash)
        {
            Hash = hash;
        }
    }
}