using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using WebUtilities;

namespace FeedReaderTests.MockClasses
{
    public class MockHttpResponse : IWebResponseMessage
    {

        #region Regex
        private static Regex BeatSaverRegex =
            new Regex(@"\/api\/.*?\/(?:(?:uploader\/)(.+)(?:\/))?(\d+)(?:\?q=)?(.*$)?", RegexOptions.Compiled);
        private static Regex BeastSaberRegex =
            new Regex(@"(?:(?:(?:members\/)|(?:bookmarked_by\=))(.+?)(?:(?:\/wall\/.*)|(?:\&).*))?page=(\d+).*?(?:(?:&count=)(\d+))?", RegexOptions.Compiled);
        private static Regex ScoreSaberRegex =
            new Regex(@"(?:(?:&cat=)(\d+)).*?page=(\d+).*?(?:(?:&ranked=)(\d))?(?:(?:&search=)(.*))?", RegexOptions.Compiled);

        enum BeatSaverGroup
        {
            Uploader = 1,
            Page = 2,
            Query = 3
        }

        enum BeastSaberGroup
        {
            Username = 1,
            Page = 2,
            SongsPerPage = 3
        }

        enum ScoreSaberGroup
        {
            Catalog = 1,
            Page = 2,
            Ranked = 3,
            Query = 4
        }
        #endregion
#pragma warning disable CA1055 // Uri return values should not be strings
        public static string GetFileForUrl(string url)
#pragma warning restore CA1055 // Uri return values should not be strings
        {
            Uri urlAsUri = string.IsNullOrEmpty(url) ? null : new Uri(url);
            return GetFileForUrl(urlAsUri);
        }

        public static string GetFileForUrl(Uri url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url), "url cannot be null for MockHttpContent.GetFileForUrl");
            var urlStr = url.ToString().ToLower();
            string directory = "Data";
            string path = string.Empty;
            IEnumerable<FileInfo> files = null;
            if (urlStr.Contains("beatsaver.com"))
            {
                directory = Path.Combine(directory, "BeatSaver");
                var match = BeatSaverRegex.Match(urlStr);
                var uploader = match.Groups[(int)BeatSaverGroup.Uploader]?.Value;
                var pageStr = match.Groups[(int)BeatSaverGroup.Page].Value;
                var page = string.IsNullOrEmpty(pageStr) ? 0 : int.Parse(pageStr);
                var query = match.Groups[(int)BeatSaverGroup.Query]?.Value;
                throw new NotImplementedException();
                //path = files.Single().FullName;
            }
            else if (urlStr.Contains("bsaber.com"))
            {
                var match = BeastSaberRegex.Match(urlStr);
                var username = match.Groups[(int)BeastSaberGroup.Username].Value;
                var pageStr = match.Groups[(int)BeastSaberGroup.Page].Value;
                var page = string.IsNullOrEmpty(pageStr) ? 0 : int.Parse(pageStr);
                var songsPerPageStr = match.Groups[(int)BeastSaberGroup.SongsPerPage]?.Value;
                var SongsPerPage = string.IsNullOrEmpty(songsPerPageStr) ? 20 : int.Parse(songsPerPageStr);

                directory = Path.Combine(directory, "BeastSaber");
                //var dInfo = new DirectoryInfo(directory);
                files = new DirectoryInfo(directory).GetFiles();
                if (urlStr.Contains("followings"))
                    files = files.Where(f => f.Name.Contains("followings") && f.Name.Contains(page.ToString()));
                else if (urlStr.Contains("bookmarked"))
                {
                    files = files.Where(f => f.Name.Contains("bookmarked"));
                    if (urlStr.Contains("curator"))
                        files = files.Where(f => f.Name.Contains("curator") && f.Name.Contains(page.ToString()));
                    else
                        files = files.Where(f => !f.Name.Contains("curator") && f.Name.Contains(page.ToString()));
                }
                else
                    files = null;
                path = files?.FirstOrDefault()?.FullName ?? string.Empty;
            }
            else if (urlStr.Contains("scoresaber.com"))
            {
                var match = ScoreSaberRegex.Match(urlStr);
                var catStr = match.Groups[(int)ScoreSaberGroup.Catalog].Value;
                var pageStr = match.Groups[(int)ScoreSaberGroup.Page].Value;
                var rankedStr = match.Groups[(int)ScoreSaberGroup.Ranked].Value;

                var catalog = string.IsNullOrEmpty(catStr) ? 0 : int.Parse(catStr);
                var page = string.IsNullOrEmpty(pageStr) ? 0 : int.Parse(pageStr);
                var ranked = string.IsNullOrEmpty(rankedStr) ? 0 : int.Parse(rankedStr);
                var query = match.Groups[(int)ScoreSaberGroup.Query].Value;

                directory = Path.Combine(directory, "ScoreSaber");
                files = new DirectoryInfo(directory).GetFiles();
                throw new NotImplementedException();
                //path = files.Single().FullName;
            }

            return path;
        }

        public MockHttpResponse(Uri url)
        {
            FileSourcePath = GetFileForUrl(url);
            Content = new MockHttpContent(FileSourcePath);
            if (!File.Exists(FileSourcePath))
            {
                StatusCode = HttpStatusCode.NotFound;
                ReasonPhrase = "Not Found";
            }
        }

        public string FileSourcePath { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public bool IsSuccessStatusCode { get; set; }

        private IWebResponseContent _content;
        public IWebResponseContent Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
            }
        }

        private Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //if (httpClient != null)
                    //{
                    //    httpClient.Dispose();
                    //    httpClient = null;
                    //}
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
