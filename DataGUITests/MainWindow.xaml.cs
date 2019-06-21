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
        private IIncludableQueryable<Song, ICollection<ScoreSaberDifficulty>> currentQuery;
        System.Linq.Expressions.Expression<Func<Song, bool>> currentExpression;
        public MainWindow()
        {
            InitializeComponent();
            currentExpression = (s) => s != null;
            System.Linq.Expressions.Expression<Func<Song, bool>> newExpression = (s) => s.ScoreSaberDifficulties.Count > 5;
            var test = System.Linq.Expressions.Expression.And(currentExpression.Reduce(), newExpression.ReduceExtensions());
            ScrapedDataProvider.Initialize(false);
            _context = ScrapedDataProvider.SongData;
            CollectionViewSource songViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("SongViewSource")));
            currentQuery = _context.Songs.Include(s => s.Difficulties).Include(s => s.BeatmapCharacteristics).Include(s => s.ScoreSaberDifficulties);
            _context.Difficulties.Load();
            //currentQuery.Where(s => s.ScoreSaberDifficulties.Count() > 5).Skip(skip).Take(10).Load();
            currentQuery.Where(currentExpression).Skip(skip).Take(10).Load();
            //_context.ScoreSaberDifficulties.Load();

            songViewSource.Source = _context.Songs.Local.ToObservableCollection();
            button.Content = _context.Songs.Local.Count.ToString();
            //SongGrid.ItemsSource = _context.Songs.Local.ToObservableCollection();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            skip += 10;
            currentQuery.Where(s => s.ScoreSaberDifficulties.Count() > 5).Skip(skip).Take(10).Load();
            button.Content = _context.Songs.Local.Count.ToString();
        }
    }



    /*
    public class SongTracker : IObservable<Song>
    {
        public SongTracker()
        {
            observers = new List<IObserver<Song>>();
        }
        private List<IObserver<Song>> observers;

        public IDisposable Subscribe(IObserver<Song> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber(observers, observer);
        }

        public void TrackSong(Song song)
        {
            foreach (var observer in observers)
            {
                if (song == null)
                    observer.OnError(new SongNotFoundException());
                else
                    observer.OnNext(song);
            }
        }

        public void EndTransmission()
        {
            foreach (var observer in observers.ToArray())
                if (observers.Contains(observer))
                    observer.OnCompleted();

            observers.Clear();
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<Song>> _observers;
            private IObserver<Song> _observer;

            public Unsubscriber(List<IObserver<Song>> observers, IObserver<Song> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                {
                    if (_observer != null && _observers.Contains(_observer))
                        _observers.Remove(_observer);
                }
            }
        }
    }

    public class SongNotFoundException : Exception
    {
        internal SongNotFoundException()
        { }
    }
    */
}
