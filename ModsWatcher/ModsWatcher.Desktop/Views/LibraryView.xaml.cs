using ModsWatcher.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModsWatcher.Desktop.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl
    {
        public LibraryView()
        {
            InitializeComponent();
        }

        private async void ModSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            string query = textBox.Text.ToLower();

            // Rule: Only search after 3 characters
            if (query.Length >= 3)
            {
                // Find the first mod that contains the search string
                var match = ModListBox.Items.Cast<ModItemViewModel>()
                    .FirstOrDefault(m => m.Name.ToLower().Contains(query));

                if (match != null)
                {
                    ModListBox.SelectedItem = match;
                    ModListBox.ScrollIntoView(match);
                }
            }
        }
    }
}
