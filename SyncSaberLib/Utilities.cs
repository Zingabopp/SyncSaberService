﻿using System;
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
using System.Diagnostics;

namespace SyncSaberLib
{
    public static class Utilities
    {
        public static string MakeSafeFilename(string str)
        {
            StringBuilder retStr = new StringBuilder(str);
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
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
                        try
                        {
                            string oldFilePath = Path.Combine(target.FullName, "FilesToDelete");
                            if (!Directory.Exists(oldFilePath))
                            {
                                Directory.CreateDirectory(oldFilePath);
                            }
                            File.Move(newPath, Path.Combine(oldFilePath, fileInfo.Name));
                        }
                        catch (Exception) { } // TODO: This is dirty code
                    }
                }
                // Check for file lock
                var time = Stopwatch.StartNew();
                bool waitTimeout = false;
                while (IsFileLocked(fileInfo.FullName) && !waitTimeout)
                    waitTimeout = time.ElapsedMilliseconds < 1000;
                if (waitTimeout)
                    Logger.Warning($"Timeout waiting for {fileInfo.FullName} to be released to move.");
                fileInfo.MoveTo(newPath);
            }
        }
        //async static Task<bool>
        public static bool IsFileLocked(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return !(inputStream.Length > 0);
            }
            catch (Exception) // TODO: Catch only IOException? so the caller doesn't wait for a Timeout if there are other errors
            {
                return true;
            }
        }

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
            while (queue.TryDequeue(out T item))
            {
                // do nothing
            }
        }
    }

    public static class AggregateExceptionExtensions
    {
        public static void WriteExceptions(this AggregateException ae, string message)
        {
            for(int i = 0; i < ae.InnerExceptions.Count; i++)
            {
                Logger.Exception($"Exception {i}:\n", ae.InnerExceptions[i]);
                if (ae.InnerExceptions[i] is AggregateException ex)
                    WriteExceptions(ex, ""); // TODO: This could get very long
            }
        }
    }

    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Adds the given key and value to the dictionary. If they key already exists, updates the value.
        /// Returns true if the key already exists.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True if the key already exists, false otherwise.</returns>
        public static bool AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
                return true;
            }
            dict.Add(key, value);
            return false;
        }
    }

}
