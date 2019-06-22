using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncSaberLib.Data
{
    [Table("songs")]
    public class Song
    {
        #region Main
        [Key]
        public string SongId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [Key]
        public string Hash { get; set; }
        public DateTime Uploaded { get; set; }
        public string DownloadUrl { get; set; }
        public string CoverUrl { get; set; }
        #endregion
        #region Metadata
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public string SongAuthorName { get; set; }
        public string LevelAuthorName { get; set; }
        public double BeatsPerMinute { get; set; }
        #endregion
        #region Stats
        public int Downloads { get; set; }
        public int Plays { get; set; }
        public int DownVotes { get; set; }
        public int UpVotes { get; set; }
        public double Heat { get; set; }
        public double Rating { get; set; }
        #endregion

        public DateTime ScrapedAt { get; set; }

        public virtual ICollection<SongDifficulty> Difficulties { get; set; }
        public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }
        [ForeignKey("SongHash")]
        public virtual ICollection<ScoreSaberDifficulty> ScoreSaberDifficulties { get; set; }

        public string UploaderRefId { get; set; }
        public Uploader Uploader { get; set; }

        public Song()
        {
        }

        public Song(SongInfo s)
        {
            SongId = s.BeatSaverInfo._id;
            Key = s.BeatSaverInfo.key.ToLower();
            Name = s.BeatSaverInfo.name;
            Description = s.BeatSaverInfo.description;
            Hash = s.BeatSaverInfo.hash.ToUpper();
            Uploaded = s.BeatSaverInfo.uploaded;
            DownloadUrl = s.BeatSaverInfo.downloadURL;
            CoverUrl = s.BeatSaverInfo.coverURL;

            SongName = s.BeatSaverInfo.metadata.songName;
            SongSubName = s.BeatSaverInfo.metadata.songSubName;
            SongAuthorName = s.BeatSaverInfo.metadata.songAuthorName;
            LevelAuthorName = s.BeatSaverInfo.metadata.levelAuthorName;
            BeatsPerMinute = s.BeatSaverInfo.metadata.bpm;

            Downloads = s.BeatSaverInfo.stats.downloads;
            Plays = s.BeatSaverInfo.stats.plays;
            DownVotes = s.BeatSaverInfo.stats.downVotes;
            UpVotes = s.BeatSaverInfo.stats.upVotes;
            Heat = s.BeatSaverInfo.stats.heat;
            Rating = s.BeatSaverInfo.stats.rating;

            ScrapedAt = s.BeatSaverInfo.ScrapedAt;

            Difficulties = Difficulty.DictionaryToDifficulties(s.BeatSaverInfo.metadata.difficulties).Select(d =>
            new SongDifficulty() { Difficulty = d, Song = this, SongId = s.BeatSaverInfo._id }).ToList();
            BeatmapCharacteristics = Characteristic.ConvertCharacteristics(s.BeatSaverInfo.metadata.characteristics).Select(c =>
            new BeatmapCharacteristic() { SongId = s.BeatSaverInfo._id, Song = this, Characteristic = c }).ToList();
            UploaderRefId = s.BeatSaverInfo.uploader.id;
            Uploader = new Uploader() { UploaderId = UploaderRefId, UploaderName = s.BeatSaverInfo.uploader.username };
            ScoreSaberDifficulties = s.ScoreSaberInfo.Values.Select(d => new ScoreSaberDifficulty(d)).ToList();
        }

    }

    [Table("characteristics")]
    public class Characteristic
    {
        [NotMapped]
        public static Dictionary<string, Characteristic> AvailableCharacteristics = new Dictionary<string, Characteristic>();
        static Characteristic()
        {
            AvailableCharacteristics = new Dictionary<string, Characteristic>
            {
                { "Standard", new Characteristic() { CharacteristicId = 0, CharacteristicName = "Standard" } },
                { "NoArrows", new Characteristic() { CharacteristicId = 1, CharacteristicName = "NoArrows" } },
                { "OneSaber", new Characteristic() { CharacteristicId = 2, CharacteristicName = "OneSaber" } },
                { "Lightshow", new Characteristic() { CharacteristicId = 3, CharacteristicName = "Lightshow" } }
            };
        }
        public static ICollection<Characteristic> ConvertCharacteristics(ICollection<string> characteristics)
        {
            List<Characteristic> retList = new List<Characteristic>();
            foreach (var c in characteristics)
            {

                if (!AvailableCharacteristics.ContainsKey(c))
                    AvailableCharacteristics.Add(c, new Characteristic() { CharacteristicName = c });
                retList.Add(AvailableCharacteristics[c]);
            }

            return retList;
        }

        public override string ToString()
        {
            return CharacteristicName;
        }

        [Key]
        public int CharacteristicId { get; set; }
        [Key]
        public string CharacteristicName { get; set; }
        public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }
    }

    [Table("BeatmapCharacteristics")]
    public class BeatmapCharacteristic
    {

        public int CharactersticId { get; set; }
        public Characteristic Characteristic { get; set; }

        public string SongId { get; set; }
        public Song Song { get; set; }

        public override string ToString()
        {
            return Characteristic.CharacteristicName;
        }
    }

    [Table("songdifficulties")]
    public class SongDifficulty
    {
        public int DifficultyId { get; set; }
        public Difficulty Difficulty { get; set; }

        public string SongId { get; set; }
        public Song Song { get; set; }

        public override string ToString()
        {
            return Difficulty?.DifficultyName;
        }
    }

    [Table("difficulties")]
    public class Difficulty
    {
        /// <summary>
        /// Use a dictionary of created Difficulties so it doesn't keep creating the same ones.
        /// </summary>
        [NotMapped]
        public static Dictionary<int, Difficulty> AvailableDifficulties;
        /// <summary>
        /// Initialize the standard Difficulties so they have the right ID.
        /// </summary>
        static Difficulty()
        {
            AvailableDifficulties = new Dictionary<int, Difficulty>
            {
                { 0, new Difficulty() { DifficultyId = 0, DifficultyName = "Easy" } },
                { 1, new Difficulty() { DifficultyId = 1, DifficultyName = "Normal" } },
                { 2, new Difficulty() { DifficultyId = 2, DifficultyName = "Hard" } },
                { 3, new Difficulty() { DifficultyId = 3, DifficultyName = "Expert" } },
                { 4, new Difficulty() { DifficultyId = 4, DifficultyName = "ExpertPlus" } }
            };
        }
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; }
        public virtual ICollection<SongDifficulty> SongDifficulties { get; set; }


        public static ICollection<Difficulty> DictionaryToDifficulties(Dictionary<string, bool> diffs)
        {
            List<Difficulty> difficulties = new List<Difficulty>();
            for (int i = 0; i < diffs.Count; i++)
            {
                if (diffs.Values.ElementAt(i))
                {
                    if (!AvailableDifficulties.ContainsKey(i))
                        AvailableDifficulties.Add(i, new Difficulty() { DifficultyId = i, DifficultyName = diffs.Keys.ElementAt(i) });
                    difficulties.Add(AvailableDifficulties[i]);
                }
            }
            return difficulties;
        }

        public override string ToString()
        {
            return DifficultyName;
        }
    }

    [Table("uploaders")]
    public class Uploader
    {
        [Key]
        public string UploaderId { get; set; }
        [Key]
        public string UploaderName { get; set; }
        [ForeignKey("UploaderRefId")]
        public virtual ICollection<Song> Songs { get; set; }

        public override string ToString()
        {
            return UploaderName;
        }
    }
}
