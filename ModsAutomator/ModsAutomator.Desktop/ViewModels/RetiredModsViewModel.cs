using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class RetiredModsViewModel : BaseViewModel, IInitializable<ModdedApp>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private ModdedApp _parentApp;

        public ObservableCollection<UnusedModHistory> RetiredMods { get; } = new();

        public bool HasNoRetiredMods => RetiredMods.Count == 0;

        public RetiredModsViewModel(INavigationService navigationService, IStorageService storageService)
        {
            _navigationService = navigationService;
            _storageService = storageService;
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
                await _storageService.RestoreModFromHistoryAsync(historyItem);
                await LoadRetiredMods(); // Refresh list
            }
        });

        public ICommand BackCommand => new RelayCommand(o =>
        {
            _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(_parentApp);
        });
    }
}