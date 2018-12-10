using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ABCat.Core.Converters
{
    public class ValueToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = Visibility.Collapsed;
            if (value == null)
            {
                if (parameter.ToStringOrEmpty().ToLower() == "null") result = Visibility.Visible;
            }
            else
            {
                var variants = parameter.ToStringOrEmpty().Split('|');

                if (
                    variants.Any(
                        variant =>
                            string.Compare(value.ToString(), variant, StringComparison.InvariantCultureIgnoreCase) ==
                            0))
                {
                    result = Visibility.Visible;
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}