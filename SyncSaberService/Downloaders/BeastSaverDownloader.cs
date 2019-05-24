using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SimpleJSON;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Http;
using static SyncSaberService.Utilities;

namespace SyncSaberService.Downloaders
{
    public class BeastSaverDownloader : IFeedReader
    {
        private string _username, _password, _loginUri;
        private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private static readonly string USERNAMEKEY = "{USERNAME}";
        private static readonly string PAGENUMKEY = "{PAGENUM}";
        private static readonly Uri FeedRootUri = new Uri("https://bsaber.com");

        private static CookieContainer _cookies;
        private static CookieContainer Cookies
        {
            get
            {
                return _cookies;
            }
            set
            {
                _cookies = value;
            }
        }
        private Dictionary<int, string> _feeds;
        public Dictionary<int, string> FeedUrls
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<int, string>()
                    {
                        { 0, "https://bsaber.com/members/" + USERNAMEKEY + "/wall/followings/feed/?acpage=" + PAGENUMKEY },
                        { 1, "https://bsaber.com/members/" + USERNAMEKEY + "/bookmarks/feed/?acpage=" + PAGENUMKEY },
                        { 2, "https://bsaber.com/members/curatorrecommended/bookmarks/feed/?acpage=" + PAGENUMKEY }
                    };
                }
                return _feeds;
            }
        }

        /*
private static string _cookieHeader = "";
private static string CookieHeader
{
get
{
if (_cookieHeader == "" && !(Cookies == null))
_cookieHeader = Cookies.GetCookieHeader(new Uri("https://bsaber.com"));
return _cookieHeader;
}
}
*/
        public BeastSaverDownloader(string username, string password, string loginUri = DefaultLoginUri)
        {
            _username = username;
            _password = password;
            _loginUri = loginUri;
            
        }

        public static CookieContainer GetBSaberCookies(string username, string password)
        {
            CookieContainer tempContainer = null;
            lock (_cookies)
            {
                if (_cookies != null)
                {
                    tempContainer = new CookieContainer();
                    tempContainer.Add(_cookies.GetCookies(FeedRootUri));
                }
            }
            if (tempContainer != null)
                return tempContainer;
            string loginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
            string reqString = $"log={username}&pwd={password}&rememberme=forever";
            var tempCookies = GetCookies(loginUri, reqString);
            lock (_cookies)
            {
                _cookies = tempCookies;
            }
            return Cookies;
        }

        public static CookieContainer GetCookies(string loginUri, string requestString)
        {
            byte[] requestData = Encoding.UTF8.GetBytes(requestString);
            CookieContainer cc = new CookieContainer();
            var request = (HttpWebRequest) WebRequest.Create(loginUri);
            request.Proxy = null;
            request.AllowAutoRedirect = false;
            request.CookieContainer = cc;
            request.Method = "post";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = requestData.Length;
            using (Stream s = request.GetRequestStream())
                s.Write(requestData, 0, requestData.Length);
            HttpWebResponse response = (HttpWebResponse) request.GetResponse(); // Needs this to populate cookies

            return cc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText</exception>
        /// <returns></returns>
        public SongInfo[] GetSongsFromPage(string pageText)
        {
            List<SongInfo> songsOnPage = new List<SongInfo>();

            int totalSongsForPage = 0;
            XmlDocument xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(pageText);

            XmlNodeList xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/rss/channel/item");
            foreach (object obj in xmlNodeList)
            {
                XmlNode node = (XmlNode) obj;
                if (node["DownloadURL"] == null || node["SongTitle"] == null)
                {
                    Logger.Debug("Not a song! Skipping!");
                }
                else
                {
                    string songName = node["SongTitle"].InnerText;
                    string innerText = node["DownloadURL"].InnerText;
                    if (innerText.Contains("dl.php"))
                    {
                        Logger.Warning("Skipping BeastSaber download with old url format!");
                        totalSongsForPage++;
                    }
                    else
                    {
                        string songIndex = innerText.Substring(innerText.LastIndexOf('/') + 1);
                        string mapper = GetMapperFromBsaber(node.InnerText);
                        string songUrl = "https://beatsaver.com/download/" + songIndex;
                        SongInfo currentSong = new SongInfo(songIndex, songName, songUrl, mapper);
                        string currentSongDirectory = Path.Combine(Config.BeatSaberPath, "CustomSongs", songIndex);
                        //bool downloadFailed = false;
                        songsOnPage.Add(currentSong);
                    }
                }
            }
            return songsOnPage.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public string GetPageText(string url)
        {
            HttpClient hClient = new HttpClient();
            hClient.DefaultRequestHeaders.Add(HttpRequestHeader.Cookie.ToString(), GetBSaberCookies(_username, _password).GetCookieHeader(FeedRootUri));
            //bool cancelJob = false;
            //lock (EarliestEmptyPage)
            //{
            //    if ((EarliestEmptyPage.number) < info.pageIndex)
            //    {
            //        //Logger.Debug($"Skipping page {info.pageIndex}: {info.pageIndex} > {EarliestEmptyPage.number} ");
            //        return;
            //        //cancelJob = true;
            //    }
            //}

            var pageReadTask = hClient.GetStringAsync(url); //jobClient.DownloadString(info.feedUrl);
            pageReadTask.Wait();
            string pageText = pageReadTask.Result;
            //Logger.Debug(pageText.Result);
            hClient.Dispose();
            return pageText;
        }

        public async Task<string> GetPageTextAsync(string url)
        {
            return await new Task<string>(() => GetPageText(url));
        }

        public string GetPageUrl(string feedUrlBase, int page)
        {
            string feedUrl = feedUrlBase.Replace(USERNAMEKEY, _username).Replace(PAGENUMKEY, page.ToString());
            return feedUrl;
        }

        public string GetPageUrl(int feedIndex, int page)
        {
            return GetPageUrl(FeedUrls[feedIndex], page);
        }

        private static string GetMapperFromBsaber(string innerText)
        {
            //TODO: Needs testing for when a mapper's name isn't obvious
            string prefix = "Mapper: ";
            string suffix = "</p>";

            int startIndex = innerText.IndexOf(prefix);
            if (startIndex < 0)
                return "";
            startIndex += prefix.Length;
            int endIndex = innerText.IndexOf(suffix, startIndex);
            if (endIndex > startIndex && startIndex >= 0)
                return innerText.Substring(startIndex, endIndex - startIndex);
            else
                return "";
        }
    }
}
