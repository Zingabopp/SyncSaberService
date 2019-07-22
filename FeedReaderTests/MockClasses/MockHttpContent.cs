using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebUtilities;

namespace FeedReaderTests.MockClasses
{
    public class MockHttpContent : IWebResponseContent
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

        public static string GetFileForUrl(string url)
        {
            url = url.ToLower();
            string directory = "Data";
            string path = string.Empty;
            IEnumerable<FileInfo> files = null;
            if (url.Contains("beatsaver.com"))
            {
                directory = Path.Combine(directory, "BeatSaver");
                var match = BeatSaverRegex.Match(url);
                var uploader = match.Groups[(int)BeatSaverGroup.Uploader]?.Value;
                var pageStr = match.Groups[(int)BeatSaverGroup.Page].Value;
                var page = string.IsNullOrEmpty(pageStr) ? 0 : int.Parse(pageStr);
                var query = match.Groups[(int)BeatSaverGroup.Query]?.Value;

                path = files.Single().FullName;
            }
            else if (url.Contains("bsaber.com"))
            {
                var match = BeastSaberRegex.Match(url);
                var username = match.Groups[(int)BeastSaberGroup.Username].Value;
                var pageStr = match.Groups[(int)BeastSaberGroup.Page].Value;
                var page = string.IsNullOrEmpty(pageStr) ? 0 : int.Parse(pageStr);
                var songsPerPageStr = match.Groups[(int)BeastSaberGroup.SongsPerPage]?.Value;
                var SongsPerPage = string.IsNullOrEmpty(songsPerPageStr) ? 20 : int.Parse(songsPerPageStr);

                directory = Path.Combine(directory, "BeastSaber");
                files = new DirectoryInfo(directory).GetFiles();
                if (url.Contains("followings"))
                    files = files.Where(f => f.Name.Contains("followings") && f.Name.Contains(page.ToString()));
                else if (url.Contains("bookmarked"))
                {
                    files = files.Where(f => f.Name.Contains("bookmarked"));
                    if (url.Contains("curator"))
                        files = files.Where(f => f.Name.Contains("curator") && f.Name.Contains(page.ToString()));
                    else
                        files = files.Where(f => !f.Name.Contains("curator") && f.Name.Contains(page.ToString()));
                }
                path = files.Single().FullName;
            }
            else if (url.Contains("scoresaber.com"))
            {
                var match = ScoreSaberRegex.Match(url);
                var catStr = match.Groups[(int)ScoreSaberGroup.Catalog].Value;
                var pageStr = match.Groups[(int)ScoreSaberGroup.Page].Value;
                var rankedStr = match.Groups[(int)ScoreSaberGroup.Ranked].Value;

                var catalog = string.IsNullOrEmpty(catStr) ? 0 : int.Parse(catStr);
                var page = string.IsNullOrEmpty(pageStr) ? 0 : int.Parse(pageStr);
                var ranked = string.IsNullOrEmpty(rankedStr) ? 0 : int.Parse(rankedStr);
                var query = match.Groups[(int)ScoreSaberGroup.Query].Value;

                directory = Path.Combine(directory, "ScoreSaber");
                files = new DirectoryInfo(directory).GetFiles();

                path = files.Single().FullName;
            }

            return path;
        }
        public string FileSourcePath { get; private set; }
        private readonly string _contentType;

        public Dictionary<string, IEnumerable<string>> _headers;
        public MockHttpContent(string url, Dictionary<string, IEnumerable<string>> headers = null)
        {
            if (headers == null)
                _headers = new Dictionary<string, IEnumerable<string>>();
            FileSourcePath = GetFileForUrl(url);
            if (FileSourcePath.EndsWith("json"))
                _contentType = @"application/json";
            else if (FileSourcePath.EndsWith("xml"))
                _contentType = @"text/xml";
            else
                _contentType = null;
            Headers = new ReadOnlyDictionary<string, IEnumerable<string>>(_headers);
        }


        #region IWebResponseContent
        public string ContentType { get { return _contentType; } }

        public ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

        public async Task<byte[]> ReadAsByteArrayAsync()
        {
            using (FileStream stream = new FileStream(FileSourcePath, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream).ConfigureAwait(false);
                    return memStream.ToArray();
                }
            }
        }

        public async Task ReadAsFileAsync(string filePath, bool overwrite)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            if (!overwrite && File.Exists(filePath))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", filePath));
            }
            using (var stream = File.OpenRead(FileSourcePath))
            using (var writeStream = File.OpenWrite(filePath))
            {
                await stream.CopyToAsync(writeStream).ConfigureAwait(false);
            }

        }

        public async Task<Stream> ReadAsStreamAsync()
        {
            using (Stream stream = new FileStream(FileSourcePath, FileMode.Open, FileAccess.Read))
                return await new Task<Stream>(() => { return stream; }).ConfigureAwait(false);
        }

        public async Task<string> ReadAsStringAsync()
        {
            using (FileStream stream = new FileStream(FileSourcePath, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _headers = null;
                    Headers = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #endregion

    }
}
