using System;
using System.Collections.Generic;
using System.Text;

namespace FeedReader.Logging
{
    public class LoggingController
    {
        public static LoggingController DefaultLogController { get; protected set; }
        static LoggingController()
        {
            DefaultLogController = new LoggingController()
            {
                LoggerName = "FeedReader",
                ShortSource = true,
                LogLevel = LogLevel.Debug,
                EnableTimestamp = false
            };
        }
        private string _loggerName;
        private LogLevel _logLevel;
        private bool _shortSource;
        private bool _enableTimeStamp;

        public string LoggerName
        {
            get { return _loggerName; }
            set
            {
                if (_loggerName != value)
                {
                    _loggerName = value;
                    PropertyChanged?.Invoke("LoggerName", value);
                }
            }
        }
        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set
            {
                if (_logLevel != value)
                {
                    _logLevel = value;
                    PropertyChanged?.Invoke("LogLevel", value);
                }
            }
        }
        public bool ShortSource
        {
            get { return _shortSource; }
            set
            {
                if (_shortSource != value)
                {
                    _shortSource = value;
                    PropertyChanged?.Invoke("ShortSource", value);
                }
            }
        }
        public bool EnableTimestamp
        {
            get { return _enableTimeStamp; }
            set
            {
                if (_enableTimeStamp != value)
                {
                    _enableTimeStamp = value;
                    PropertyChanged?.Invoke("EnableTimestamp", value);
                }
            }
        }

        public event Action<string, object> PropertyChanged;

    }
}
