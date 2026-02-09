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
            // values[0] is SelectedMod (object)
            // values[1] is IsUsed (bool)
            bool isModSelected = values[0] != null;
            bool isUsed = values[1] is bool b && b;

            return isModSelected && isUsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
