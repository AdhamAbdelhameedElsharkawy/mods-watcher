using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace ModsAutomator.Desktop.ViewModels
{
    public class LibraryViewModel : BaseViewModel, IInitializable<ModdedApp>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private ModdedApp _selectedApp;
        private ModItemViewModel _selectedMod;

        public ObservableCollection<ModItemViewModel> Mods { get; set; }

        public ModItemViewModel SelectedMod
        {
            get => _selectedMod;
            set => SetProperty(ref _selectedMod, value);
        }

        public ModdedApp SelectedApp
        {
            get => _selectedApp;
            set => SetProperty(ref _selectedApp, value);
        }

        // --- Commands ---
        public ICommand NavToArchiveCommand { get; }
        public ICommand AddModShellCommand { get; }
        public ICommand EditModShellCommand { get; }
        public ICommand CrawlAppCommand { get; }
        public ICommand GoToCrawlerCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand RetireCommand { get; }
        public ICommand HardWipeCommand { get; }

        public LibraryViewModel(INavigationService navigationService, IStorageService storageService)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            Mods = new ObservableCollection<ModItemViewModel>();

            // Initialize Commands
            NavToArchiveCommand = new RelayCommand(_ => _navigationService.NavigateTo<AppSelectionViewModel>());
            AddModShellCommand = new RelayCommand(_ => RegisterNewMod());
            EditModShellCommand = new RelayCommand(_ => EditSelectedModShell());

            // Map the rest of the buttons
            CrawlAppCommand = new RelayCommand(_ => CrawlAllMods());
            GoToCrawlerCommand = new RelayCommand(obj => ExecuteCrawl(obj));
            ShowHistoryCommand = new RelayCommand(_ => ViewModHistory());
            RetireCommand = new RelayCommand(_ => RetireSelectedMod());
            HardWipeCommand = new RelayCommand(_ => HardWipeSelectedMod());
        }

        public void Initialize(ModdedApp app)
        {
            SelectedApp = app;
            LoadLibrary();
        }

        private async void LoadLibrary()
        {
            if (SelectedApp == null) return;
            Mods.Clear();

            var libraryData = await _storageService.GetModsByAppId(SelectedApp.Id);
            foreach (var (shell, installed) in libraryData)
            {
                Mods.Add(new ModItemViewModel(shell, installed));
            }
        }

        // --- Command Logic ---

        private void ExecuteCrawl(object? obj)
        {
            // Use the parameter if available (from list button), otherwise use SelectedMod (from inspector)
            var target = obj as ModItemViewModel ?? SelectedMod;
            if (target == null) return;

            // _navigationService.NavigateTo<AvailableModsViewModel, (Mod, ModdedApp)>((target.Shell, SelectedApp));
        }

        private void CrawlAllMods()
        {
            // Logic for batch crawling all mods in the current app
        }

        private void ViewModHistory()
        {
            if (SelectedMod == null) return;
            _navigationService.NavigateTo<ModHistoryViewModel, (Guid, ModdedApp)>((SelectedMod.Shell.Id, SelectedApp));
        }

        private async void RetireSelectedMod()
        {
            if (SelectedMod?.Installed == null) return;

            var result = MessageBox.Show($"Retire {SelectedMod.Shell.Name}? This will disable the mod but keep its files.",
                "Retire Mod", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SelectedMod.Installed.IsUsed = false;
                //TODO: This should ideally be a service method that handles the logic of retiring (and potentially moving to history), but for now we'll just update the flag and save.
                //await _storageService.UpdateInstalledModAsync(SelectedMod.Installed);
                LoadLibrary(); // Refresh UI
            }
        }

        //TODO: This should ideally be a service method that handles the logic of hard wiping (deleting files, moving to unused history, etc), but for now we'll just call a placeholder method and refresh the UI.
        private async void HardWipeSelectedMod()
        {
            //if (SelectedMod == null) return;

            //var result = MessageBox.Show($"WARNING: Hard Wipe will delete {SelectedMod.Shell.Name} and move it to Unused History. This cannot be undone.",
            //    "HARD WIPE", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            //if (result == MessageBoxResult.Yes)
            //{
            //    // This would be your service method that handles the migration to unusedModhistory
            //    await _storageService.HardWipeModAsync(SelectedMod.Shell.Id);
            //    LoadLibrary();
            //}
        }

        private void RegisterNewMod()
        {
            var vm = new ModShellDialogViewModel(_storageService, SelectedApp.Id);
            var dialog = new Views.ModShellDialog { DataContext = vm, Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true) LoadLibrary();
        }

        private void EditSelectedModShell()
        {
            if (SelectedMod == null) return;
            var vm = new ModShellDialogViewModel(_storageService, SelectedApp.Id, SelectedMod.Shell);
            var dialog = new Views.ModShellDialog { DataContext = vm, Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true) LoadLibrary();
        }
    }
}