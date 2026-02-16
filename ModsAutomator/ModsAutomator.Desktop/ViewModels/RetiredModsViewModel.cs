using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class RetiredModsViewModel : BaseViewModel, IInitializable<ModdedApp>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;
        private ModdedApp _parentApp;

        public ObservableCollection<UnusedModHistory> RetiredMods { get; } = new();

        public bool HasNoRetiredMods => RetiredMods.Count == 0;

        public RetiredModsViewModel(INavigationService navigationService, IStorageService storageService, IDialogService dialogService)
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