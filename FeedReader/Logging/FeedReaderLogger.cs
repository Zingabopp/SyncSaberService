using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;

namespace FeedReader.Logging
{
    public class FeedReaderLogger
        : FeedReaderLoggerBase
    {
        public FeedReaderLogger()
        {
            LoggerName = "FeedReader";
        }

        public FeedReaderLogger(LoggingController controller)
        {
            LogController = controller;
        }

        public override void Trace(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > LogLevel.Trace)
            {
                return;
            }
            string sourcePart, timePart = "";
            if (!ShortSource)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (EnableTimestamp)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            Console.WriteLine($"{sourcePart}{timePart} - Trace] {message}");
        }

        public override void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > LogLevel.Debug)
            {
                return;
            }
            string sourcePart, timePart = "";
            if (!ShortSource)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (EnableTimestamp)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            Console.WriteLine($"{sourcePart}{timePart} - Debug] {message}");
        }

        public override void Info(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > LogLevel.Info)
            {
                return;
            }
            string sourcePart, timePart = "";
            if (!ShortSource)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (EnableTimestamp)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            Console.WriteLine($"{sourcePart}{timePart} - Info] {message}");
        }

        public override void Warning(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > LogLevel.Warning)
            {
                return;
            }
            string sourcePart, timePart = "";
            if (!ShortSource)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (EnableTimestamp)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            Console.WriteLine($"{sourcePart}{timePart} - Warning] {message}");
        }

        public override void Error(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > LogLevel.Error)
            {
                return;
            }
            string sourcePart, timePart = "";
            if (!ShortSource)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (EnableTimestamp)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            Console.WriteLine($"{sourcePart}{timePart} - Error] {message}");
        }

        public override void Exception(string message, Exception e, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > LogLevel.Exception)
            {
                return;
            }
            Console.WriteLine($"[{Path.GetFileName(file)}_{member}({line}) @ {DateTime.Now.ToString("HH:mm")} - Exception] {message} - {e.GetType().FullName}-{e.Message}\n{e.StackTrace}");
        }
    }
}
