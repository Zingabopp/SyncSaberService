using System;
using System.Collections.Generic;
using System.Text;
using FeedReader.Logging;

namespace FeedReader
{
    static class Util
    {
        public static FeedReaderLoggerBase Logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static int MaxAggregateExceptionDepth = 10;

        public static void WriteExceptions(this AggregateException ae, string message)
        {
            Logger.Exception(message, ae);
            for (int i = 0; i < ae.InnerExceptions.Count; i++)
            {
                Logger.Exception($"Exception {i}:\n", ae.InnerExceptions[i]);
                if (ae.InnerExceptions[i] is AggregateException ex)
                    WriteExceptions(ex, 0); // TODO: This could get very long
            }
        }
        public static void WriteExceptions(this AggregateException ae, int depth = 0)
        {
            for (int i = 0; i < ae.InnerExceptions.Count; i++)
            {
                Logger.Exception($"Exception {i}:\n", ae.InnerExceptions[i]);
                if (ae.InnerExceptions[i] is AggregateException ex)
                {
                    if (depth < MaxAggregateExceptionDepth)
                    {
                        WriteExceptions(ex, depth + 1);
                    }
                }
            }
        }
    }
}
