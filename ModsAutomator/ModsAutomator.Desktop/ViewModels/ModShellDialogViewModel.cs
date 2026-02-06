using ModsAutomator.Core.Entities;
using ModsAutomator.Services.Interfaces;
using System.Windows;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModShellDialogViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;
        public Mod Shell { get; }
        public bool IsEditMode { get; }

        // UI logic
        public bool CanEditName => !IsEditMode;
        public string DialogTitle => IsEditMode ? "Edit Mod Identity" : "Register New Mod";

        public string Name
        {
            get => Shell.Name;
            set { Shell.Name = value; OnPropertyChanged(); }
        }

        public string RootSourceUrl
        {
            get => Shell.RootSourceUrl;
            set { Shell.RootSourceUrl = value; OnPropertyChanged(); }
        }

        public string? Description
        {
            get => Shell.Description;
            set { Shell.Description = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ModShellDialogViewModel(IStorageService storageService, int appId, Mod? existingMod = null)
        {
            _storageService = storageService;
            IsEditMode = existingMod != null;

            // Use provided Mod for Edit, or new Mod for Add (linking to the current App)
            Shell = existingMod ?? new Mod { AppId = appId };

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => Close(false));
        }

        private async Task SaveAsync()
        {
            //if (IsEditMode)
            //    await _storageService.UpdateModShellAsync(Shell);
            //else
            //    await _storageService.AddModShellAsync(Shell);

            //Close(true);
        }

        private void Close(bool result)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = result;
                    window.Close();
                }
            }
        }
    }
}