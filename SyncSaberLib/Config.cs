using IniParser;
using IniParser.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static SyncSaberLib.Utilities;

namespace SyncSaberLib
{
    public static class OldConfig
    {
        public static class SettingKeys
        {
            public static KeyData AutoDownloadSongs
            {
                get
                {
                    return CreateKeyData("AutoDownloadSongs", "1");
                }
            }
            public static KeyData AutoUpdateSongs
            {
                get
                {
                    return CreateKeyData("AutoUpdateSongs", "1");
                }
            }
            public static KeyData BeastSaberUsername
            {
                get
                {
                    return CreateKeyData("BeastSaberUsername", "");
                }
            }
            public static KeyData DeleteOldVersions
            {
                get
                {
                    return CreateKeyData("DeleteOldVersions", "1");
                }
            }
            public static KeyData SyncBookmarksFeed
            {
                get
                {
                    return CreateKeyData("SyncBookmarksFeed", "1");
                }
            }
            public static KeyData SyncCuratorRecommendedFeed
            {
                get
                {
                    return CreateKeyData("SyncCuratorRecommendedFeed", "1");
                }
            }
            public static KeyData SyncFollowingsFeed
            {
                get
                {
                    return CreateKeyData("SyncFollowingsFeed", "1");
                }
            }
            public static KeyData MaxBookmarksPages
            {
                get
                {
                    return CreateKeyData("MaxBookmarksPages", "0");
                }
            }
            public static KeyData MaxCuratorRecommendedPages
            {
                get
                {
                    return CreateKeyData("MaxCuratorRecommendedPages", "0");
                }
            }
            public static KeyData MaxFollowingsPages
            {
                get
                {
                    return CreateKeyData("MaxFollowingsPages", "0");
                }
            }
            public static KeyData MaxBeatSaverPages
            {
                get
                {
                    return CreateKeyData("MaxBeatSaverPages", "5");
                }
            }
            public static KeyData BeatSaberPath
            {
                get
                {
                    
                    return CreateKeyData("BeatSaberPath", Path.Combine("C:", "Program Files (x86)", "Steam", "steamapps", "common", "Beat Saber"));
                }
            }
            public static KeyData DownloadTimeout
            {
                get
                {
                    return CreateKeyData("DownloadTimeout", "5");
                }
            }
            public static KeyData MaxConcurrentDownloads
            {
                get
                {
                    return CreateKeyData("MaxConcurrentDownloads", "3");
                }
            }
            public static KeyData MaxConcurrentPageChecks
            {
                get
                {
                    return CreateKeyData("MaxConcurrentPageChecks", "5");
                }
            }
            public static KeyData SyncTopPPFeed
            {
                get
                {
                    return CreateKeyData("SyncTopPPFeed", "0");
                }
            }
            public static KeyData MaxScoreSaberPages
            {
                get
                {
                    return CreateKeyData("MaxScoreSaberSongs", "50");
                }
            }
            public static KeyData LoggingLevel
            {
                get
                {
                    return CreateKeyData("LoggingLevel", "Info");
                }
            }
            public static KeyData SyncFavoriteMappersFeed
            {
                get
                {
                    return CreateKeyData("SyncFavoriteMappersFeed", "1");
                }
            }
            public static KeyData OculusQuest
            {
                get
                {
                    return CreateKeyData("OculusQuest", "False");
                }
            }
            public static KeyData QuestSongsPath
            {
                get
                {
                    return CreateKeyData("QuestSongsPath", "");
                }
            }

        }


