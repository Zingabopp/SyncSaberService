using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;

namespace SyncSaberLib
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error
    }

    public static class Logger
    {
        private static readonly string LoggerName = typeof(Logger).Namespace;
        public static LogLevel LogLevel = LogLevel.Info;
        private static readonly ConsoleColor DefaultFgColor = ConsoleColor.Gray;
        public static bool ShortenSourceName = false;
        public static bool ShowTime = true;
        public static StreamWriter fileWriter = new StreamWriter("log.txt", false);

        private static void ResetForegroundColor()
        {
            Console.ForegroundColor = DefaultFgColor;
        }

        public static void Trace(string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)//params object[] args)
        {
            if (LogLevel > LogLevel.Trace)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            string sourcePart, timePart = "";
            if (!ShortenSourceName)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (ShowTime)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            string logText = $"{sourcePart}{timePart} - Trace] {text}";
            Console.WriteLine(logText);
            fileWriter.Write(logText + "\n");
            //Console.ForegroundColor = ConsoleColor.Cyan;
            //Console.WriteLine("[" + LoggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Trace] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public static void Debug(string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)//params object[] args)
        {
            if (LogLevel > LogLevel.Debug)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            string sourcePart, timePart = "";
            if (!ShortenSourceName)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (ShowTime)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            string logText = $"{sourcePart}{timePart} - Debug] {text}";
            Console.WriteLine(logText);
            fileWriter.Write(logText + "\n");
            
            //Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine("[" + LoggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Debug] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public static void Info(string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)//params object[] args)
        {
            if (LogLevel > LogLevel.Info)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            string sourcePart, timePart = "";
            if (!ShortenSourceName)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (ShowTime)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            string logText = $"{sourcePart}{timePart} - Info] {text}";
            Console.WriteLine(logText);
            fileWriter.Write(logText + "\n");
            //Console.WriteLine("[" + LoggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Info] " + String.Format(format, args));
            //Console.WriteLine("{0}_{1}({2}): {3}", Path.GetFileName(file), member, line, format);
            ResetForegroundColor();
        }

        public static void Warning(string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)//params object[] args)
        {
            if (LogLevel > LogLevel.Warn)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            string sourcePart, timePart = "";
            if (!ShortenSourceName)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (ShowTime)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            string logText = $"{sourcePart}{timePart} - Warning] {text}";
            Console.WriteLine(logText);
            fileWriter.Write(logText + "\n");
            fileWriter.Flush();
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine("[" + LoggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Warning] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public static void Error(string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)//params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string sourcePart, timePart = "";
            if (!ShortenSourceName)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (ShowTime)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            string logText = $"{sourcePart}{timePart} - Error] {text}";
            Console.WriteLine(logText);
            fileWriter.Write(logText + "\n");
            fileWriter.Flush();

            //Console.WriteLine("[" + LoggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Error] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public static void Exception(string text,
            Exception e,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            //Console.WriteLine("[" + LoggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format("{0}-{1}-{2}\n{3}", text, e.GetType().FullName, e.Message, e.StackTrace));
            string logText = $"[{Path.GetFileName(file)}_{member}({line}) @ {DateTime.Now.ToString("HH:mm")} - Info] {text} - {e.GetType().FullName}-{e.Message}\n{e.StackTrace}";
            Console.WriteLine(logText);
            fileWriter.Write(logText + "\n");
            fileWriter.Flush();
            ResetForegroundColor();
        }

        public static string EscapeBraces(this string str)
        {
            return str.Replace("{", "{{").Replace("}", "}}");
        }
    }
}
