using System;
using System.Text.RegularExpressions;
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
            //Logger.Debug(this.ToString());
        }

        public string Index
        {
            get { return _songIndex; }
            set
            {
                _songIndex = value;
                if (_beatSaverRegex.IsMatch(_songIndex))
                {
                    int dashIndex = _songIndex.IndexOf('-');
                    _songID = int.Parse(_songIndex.Substring(0, dashIndex));
                    _songVersion = int.Parse(_songIndex.Substring(dashIndex + 1));
                }
                else
                {
                    Logger.Warning($"Invalid song index: {value}");
                }
            }
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

        private int _songID = 0;
        public int SongID
        {
            get
            {

                return _songID;
            }
        }

        private int _songVersion = 0;
        public int SongVersion
        {
            get
            {
                return _songVersion;
            }
        }

        public override string ToString()
        {
            StringBuilder retStr = new StringBuilder();
            retStr.Append("SongInfo:");
            retStr.AppendLine("   Index: " + Index);
            retStr.AppendLine("   Name: " + Name);
            retStr.AppendLine("   Author: " + Author);
            retStr.AppendLine("   URL: " + URL);
            retStr.AppendLine("   Feed: " + Feed);
            return retStr.ToString();
        }

        private static readonly Regex _digitRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);

        private static readonly Regex _beatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);

        private string _songUrl, _songIndex, _songName, _authorName, _feedName;
    }
}
