using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.Views;
using System.Windows;

namespace ModsWatcher.Desktop.Services
{
    public class WpfDialogService : IDialogService
    {
        public bool ShowConfirmation(string message, string title)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowInfo(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task<List<CrawledLink>> ShowLinkSelectorAsync(IEnumerable<CrawledLink> links)
        {
            var vm = new LinkSelectorViewModel(links);
            var dialog = new LinkSelectorView { DataContext = vm };

            var result = dialog.ShowDialog();

            if (result == true)
            {
                return vm.Links.Where(x => x.IsSelected).ToList();
            }

            return new List<CrawledLink>(); // Return empty if cancelled
        }

        public async Task<(AvailableMod? Primary, List<AvailableMod> Selected)> ShowVersionSelectorAsync(List<AvailableMod> availableMods)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var vm = new VersionSelectorViewModel(availableMods);
                var window = new VersionSelectorWindow
                {
                    DataContext = vm,
                    Owner = Application.Current.MainWindow
                };

                vm.RequestClose = (result) =>
                {
                    try { window.DialogResult = result; } catch { }
                    window.Close();
                };

                if (window.ShowDialog() == true)
                {
                    return (vm.PrimaryMod, vm.SelectedMods);
                }

                return (null, new List<AvailableMod>());
            });
        }

        public string? ShowPrompt(string message, string title)
        {
            var dialog = new InputDialog(message, title);
            // Ensure the dialog appears over the main window
            dialog.Owner = App.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                return dialog.ResponseText;
            }
            return null;
        }
    }
}