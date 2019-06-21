using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using SyncSaberLib.Data;

namespace DataGUITests.DataConverters
{
    public class SongRankedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<ScoreSaberDifficulty> scoreSaberSongs)
            {
                return scoreSaberSongs.Any(s => s.Ranked == true) == true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SongRankedComparer : IComparer
    {
        private bool _direction;
        public SongRankedComparer() { }
        public SongRankedComparer(bool direction)
        {
            _direction = direction;
            if(_direction == true)
            {
                Console.WriteLine("True");
            }
        }
        public int Compare(object x, object y)
        {
            if (x is IEnumerable<ScoreSaberDifficulty> first && y is IEnumerable<ScoreSaberDifficulty> second)
            {
                return (_direction ? 1 : -1) * (first.Count().CompareTo(second.Count()));
            }
            else
                return 0;
        }
    }

    public class ScoreSaberDifficultiesCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<ScoreSaberDifficulty> scoreSaberSongs)
            {
                return scoreSaberSongs.Count();
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
