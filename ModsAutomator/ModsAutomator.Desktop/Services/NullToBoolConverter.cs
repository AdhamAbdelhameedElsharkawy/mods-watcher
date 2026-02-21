using System.Globalization;
using System.Windows.Data;

namespace ModsWatcher.Desktop.Services
{
    using System.Windows; // Required for Visibility

    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isNotNull = value != null;
            string mode = parameter as string;

            // This handles the "Inverted" parameter for the Empty State icon
            if (mode == "Inverted")
            {
                return isNotNull ? Visibility.Collapsed : Visibility.Visible;
            }

            // This handles the normal view for the Mod Detail
            return isNotNull ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
