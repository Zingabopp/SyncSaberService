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

        private CookieContainer _cookies;
        private CookieContainer Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = GetBSaberCookies(_username, _password);
                return _cookies;
            }
        }
        private string _cookieHeader = "";
        private string CookieHeader
        {
            get
            {
                if (_cookieHeader == "")
                    _cookieHeader = Cookies.GetCookieHeader(new Uri("https://bsaber.com"));
                return _cookieHeader;
            }
        }

        public BeastSaverDownloader(string username, string password, string loginUri = DefaultLoginUri)
        {
            _username = username;
            _password = password;
            _loginUri = loginUri;
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
            HttpClient hClient = new HttpClient();
            hClient.DefaultRequestHeaders.Add(HttpRequestHeader.Cookie.ToString(), CookieHeader);
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

        public string GetPageUrl(int page)
        {
            throw new NotImplementedException();
        }
    }
}
