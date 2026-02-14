using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ModsAutomator.Desktop.Services
{
    public class CountToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
            {
                // Returns Amber/Gold if updates are pending
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
            }

            // Returns Muted Gray if no updates
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
