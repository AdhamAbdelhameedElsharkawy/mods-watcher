using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class AvailableVersionsViewModel : BaseViewModel, IInitializable<(Mod Shell, ModdedApp App)>
    {
        private readonly INavigationService _navigationService;
        // private readonly ICrawlerService _crawlerService;

        private Mod _shell;
        private ModdedApp _parentApp; // We store this to go back later
        public string ModName => _shell?.Name ?? "Loading...";

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public ObservableCollection<AvailableMod> Versions { get; set; }

        public ICommand InstallCommand { get; }
        public ICommand BackCommand { get; }

        public AvailableVersionsViewModel(INavigationService navigationService /*, ICrawlerService crawler */)
        {
            _navigationService = navigationService;
            // _crawlerService = crawler;
            Versions = new ObservableCollection<AvailableMod>();

            InstallCommand = new RelayCommand(async o => await InstallVersion(o));
            BackCommand = new RelayCommand(o => _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(_parentApp));
        }

        public void Initialize((Mod Shell, ModdedApp App) data)
        {
            _shell = data.Shell;
            _parentApp = data.App;
            OnPropertyChanged(nameof(ModName)); // Update UI with name
            StartScan();
        }

        private async void StartScan()
        {
            IsScanning = true;
            Versions.Clear();

            // var results = await _crawlerService.ScanUrlAsync(_shell.SourceUrl);
            // foreach(var v in results) Versions.Add(v);

            IsScanning = false;
        }

        private async Task InstallVersion(object parameter)
        {
            if (parameter is AvailableMod selectedVersion)
            {
                // 1. Trigger Download/Extract
                // 2. Save to DB as the new 'InstalledMod'
                // 3. Return to Library
                // _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(_shell.App);
            }
        }
    }
}