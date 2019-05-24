using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.ComponentModel;

namespace SyncSaberService
{
    public static class Utilities
    {


        /// <summary>
        /// Creates a new KeyData object with the provided keyName and keyValue;
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public static KeyData CreateKeyData(string keyName, string keyValue)
        {
            var data = new KeyData(keyName);
            data.Value = keyValue;
            return data;
        }

        /// <summary>
        /// Tries to parse a string as a bool, returns false if it fails.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <param name="defaultVal"></param>
        /// <returns>Successful</returns>
        public static bool StrToBool(string str, out bool result, bool defaultVal = false)
        {
            bool successful = true;
            switch (str.ToLower())
            {
                case "0":
                    result = false;
                    break;
                case "false":
                    result = false;
                    break;
                case "1":
                    result = true;
                    break;
                case "true":
                    result = true;
                    break;
                default:
                    successful = false;
                    result = defaultVal;
                    break;
            }
            return successful;
        }



        public static void EmptyDirectory(string directory, bool delete = true)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            if (directoryInfo.Exists)
            {
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
                if (delete)
                {
                    directoryInfo.Delete(true);
                }
            }
        }

        public static void MoveFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo directoryInfo in source.GetDirectories())
            {
                Utilities.MoveFilesRecursively(directoryInfo, target.CreateSubdirectory(directoryInfo.Name));
            }
            foreach (FileInfo fileInfo in source.GetFiles())
            {
                string newPath = Path.Combine(target.FullName, fileInfo.Name);
                if (File.Exists(newPath))
                {
                    try
                    {
                        File.Delete(newPath);
                    }
                    catch (Exception)
                    {
                        string oldFilePath = Path.Combine(Config.BeatSaberPath, "FilesToDelete");
                        if (!Directory.Exists(oldFilePath))
                        {
                            Directory.CreateDirectory(oldFilePath);
                        }
                        File.Move(newPath, Path.Combine(oldFilePath, fileInfo.Name));
                    }
                }
                fileInfo.MoveTo(newPath);
            }
        }
        //async static Task<bool>

        public static void WriteStringListSafe(string path, List<string> data, bool sort = true)
        {
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", true);
            }
            if (sort)
            {
                data.Sort();
            }
            File.WriteAllLines(path, data);
            File.Delete(path + ".bak");
        }
        
        public static CookieContainer LoginBSaber(string username, string password)
        {
            string loginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
            string reqString = $"log={username}&pwd={password}&rememberme=forever";
            byte[] requestData = Encoding.UTF8.GetBytes(reqString);
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
            
            //using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
            //{
            //    foreach (Cookie c in response.Cookies)
            //        Console.WriteLine(c.Name + " = " + c.Value);
            //}
           
            HttpWebResponse response = (HttpWebResponse) request.GetResponse(); // Needs this to populate cookies
            return cc;
        }
       
        public static string FormatTimeSpan(TimeSpan timeElapsed)
        {
            string timeElapsedStr = "";
            if (timeElapsed.TotalMinutes >= 1)
            {
                timeElapsedStr = $"{(int) timeElapsed.TotalMinutes}m ";
            }
            timeElapsedStr = $"{timeElapsedStr}{timeElapsed.Seconds}s";
            return timeElapsedStr;
        }


    }

    internal static class ConcurrentQueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            T item;
            while (queue.TryDequeue(out item))
            {
                // do nothing
            }
        }
    }

}
