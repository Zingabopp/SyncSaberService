using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace SyncSaberLib.Data
{
    public class SongDataContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }
        public DbSet<ScoreSaberDifficulty> ScoreSaberDifficulties { get; set; }
        public DbSet<Characteristic> Characteristics { get; set; }
        public DbSet<Difficulty> Difficulties { get; set; }
        public DbSet<Uploader> Uploaders { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=songs.db");

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>()
                .HasKey(s => s.Hash);
            modelBuilder.Entity<ScoreSaberDifficulty>()
                .HasKey(d => d.ScoreSaberDifficultyId);
            modelBuilder.Entity<Characteristic>()
                .HasKey(c => c.CharacteristicId);
            modelBuilder.Entity<Difficulty>()
                .HasKey(d => d.DifficultyId);
            modelBuilder.Entity<Difficulty>()
                .HasAlternateKey(d => d.DifficultyName);
            modelBuilder.Entity<Uploader>()
                .HasKey(u => u.UploaderId);
            modelBuilder.Entity<Uploader>()
                .HasAlternateKey(u => u.UploaderName);

            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasKey(b => new { b.CharactersticId, b.SongId });
            modelBuilder.Entity<SongDifficulty>()
                .HasKey(d => new { d.DifficultyId, d.SongId });



            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasOne(b => b.Characteristic)
                .WithMany(b => b.BeatmapCharacteristics)
                .HasForeignKey(b => b.CharactersticId);

            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasOne(b => b.Song)
                .WithMany(b => b.BeatmapCharacteristics)
                .HasForeignKey(b => b.SongId);

            modelBuilder.Entity<SongDifficulty>()
                .HasOne(b => b.Difficulty)
                .WithMany(b => b.SongDifficulties)
                .HasForeignKey(b => b.DifficultyId);

            modelBuilder.Entity<SongDifficulty>()
                .HasOne(b => b.Song)
                .WithMany(b => b.Difficulties)
                .HasForeignKey(b => b.SongId);


        }


    }
}
