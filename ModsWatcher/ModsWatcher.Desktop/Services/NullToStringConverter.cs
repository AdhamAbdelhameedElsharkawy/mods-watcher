using System.Globalization;
using System.Windows.Data;

namespace ModsWatcher.Desktop.Services
{
    public class NullToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter?.ToString().Split('|');
            if (param == null || param.Length < 2) return string.Empty;

            // param[0] = Text if NOT NULL
            // param[1] = Text if NULL
            return value != null ? param[0] : param[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
