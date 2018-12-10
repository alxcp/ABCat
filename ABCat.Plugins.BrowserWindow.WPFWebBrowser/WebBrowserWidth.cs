using System;
using System.Globalization;
using System.Windows.Data;
using JetBrains.Annotations;

namespace ABCat.Plugins.BrowserWindow.WPFWebBrowser
{
    public class WebBrowserWidth : IValueConverter
    {
        public object Convert([NotNull] object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double) value + 5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}