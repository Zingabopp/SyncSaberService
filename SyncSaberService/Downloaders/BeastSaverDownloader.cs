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
        private string _username, _password;
        private readonly string _loginUri = "";
        private CookieContainer _cookies;
        public BeastSaverDownloader(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public static CookieContainer GetBSaberCookies(string username, string password)
        {
            string loginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
            string reqString = $"log={username}&pwd={password}&rememberme=forever";
            return GetCookies(loginUri, reqString);
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

        public SongInfo[] GetSongsFromPage(string pageText)
        {
            throw new NotImplementedException();
        }

        public string GetPageText(string url)
        {
            throw new NotImplementedException();
        }

        public string GetPageUrl(int page)
        {
            throw new NotImplementedException();
        }
    }
}
