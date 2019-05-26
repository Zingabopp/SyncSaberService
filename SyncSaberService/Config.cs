using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IniParser;
using IniParser.Model;
using System.Reflection;
using static SyncSaberService.Utilities;

namespace SyncSaberService
{
    public static class Config
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
            public static KeyData BeastSaberPassword
            {
                get
                {
                    return CreateKeyData("BeastSaberPassword", "");
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
            public static KeyData BeatSaberPath
            {
                get
                {
                    return CreateKeyData("BeatSaberPath", @"C:\Program Files (x86)\Steam\steamapps\common\Beat Saber");
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
                    return CreateKeyData("MaxConcurrentDownloads", "2");
                }
            }
            public static KeyData MaxConcurrentPageChecks
            {
                get
                {
                    return CreateKeyData("MaxConcurrentPageChecks", "10");
                }
            }
        }

        

        private static Dictionary<string, bool> _errorStatus;
        private static Dictionary<string, bool> SettingError
        {
            get
            {
                if (_errorStatus == null)
                {
                    _errorStatus = new Dictionary<string, bool>(){
                        { SettingKeys.AutoDownloadSongs.KeyName, false },
                        { SettingKeys.AutoUpdateSongs.KeyName, false },
                        { SettingKeys.BeastSaberUsername.KeyName, false },
                        { SettingKeys.MaxFollowingsPages.KeyName, false },
                        { SettingKeys.MaxBookmarksPages.KeyName, false },
                        { SettingKeys.MaxCuratorRecommendedPages.KeyName, false },
                        { SettingKeys.DeleteOldVersions.KeyName, false },
                        { SettingKeys.SyncBookmarksFeed.KeyName, false },
                        { SettingKeys.SyncCuratorRecommendedFeed.KeyName, false },
                        { SettingKeys.SyncFollowingsFeed.KeyName, false },
                        { SettingKeys.BeatSaberPath.KeyName, false },
                        { SettingKeys.BeastSaberPassword.KeyName, false },
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

        public static void ReadAllSettings()
        {
            object setting = AutoDownloadSongs;
            setting = AutoUpdateSongs;
            setting = BeastSaberUsername;
            setting = BeastSaberPassword;
            setting = DeleteOldVersions;
            setting = SyncBookmarksFeed;
            setting = SyncCuratorRecommendedFeed;
            setting = SyncFollowingsFeed;
            setting = MaxBookmarksPages;
            setting = MaxCuratorRecommendedPages;
            setting = MaxFollowingsPages;
            setting = DownloadTimeout;
            setting = MaxConcurrentDownloads;
            setting = MaxConcurrentPageChecks;
            setting = BeatSaberPath;
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
            Console.WriteLine($"Path: {Setting[SettingKeys.BeatSaberPath.KeyName]()}");
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
                if (_data == null)
                    if (!_data.Sections.ContainsSection(SectionName))
                        Initialize();
                return _data.Sections[SectionName];
            }
            set
            {
                //Settings[SettingKeys.SyncFollowingsFeed.KeyName] = value.ToString();
                Write();
            }
        }

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

        public static string BeastSaberPassword
        {
            get
            {
                return Settings.GetString(SettingKeys.BeastSaberPassword);
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
                if (val < 1)
                {
                    //Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = 30;
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
                if (val < 1)
                {
                    //Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = 30;
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
                if (val < 1)
                {
                    //Logger.Warning($"Value of {val} is invalid for setting {setting.KeyName}, using {setting.Value} instead.");
                    SettingError[setting.KeyName] = true;
                    val = 30;
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
                        SettingError[setting.KeyName] = true;
                        //throw new FileNotFoundException($"Unable to locate 'Beat Saber.exe' inside the specified BeatSaberPath folder {path}.");
                    }
                }
                else { SettingError[setting.KeyName] = false; }

                return path;
            }
            set
            {
                var bsDir = new DirectoryInfo(value);
                if(bsDir.GetFiles("Beat Saber.exe").Length > 0)
                    SettingError[SettingKeys.BeatSaberPath.KeyName] = false;
                Settings[SettingKeys.BeatSaberPath.KeyName] = value.ToString();
                Write();
            }
        }

        public static bool IsBeatSaberDirectory(string path)
        {
            DirectoryInfo bsDir = new DirectoryInfo(path);
            bool valid = bsDir.Exists;
            if (bsDir.Exists)
            {
                var files = bsDir.GetFiles("Beat Saber.exe");
                return files.Count() > 0;
            }
            return false;
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

        private static List<string> _favoriteMappers;
        public static List<string> FavoriteMappers
        {
            get
            {
                if (_favoriteMappers == null)
                    _favoriteMappers = new List<string>();
                FileInfo mapperFile = new FileInfo(Path.Combine(BeatSaberPath, "UserData\\FavoriteMappers.ini"));
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

        public static string AsString(int padding = 0)
        {
            string pad = "";
            for (int i = 0; i < padding; i++)
                pad = pad + " ";
            return $"{pad}AutoDownloadSongs: { AutoDownloadSongs}\n" +
                $"{pad}AutoUpdateSongs: {AutoUpdateSongs}\n" +
                $"{pad}BeastSaberUsername: {BeastSaberUsername}\n" +
                $"{pad}BeastSaberPassword: {BeastSaberPassword}\n" +
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