        #region "Setting Properties"
        public static bool AutoDownloadSongs
        {
            get
            {
                return Settings.GetBool(SettingKeys.AutoDownloadSongs);
            }
            set
            {
                Settings[SettingKeys.AutoDownloadSongs.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool AutoUpdateSongs
        {
            get
            {
                return Settings.GetBool(SettingKeys.AutoUpdateSongs);
            }
            set
            {
                Settings[SettingKeys.AutoUpdateSongs.KeyName] = value.ToString();
                Write();
            }
        }

        public static string BeastSaberUsername
        {
            get
            {
                return Settings.GetString(SettingKeys.BeastSaberUsername);
            }
            set
            {
                Settings[SettingKeys.BeastSaberUsername.KeyName] = value.ToString();
                Write();
            }
        }

        public static int MaxFollowingsPages
        {
            get
            {
                KeyData setting = SettingKeys.MaxFollowingsPages;
                int val = Settings.GetInt(setting);
                if (val < 0)
                {
                    Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = int.Parse(setting.Value);
                }
                else { SettingError[setting.KeyName] = false; }

                return val;
            }
            set
            {
                Settings[SettingKeys.MaxFollowingsPages.KeyName] = value.ToString();
                Write();
            }
        }

        public static int MaxBookmarksPages
        {
            get
            {
                KeyData setting = SettingKeys.MaxBookmarksPages;
                int val = Settings.GetInt(setting);
                if (val < 0)
                {
                    Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = int.Parse(setting.Value); ;
                }
                else { SettingError[setting.KeyName] = false; }
                return val;
            }
            set
            {
                Settings[SettingKeys.MaxBookmarksPages.KeyName] = value.ToString();
                Write();
            }
        }

        public static int MaxCuratorRecommendedPages
        {
            get
            {
                KeyData setting = SettingKeys.MaxCuratorRecommendedPages;
                int val = Settings.GetInt(setting);
                if (val < 0)
                {
                    Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = int.Parse(setting.Value); ;
                }
                else { SettingError[setting.KeyName] = false; }
                return val;
            }
            set
            {
                Settings[SettingKeys.MaxCuratorRecommendedPages.KeyName] = value.ToString();
                Write();
            }
        }

        public static int MaxBeatSaverPages
        {
            get
            {
                KeyData setting = SettingKeys.MaxBeatSaverPages;
                int val = Settings.GetInt(setting);
                if (val < 1)
                {
                    //Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = int.Parse(setting.Value); ;
                }
                else { SettingError[setting.KeyName] = false; }
                return val;
            }
            set
            {
                Settings[SettingKeys.MaxBeatSaverPages.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool DeleteOldVersions
        {
            get
            {
                return Settings.GetBool(SettingKeys.DeleteOldVersions);
            }
            set
            {
                Settings[SettingKeys.DeleteOldVersions.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool SyncBookmarksFeed
        {
            get
            {
                return Settings.GetBool(SettingKeys.SyncBookmarksFeed);
            }
            set
            {
                Settings[SettingKeys.SyncBookmarksFeed.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool SyncCuratorRecommendedFeed
        {
            get
            {
                return Settings.GetBool(SettingKeys.SyncCuratorRecommendedFeed);
            }
            set
            {
                Settings[SettingKeys.SyncCuratorRecommendedFeed.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool SyncFollowingsFeed
        {
            get
            {
                return Settings.GetBool(SettingKeys.SyncFollowingsFeed);
            }
            set
            {
                Settings[SettingKeys.SyncFollowingsFeed.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool SyncTopPPFeed
        {
            get
            {
                return Settings.GetBool(SettingKeys.SyncTopPPFeed);
            }
            set
            {
                Settings[SettingKeys.SyncTopPPFeed.KeyName] = value.ToString();
                Write();
            }
        }

        public static int MaxScoreSaberSongs
        {
            get
            {
                KeyData setting = SettingKeys.MaxScoreSaberPages;
                int val = Settings.GetInt(setting);
                if (val < 1)
                {
                    //Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = int.Parse(setting.Value); ;
                }
                else { SettingError[setting.KeyName] = false; }
                return val;
            }
            set
            {
                Settings[SettingKeys.MaxScoreSaberPages.KeyName] = value.ToString();
                Write();
            }
        }

        public static string BeatSaberPath
        {
            get
            {
                KeyData setting = SettingKeys.BeatSaberPath;
                string path = Settings.GetString(setting);

                if (!IsBeatSaberDirectory(path))
                {
                    if (!SettingError[setting.KeyName]) // don't repeat error message over and over
                    {
                        Logger.Error($"Beat Saber path {path} is invalid. Couldn't find 'Beat Saber.exe'");
                        path = GetBeatSaberPathFromRegistry();
                        if (string.IsNullOrEmpty(path))
                            SettingError[setting.KeyName] = true;
                        else
                        {
                            Settings[SettingKeys.BeatSaberPath.KeyName] = path;
                            Write();
                        }
                        //throw new FileNotFoundException($"Unable to locate 'Beat Saber.exe' inside the specified BeatSaberPath folder {path}.");
                    }
                }
                else { SettingError[setting.KeyName] = false; }

                return path;
            }
            set
            {
                var bsDir = new DirectoryInfo(value);
                if (bsDir.GetFiles("Beat Saber.exe").Length > 0)
                    SettingError[SettingKeys.BeatSaberPath.KeyName] = false;
                Settings[SettingKeys.BeatSaberPath.KeyName] = value.ToString();
                Write();
            }
        }

        /// <summary>
        /// Timeout for Beat Saver downloads in seconds.
        /// </summary>
        public static int DownloadTimeout
        {
            get
            {
                KeyData setting = SettingKeys.DownloadTimeout;
                int val = Settings.GetInt(setting);
                if (val < 0)
                {
                    int defaultVal = int.Parse(setting.Value);
                    if (!SettingError[setting.KeyName]) // don't repeat error message over and over
                    {
                        Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {defaultVal} instead.");
                        SettingError[setting.KeyName] = true;
                    }
                    val = defaultVal;
                }
                else { SettingError[setting.KeyName] = false; }
                return val;
            }
            set
            {
                Settings[SettingKeys.DownloadTimeout.KeyName] = value.ToString();
                Write();
            }
        }

        public static int MaxConcurrentDownloads
        {
            get
            {
                KeyData setting = SettingKeys.MaxConcurrentDownloads;
                int val = Settings.GetInt(setting);
                if (val < 0)
                {
                    int defaultVal = int.Parse(setting.Value);
                    if (!SettingError[setting.KeyName]) // don't repeat error message over and over
                    {
                        Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {defaultVal} instead.");
                        SettingError[setting.KeyName] = true;
                    }
                    val = defaultVal; ;
                }
                else { SettingError[setting.KeyName] = false; }
                return val;
            }
            set
            {
                Settings[SettingKeys.MaxConcurrentDownloads.KeyName] = value.ToString();
                Write();
            }
        }

        public static int MaxConcurrentPageChecks
        {
            get
            {
                KeyData setting = SettingKeys.MaxConcurrentPageChecks;
                int val = Settings.GetInt(setting);
                if (val < 0)
                {
                    int defaultVal = int.Parse(setting.Value);
                    if (!SettingError[setting.KeyName]) // don't repeat error message over and over
                    {
                        Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {defaultVal} instead.");
                        SettingError[setting.KeyName] = true;
                    }
                    val = defaultVal; ;
                }
                else { SettingError[setting.KeyName] = false; }
                return val;
            }
            set
            {
                Settings[SettingKeys.MaxConcurrentPageChecks.KeyName] = value.ToString();
                Write();
            }
        }

        public static string LoggingLevel
        {
            get
            {
                KeyData setting = SettingKeys.LoggingLevel;
                string path = Settings.GetString(setting);
                return path;
            }
            set
            {
                try
                {
                    var logLevel = StrToLogLevel(value);
                    Settings[SettingKeys.LoggingLevel.KeyName] = value.ToString();
                    Write();
                }
                catch (InvalidCastException)
                {
                    Logger.Error($"{value} does not correspond to a valid LogLevel.");
                }

            }
        }

        public static bool SyncFavoriteMappersFeed
        {
            get
            {
                return Settings.GetBool(SettingKeys.SyncFavoriteMappersFeed);
            }
            set
            {
                Settings[SettingKeys.SyncFavoriteMappersFeed.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool OculusQuest
        {
            get
            {
                return Settings.GetBool(SettingKeys.OculusQuest);
            }
            set
            {
                Settings[SettingKeys.OculusQuest.KeyName] = value.ToString();
                Write();
            }
        }

        public static string QuestSongsPath
        {
            get
            {
                KeyData setting = SettingKeys.QuestSongsPath;
                string path = Settings.GetString(setting);


                return path;
            }
            set
            {
                var questDir = new DirectoryInfo(value);
                if (!questDir.Exists)
                    Logger.Warning($"Quest songs directory doesn't exist: {questDir.FullName}");
                Settings[SettingKeys.QuestSongsPath.KeyName] = questDir.FullName;
                Write();
            }
        }
        #endregion

        public static void ReadAllSettings()
        {
            object setting = null;
            setting = BeatSaberPath;
            setting = BeastSaberUsername;
            setting = OculusQuest;
            setting = QuestSongsPath;

            setting = SyncCuratorRecommendedFeed;
            setting = SyncBookmarksFeed;
            setting = SyncFollowingsFeed;
            setting = SyncTopPPFeed;
            setting = SyncFavoriteMappersFeed;

            setting = MaxCuratorRecommendedPages;
            setting = MaxBookmarksPages;
            setting = MaxFollowingsPages;
            setting = MaxScoreSaberSongs;
            setting = MaxBeatSaverPages;

            //setting = AutoDownloadSongs;
            //setting = AutoUpdateSongs;

            setting = DeleteOldVersions;
            setting = DownloadTimeout;
            setting = MaxConcurrentDownloads;
            setting = MaxConcurrentPageChecks;
            setting = LoggingLevel;

        }

        public static LogLevel StrToLogLevel(string lvlStr)
        {
            LogLevel level;
            switch (lvlStr.ToLower())
            {
                case "info":
                    level = LogLevel.Info;
                    break;
                case "debug":
                    level = LogLevel.Debug;
                    break;
                case "warn":
                    level = LogLevel.Warn;
                    break;
                case "error":
                    level = LogLevel.Error;
                    break;
                case "trace":
                    level = LogLevel.Trace;
                    break;
                default:
                    throw new InvalidCastException($"{lvlStr} does not correspond to a LogLevel");
            }
            return level;
        }

        // Using Path.Combine makes it safe for regions that don't use '\' as a directory separator?
        private static readonly string STEAM_REG_KEY = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Uninstall", "Steam App 620980");
        //private const string STEAM_REG_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980";
        private static readonly string OCULUS_REG_KEY = Path.Combine("SOFTWARE", "WOW6432Node", "Oculus VR, LLC", "Oculus", "Config");
        //private const string OCULUS_REG_KEY = @"SOFTWARE\WOW6432Node\Oculus VR, LLC\Oculus\Config";
        public static string GetBeatSaberPathFromRegistry()
        {
            bool isSteam = false;
            string path;
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))// Doesn't work in 32 bit mode without this
            {
                using (var steamKey = hklm?.OpenSubKey(STEAM_REG_KEY))
                    path = (string)steamKey?.GetValue("InstallLocation", string.Empty);
                if (string.IsNullOrEmpty(path))
                {
                    using (var oculusKey = hklm?.OpenSubKey(OCULUS_REG_KEY))
                        path = (string)oculusKey?.GetValue("InitialAppLibrary", string.Empty);
                    if (!string.IsNullOrEmpty(path))
                    {
                        path = Path.Combine(path, "Software", "hyperbolic-magnetism-beat-saber");
                    }
                }
                else
                {
                    isSteam = true;
                }
            }
            if (IsBeatSaberDirectory(path))
            {
                Logger.Info($"Found {(isSteam ? "Steam" : "Oculus")} installation of Beat Saber at {path}");
            }
            else
                path = string.Empty;
            return path;
        }

        private static Dictionary<string, bool> _errorStatus;
        private static Dictionary<string, bool> SettingError
        {
            get
            {
                if (_errorStatus == null)
                {
                    _errorStatus = new Dictionary<string, bool>(){
                        { SettingKeys.BeatSaberPath.KeyName, false },
                        { SettingKeys.BeastSaberUsername.KeyName, false },
                        { SettingKeys.AutoDownloadSongs.KeyName, false },
                        { SettingKeys.AutoUpdateSongs.KeyName, false },
                        { SettingKeys.MaxFollowingsPages.KeyName, false },
                        { SettingKeys.MaxBookmarksPages.KeyName, false },
                        { SettingKeys.MaxBeatSaverPages.KeyName, false },
                        { SettingKeys.MaxCuratorRecommendedPages.KeyName, false },
                        { SettingKeys.DeleteOldVersions.KeyName, false },
                        { SettingKeys.SyncBookmarksFeed.KeyName, false },
                        { SettingKeys.SyncCuratorRecommendedFeed.KeyName, false },
                        { SettingKeys.SyncFollowingsFeed.KeyName, false },
                        { SettingKeys.MaxConcurrentDownloads.KeyName, false },
                        { SettingKeys.MaxConcurrentPageChecks.KeyName, false },
                        { SettingKeys.DownloadTimeout.KeyName, false },
                    };
                }
                return _errorStatus;
            }
        }
        public static List<String> Errors = new List<string>();
        private static List<KeyData> CriticalSettings = new List<KeyData>() {
            SettingKeys.BeatSaberPath
        };
        public static Dictionary<string, Func<string>> Setting;
        public static bool CriticalError
        {
            get
            {
                var CriticalSettingKeys = CriticalSettings.Select(k => k.KeyName);
                var settingErrorsKeys = SettingError.Where(s => s.Value == true).Select(s => s.Key);
                if (CriticalSettings.Select(k => k.KeyName).Intersect(SettingError.Where(s => s.Value == true).Select(s => s.Key)).Count() > 0)
                {
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool Initialize()
        {

            bool successful = true;
            bool changed = false;

            _iniFile.Refresh();
            if (!_iniFile.Exists)
            {
                _data = new IniData();
                changed = true;
            }
            else
                _data = _parser.ReadFile(_iniFile.FullName);
            if (!_data.Sections.ContainsSection(SectionName))
            {
                _data.Sections.Add(new SectionData(SectionName));
                changed = true;
            }
            var curSection = _data.Sections[SectionName];

            if (changed)
                _parser.WriteFile(_iniFile.Name, _data);

            ReadAllSettings();
            Setting = new Dictionary<string, Func<string>> {
                { SettingKeys.BeatSaberPath.KeyName, () => { return BeatSaberPath; } }
            };
            //Console.WriteLine($"Path: {Setting[SettingKeys.BeatSaberPath.KeyName]()}");
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Logger.Info($"SyncSaberService {version.Major}.{version.Minor}.{version.Build}");
            return successful;
        }

        public static bool GetBool(this KeyDataCollection settings, string keyName, bool defaultVal = false)
        {
            if (!settings.ContainsKey(keyName))
            {
                settings.AddKey(CreateKeyData(keyName, defaultVal.ToString()));
                Write();
            }
            bool successful = StrToBool(settings[keyName], out bool result, defaultVal);
            if (!successful)
                Logger.Warning($"Unable to parse {keyName} with value {settings[keyName]} as a boolean, using default value of {defaultVal} instead.");
            return result;
        }

        public static bool GetBool(this KeyDataCollection settings, KeyData data)
        {
            StrToBool(data.Value, out bool defaultVal);
            return settings.GetBool(data.KeyName, defaultVal);
        }

        public static int GetInt(this KeyDataCollection settings, string keyName, int defaultVal = 0)
        {
            if (!settings.ContainsKey(keyName))
            {
                settings.AddKey(CreateKeyData(keyName, defaultVal.ToString()));
                Write();
            }
            int val = defaultVal;
            bool successful = int.TryParse(settings[keyName], out val);
            if (!successful)
            {
                Logger.Warning($"Unable to parse {keyName} with value {settings[keyName]} as an integer, using default value of {defaultVal} instead.");
                val = defaultVal;
            }
            return val;
        }

        public static int GetInt(this KeyDataCollection settings, KeyData data, int defaultVal = 0)
        {
            return settings.GetInt(data.KeyName, int.Parse(data.Value));
        }

        public static string GetString(this KeyDataCollection settings, string keyName, string defaultVal = "")
        {
            if (!settings.ContainsKey(keyName))
            {
                settings.AddKey(CreateKeyData(keyName, defaultVal));
                Write();
            }
            return settings[keyName];
        }

        public static string GetString(this KeyDataCollection settings, KeyData data)
        {
            return settings.GetString(data.KeyName, data.Value);
        }

        public static void Write()
        {
            try
            {
                _parser.WriteFile(_iniFile.FullName, _data);
            }
            catch (Exception)
            {
                Logger.Error("Unable to write to Config file because it's in use.");
            }

        }

        private static FileIniDataParser _parser = new FileIniDataParser();

        private static IniData _data;

        private static FileInfo _iniFile = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SyncSaberService.ini"));

        public const string SectionName = "SyncSaberService";

        public static KeyDataCollection Settings
        {
            get
            {
                var dataSections = _data?.Sections?.ContainsSection(SectionName);
                if (dataSections == null || (dataSections == false))
                    Initialize();
                return _data.Sections[SectionName];
            }
            set
            {
                //Settings[SettingKeys.SyncFollowingsFeed.KeyName] = value.ToString();
                Write();
            }
        }


        public static bool IsBeatSaberDirectory(string path)
        {
            if (string.IsNullOrEmpty(path?.Trim()))
                return false;
            DirectoryInfo bsDir = new DirectoryInfo(path);
            bool valid = bsDir.Exists;
            if (bsDir.Exists)
            {
                var files = bsDir.GetFiles("Beat Saber.exe");
                return files.Count() > 0;
            }
            return false;
        }

        private static List<string> _favoriteMappers;
        public static List<string> FavoriteMappers
        {
            get
            {
                if (_favoriteMappers == null)
                    _favoriteMappers = new List<string>();
                if (_favoriteMappers.Count > 0)
                    return _favoriteMappers;
                FileInfo mapperFile = new FileInfo(Path.Combine(BeatSaberPath, "UserData", "Favoritemappers.ini"));
                //Logger.Debug($"MapperFile: {mapperFile.FullName}");

                if (mapperFile.Exists)
                {
                    foreach (var mapper in File.ReadAllLines(mapperFile.FullName))
                    {
                        if (string.IsNullOrWhiteSpace(mapper))
                            continue;
                        //Logger.Debug($"Mapper: {mapper} read from FavoriteMappers.ini");
                        if (!_favoriteMappers.Contains(mapper))
                            _favoriteMappers.Add(mapper);
                    }
                }

                return _favoriteMappers;
            }
        }

        /// <summary>
        /// Creates a new KeyData object with the provided keyName and keyValue;
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public static KeyData CreateKeyData(string keyName, string keyValue)
        {
            var data = new KeyData(keyName)
            {
                Value = keyValue
            };
            return data;
        }

        public static string AsString(int padding = 0)
        {
            string pad = "";
            for (int i = 0; i < padding; i++)
                pad = pad + " ";
            return $"{pad}AutoDownloadSongs: { AutoDownloadSongs}\n" +
                $"{pad}AutoUpdateSongs: {AutoUpdateSongs}\n" +
                $"{pad}BeastSaberUsername: {BeastSaberUsername}\n" +
                $"{pad}MaxFollowingPages: {MaxFollowingsPages}\n" +
                $"{pad}MaxBookmarksPages: {MaxBookmarksPages}\n" +
                $"{pad}MaxCuratorRecommendedPages: {MaxCuratorRecommendedPages}\n" +
                $"{pad}DeleteOldVersions: {DeleteOldVersions}\n" +
                $"{pad}SyncBookmarksFeed: {SyncBookmarksFeed}\n" +
                $"{pad}SyncCuratorRecommendedFeed: {SyncCuratorRecommendedFeed}\n" +
                $"{pad}SyncFollowingsFeed: {SyncFollowingsFeed}\n" +
                $"{pad}BeatSaberPath: {BeatSaberPath}";
        }
    }
}
