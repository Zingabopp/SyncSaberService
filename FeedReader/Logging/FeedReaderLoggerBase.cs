using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace FeedReader.Logging
{
    public abstract class FeedReaderLoggerBase
    {
        public string LoggerName { get; set; }
        public LogLevel LogLevel { get; set; }
        public bool ShortSource { get; set; }
        public bool EnableTimestamp { get; set; }
        private LoggingController _loggingController;
        public LoggingController LogController
        {
            get { return _loggingController; }
            set
            {
                if (_loggingController == value)
                    return;
                if(_loggingController != null)
                    _loggingController.PropertyChanged -= Controller_PropertyChanged;
                if (value == null)
                {
                    _loggingController = null;
                    return;
                }
                _loggingController = value;
                LoggerName = _loggingController.LoggerName;
                LogLevel = _loggingController.LogLevel;
                ShortSource = _loggingController.ShortSource;
                EnableTimestamp = _loggingController.EnableTimestamp;
                _loggingController.PropertyChanged -= Controller_PropertyChanged;
                _loggingController.PropertyChanged += Controller_PropertyChanged;
            }
        }

#pragma warning disable CA1707 // Identifiers should not contain underscores
        protected virtual void Controller_PropertyChanged(string propertyName, object propertyValue)
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            switch (propertyName)
            {
                case "LoggerName":
                    LoggerName = propertyValue?.ToString();
                    break;
                case "LogLevel":
                    LogLevel = (LogLevel)propertyValue;
                    break;
                case "ShortSource":
                    ShortSource = (bool)propertyValue;
                    break;
                case "EnableTimeStamp":
                    EnableTimestamp = (bool)propertyValue;
                    break;
                default:
                    break;
            }
        }

        public abstract void Trace(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0);
        public abstract void Debug(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0);
        public abstract void Info(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0);
        public abstract void Warning(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0);
#pragma warning disable CA1716 // Identifiers should not match keywords
        public abstract void Error(string message,
#pragma warning restore CA1716 // Identifiers should not match keywords
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0);
        public abstract void Exception(string message, Exception e,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0);

    }
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Exception = 5,
        Disabled = 6
    }
}
