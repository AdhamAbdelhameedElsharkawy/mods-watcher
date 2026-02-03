using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class LibraryViewModel : BaseViewModel, IInitializable<ModdedApp>
    {
        private readonly INavigationService _navigationService;
        // private readonly IDataService _dataService;
        private ModdedApp _selectedApp;

        // The list of "Cards" shown in the UI
        public ObservableCollection<ModItemViewModel> Mods { get; set; }

        private ModItemViewModel _selectedMod;
        public ModItemViewModel SelectedMod
        {
            get => _selectedMod;
            set => SetProperty(ref _selectedMod, value);
        }

        public string AppName => _selectedApp?.Name ?? "Loading...";

        // Constructor handles Service Injection
        public LibraryViewModel(INavigationService navigationService /*, IDataService dataService */)
        {
            _navigationService = navigationService;
            // _dataService = dataService;
            Mods = new ObservableCollection<ModItemViewModel>();
        }

        // IInitializable Implementation: Receives the "Order" from the NavService
        public void Initialize(ModdedApp app)
        {
            _selectedApp = app;
            LoadLibrary();
        }

        private void LoadLibrary()
        {
            Mods.Clear();

            // Logic logic:
            // 1. Fetch Mod Shells where AppId == _selectedApp.Id
            // 2. For each Shell, fetch the current InstalledMod record
            // 3. Wrap them in the ModItemViewModel we discussed earlier

            /* var shells = _dataService.GetModsForApp(_selectedApp.Id);
            foreach(var shell in shells)
            {
                var installed = _dataService.GetInstalledModForShell(shell.Id);
                Mods.Add(new ModItemViewModel(shell, installed));
            }
            */
        }

        // --- Commands ---

        public ICommand GoToHistoryCommand => new RelayCommand(o =>
        {
            if (o is ModItemViewModel item)
            {
                // Pass the Guid and the current selected app as a Tuple
                _navigationService.NavigateTo<ModHistoryViewModel, (Guid, ModdedApp)>((item.Shell.Id, _selectedApp));
            }
        });

        public ICommand GoToCrawlerCommand => new RelayCommand(o =>
        {
            if (o is ModItemViewModel item)
            {
                // We pass a Tuple containing both the Mod and the current App
                _navigationService.NavigateTo<AvailableVersionsViewModel, (Mod, ModdedApp)>((item.Shell, _selectedApp));
            }
        });

        public ICommand BackCommand => new RelayCommand(o =>
            _navigationService.NavigateTo<AppSelectionViewModel>());
    }
}