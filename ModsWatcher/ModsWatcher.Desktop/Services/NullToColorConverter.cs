using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ModsWatcher.Desktop.Services
{
    public class NullToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter?.ToString().Split('|');
            if (param == null || param.Length < 2) return Brushes.Transparent;

            var colorIfNotNull = (SolidColorBrush)new BrushConverter().ConvertFrom(param[0]);
            var colorIfNull = (SolidColorBrush)new BrushConverter().ConvertFrom(param[1]);

            return value != null ? colorIfNotNull : colorIfNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
