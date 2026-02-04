using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModsAutomator.Desktop.Services
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = (int)value;
            bool isInverted = parameter?.ToString() == "Inverted";

            if (isInverted)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;

            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
