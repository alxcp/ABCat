using System;
using System.Globalization;
using System.Windows.Data;
using ABCat.Shared;

namespace ABCat.Core.Editors
{
    public class LocalizationValueConverter : IValueConverter
    {
        public Type TargetType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Enum) value).GetDescription();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToStringOrEmpty().GetEnumValueFromDescription(TargetType);
        }
    }
}