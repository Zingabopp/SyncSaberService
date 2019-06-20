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
    [Table("scoresaberdifficulties")]
    public class ScoreSaberDifficulty
    {
        [Key]
        public int ScoreSaberDifficultyId { get; set; }
        public string SongHash { get; set; }
        public string DifficultyName { get; set; }
        public int Scores { get; set; }
        public int ScoresPerDay { get; set; }
        public bool Ranked { get; set; }
        public float Stars { get; set; }
        public string Image { get; set; }

        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public string SongAuthorName { get; set; }
        public string LevelAuthorName { get; set; }
        public double BeatsPerMinute { get; set; }

        public DateTime ScrapedAt { get; set; }

        public Song Song { get; set; }

        public ScoreSaberDifficulty() { }
        public ScoreSaberDifficulty(ScoreSaberSong s)
        {
            ScoreSaberDifficultyId = s.uid;
            SongHash = s.hash;
            DifficultyName = s.difficulty;
            Scores = s.scores;
            ScoresPerDay = s.scores_day;
            Ranked = s.ranked;
            Stars = s.stars;
            Image = s.image;

            SongName = s.name;
            SongSubName = s.songSubName;
            SongAuthorName = s.songAuthorName;
            LevelAuthorName = s.levelAuthorName;
            BeatsPerMinute = s.bpm;

            ScrapedAt = s.ScrapedAt;
        }
    }
}
