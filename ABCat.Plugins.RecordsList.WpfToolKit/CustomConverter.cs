using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Plugins.RecordsList.WpfToolKit
{
    public class CustomConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var iAudioBook = values[1] as IAudioBook;

            if (iAudioBook == null) return null;

            var isSelected = (bool) values[0];

            var index = (int) values[2];

            if (isSelected)
            {
                return new SolidColorBrush(Color.FromArgb(255, 203, 232, 246));
            }

            var updated = (DateTime.Now - iAudioBook.LastUpdate).TotalDays;
            if (updated > 30)
                return index % 2 != 0
                    ? new SolidColorBrush(Color.FromArgb(255, 250, 235, 235))
                    : new SolidColorBrush(Color.FromArgb(255, 255, 240, 240));

            var created = (DateTime.Now - iAudioBook.Created).TotalDays;
            if (created < 1)
                return index % 2 != 0
                    ? new SolidColorBrush(Color.FromArgb(255, 235, 250, 235))
                    : new SolidColorBrush(Color.FromArgb(255, 240, 255, 240));
            if (created < 7)
                return index % 2 != 0
                    ? new SolidColorBrush(Color.FromArgb(255, 250, 250, 235))
                    : new SolidColorBrush(Color.FromArgb(255, 255, 255, 240));
            return index % 2 != 0
                ? new SolidColorBrush(Color.FromArgb(255, 250, 250, 250))
                : new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}