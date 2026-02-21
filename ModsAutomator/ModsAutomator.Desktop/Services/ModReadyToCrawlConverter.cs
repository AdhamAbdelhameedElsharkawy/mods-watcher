using ModsWatcher.Desktop.ViewModels;
using System.Globalization;
using System.Windows; // Required for Visibility
using System.Windows.Data;

namespace ModsWatcher.Desktop.Services
{
    public class ModReadyToCrawlConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Check if we have enough values and values[0] is our specific VM
            if (values.Length >= 2 && values[0] is ModItemViewModel modVM)
            {
                bool isUsed = values[1] is bool b && b;

                // Logic: Mod must be active AND the shell must allow crawling
                bool isReady = isUsed && modVM.Shell.IsCrawlable;

                // 2. Return Visibility if the XAML is binding to a Visibility property
                if (targetType == typeof(Visibility))
                {
                    return isReady ? Visibility.Visible : Visibility.Collapsed;
                }

                // Fallback for IsEnabled bindings
                return isReady;
            }

            // 3. If anything is null or wrong type, collapse the element
            return targetType == typeof(Visibility) ? Visibility.Collapsed : false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}