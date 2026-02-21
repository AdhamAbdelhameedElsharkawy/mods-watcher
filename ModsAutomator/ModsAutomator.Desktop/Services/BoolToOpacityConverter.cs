using System.Globalization;
using System.Windows.Data;

namespace ModsWatcher.Desktop.Services
{
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the value is null or not a bool, we don't want a "half-faded" look
            if (!(value is bool b)) return 0.0;

            return b ? 1.0 : 0.4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
