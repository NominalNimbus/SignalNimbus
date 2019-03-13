using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DebugService.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!(value is bool))
                throw new ArgumentException();

            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!(value is Visibility))
                throw new ArgumentException();

            return (Visibility)value == Visibility.Visible;
        }
    }
}