using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace YMusicGameBarWidget.Converters
{
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool b = value is bool && (bool)value;
            if (parameter != null && string.Equals(parameter.ToString(), "Invert", StringComparison.OrdinalIgnoreCase))
            {
                b = !b;
            }
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
