using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Orc.FilterBuilder;
using SyncSaberLib.Data;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Extensions;
using System.Linq.Expressions;
using static SyncSaberLib.Utilities;

namespace DataGUITests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SongDataContext _context;
        private int skip = 0;
        private int take = 0;
        private IIncludableQueryable<Song, ICollection<ScoreSaberDifficulty>> currentQuery;
        private readonly CollectionViewSource songViewSource;
        public MainWindow()
        {
            InitializeComponent();

            ScrapedDataProvider.Initialize(false);
            _context = ScrapedDataProvider.SongData;
            songViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("SongViewSource")));
            currentQuery = _context.Songs.
                Include(s => s.Difficulties).
                Include(s => s.BeatmapCharacteristics).
                Include(s => s.Uploader).
                Include(s => s.ScoreSaberDifficulties);
            _context.Difficulties.Load();
            _context.Characteristics.Load();
            //currentQuery.Where(s => s.ScoreSaberDifficulties.Count() > 5).Skip(skip).Take(10).Load();
            take = 10;
            currentQuery.Where(s => s.BeatmapCharacteristics.Count > 0).Skip(skip).Take(take).Load();
            //_context.ScoreSaberDifficulties.Load();
            var characteristics = _context.Songs.
                Where(s => s.BeatmapCharacteristics.Count > 0).
                SelectMany(s => s.BeatmapCharacteristics.
                    Select(c => c.Characteristic.CharacteristicName)).
                Distinct().ToList();
            songViewSource.Source = _context.Songs.Local.ToObservableCollection();
            songViewSource.View.Filter = SongMatches;
            button.Content = _context.Songs.Local.Count.ToString();
            //SongGrid.ItemsSource = _context.Songs.Local.ToObservableCollection();
        }

        private bool SongMatches(object item)
        {
            if (item is Song song && song.ScoreSaberDifficulties != null)
            {
                bool retVal = song.ScoreSaberDifficulties.Any(d => d.Ranked == true);
                return retVal;
            }
            return false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //skip += 10;
            take += 1000;
            _context.Dispose();
            _context = new SongDataContext();
            songViewSource.Source = _context.Songs.Local.ToObservableCollection();
            songViewSource.View.Filter = SongMatches;
            currentQuery = _context.Songs.
                Include(s => s.Difficulties).
                Include(s => s.BeatmapCharacteristics).
                Include(s => s.Uploader).
                Include(s => s.ScoreSaberDifficulties);
            _context.Difficulties.Load();
            _context.Characteristics.Load();
            currentQuery.Where(s => s.ScoreSaberDifficulties.Count() > 5).Skip(skip).Take(take).Load();
            button.Content = _context.Songs.Local.Count.ToString();
            songViewSource.View.Refresh();

        }
    }
}
