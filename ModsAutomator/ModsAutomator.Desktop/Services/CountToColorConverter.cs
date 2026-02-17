using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ModsAutomator.Desktop.Services
{
    public class CountToColorConverter : IValueConverter
    {
        // Pre-define the brushes once. Freeze them so WPF can reuse them across threads.
        private static readonly SolidColorBrush AmberBrush = CreateFrozenBrush("#FFC107");
        private static readonly SolidColorBrush GrayBrush = CreateFrozenBrush("#444444");

        private static SolidColorBrush CreateFrozenBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze(); // Makes it read-only and faster for WPF to render
            return brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Safely handles null or non-ints by defaulting to Gray
            if (value is int count && count > 0)
            {
                return AmberBrush;
            }

            return GrayBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
