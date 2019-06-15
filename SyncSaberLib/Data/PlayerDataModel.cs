using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncSaberLib.Data
{
    public class PlayerDataModel : IScrapedDataModel<List<LevelStatsData>, LevelStatsData>
    {
        [JsonProperty("version")]
        public string version;
        [JsonProperty("localPlayers")]
        public List<PlayerData> localPlayers;
        // public List<PlayerData> guestPlayers; //?
        public int lastSelectedBeatmapDifficulty;

        [JsonIgnore]
        public override List<LevelStatsData> Data { get { return localPlayers?.FirstOrDefault()?.levelsStatsData; } }

        public PlayerDataModel()
        {
            ReadOnly = true;
        }

        public override void Initialize(string filePath)
        {
            throw new NotImplementedException();
        }

        public override void WriteFile(string filePath)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayerData
    {
        public string playerId;
        public string playerName;
        public bool shouldShowTutorialPrompt;
        public bool agreedToEula;
        // public PlayerGameplayModifiers gameplayModifiers;
        // public PlayerSpecificSettings playerSpecificSettings;
        // public PlayerOverallStatsData playerAllOverallStatsData;
        public List<LevelStatsData> levelsStatsData;
        // public List<MissionStatsData> missionStatsData;
        // public List<Something> showedMissionHelpIds;
        // public AchievementData achievementsData;

    }

    public class LevelStatsData
    {
        public string levelId;
        public int difficulty;
        public string beatmapCharacteristicName;
        public int highScore;
        public int maxCombo;
        public bool fullCombo;
        public int maxRank;
        public bool validScore;
        public int playCount;
    }
}
