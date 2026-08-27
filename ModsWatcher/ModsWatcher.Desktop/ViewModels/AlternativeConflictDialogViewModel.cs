using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class AlternativeConflictDialogViewModel : BaseViewModel
    {
        // --- Display ---
        public string Title { get; }
        public string Subtitle { get; }

        // --- Result ---
        public bool Confirmed { get; private set; }

        // --- Commands ---
        public ICommand ConfirmSwapCommand { get; }
        public ICommand CancelCommand { get; }

        public AlternativeConflictDialogViewModel(string newModName, string activeModName, ILogger logger) : base(logger)
        {
            Title = "Alternative mod already in use";

            Subtitle = $"'{activeModName}' is currently active as an alternative to '{newModName}'. " +
                       $"Only one of them can be active at a time. Activating '{newModName}' will deactivate '{activeModName}'.";

            ConfirmSwapCommand = new RelayCommand(_ => CloseWith(true));
            CancelCommand = new RelayCommand(_ => CloseWith(false));
        }

        private void CloseWith(bool confirmed)
        {
            Confirmed = confirmed;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    return;
                }
            }
        }
    }
}
