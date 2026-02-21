using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class RetiredModsViewModel : BaseViewModel, IInitializable<ModdedApp>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;
        private ModdedApp _parentApp;

        public ObservableCollection<UnusedModHistory> RetiredMods { get; } = new();

        public bool HasNoRetiredMods => RetiredMods.Count == 0;

        public RetiredModsViewModel(INavigationService navigationService, IStorageService storageService, IDialogService dialogService, ILogger logger) : base(logger)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            _dialogService = dialogService;
        }

        public async void Initialize(ModdedApp app)
        {
            _parentApp = app;
            await LoadRetiredMods();
        }

        private async Task LoadRetiredMods()
        {
            RetiredMods.Clear();
            var history = await _storageService.GetRetiredModsByAppIdAsync(_parentApp.Id);

            foreach (var item in history)
            {
                RetiredMods.Add(item);
            }

            OnPropertyChanged(nameof(HasNoRetiredMods));
        }

        public ICommand RestoreCommand => new RelayCommand(async (o) =>
        {
            if (o is UnusedModHistory historyItem)
            {
                // Version Guard
                if (_parentApp.InstalledVersion != historyItem.AppVersion)
                {
                    _logger.LogWarning("User attempted to restore mod '{ModName}' which was retired under app version {RetiredVersion}, but current app version is {CurrentVersion}. Restoration blocked.",
                        historyItem.Name, historyItem.AppVersion, _parentApp.InstalledVersion);
                    _dialogService.ShowError(
                        $"Cannot restore mod. This mod was retired for app version {historyItem.AppVersion}, " +
                        $"but the current app version is {_parentApp.InstalledVersion}.",
                        "Version Mismatch");
                    return;
                }

                // Confirmation before bringing it back
                if (_dialogService.ShowConfirmation($"Restore '{historyItem.Name}' to your active library?", "Confirm Restoration"))
                {
                    await _storageService.RestoreModFromHistoryAsync(historyItem);
                    _logger.LogInformation("User restored mod '{ModName}' from retired history for app '{AppName}'.", historyItem.Name, _parentApp.Name);
                    await LoadRetiredMods(); // Refresh list
                }
            }
        });

        public ICommand BackCommand => new RelayCommand(o =>
        {
            _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(_parentApp);
        });
    }
}