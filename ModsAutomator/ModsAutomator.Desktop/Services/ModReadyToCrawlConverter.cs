using ModsAutomator.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ModsAutomator.Desktop.Services
{
    public class ModReadyToCrawlConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] is SelectedMod (ModItemViewModel)
            // values[1] is IsUsed (bool)

            if (values[0] is ModItemViewModel modVM)
            {
                bool isUsed = values[1] is bool b && b;

                // Button is only ready if the mod is active AND the shell supports crawling
                return isUsed && modVM.Shell.IsCrawlable;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
