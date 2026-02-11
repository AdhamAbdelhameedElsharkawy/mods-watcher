using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace ModsAutomator.Desktop.ViewModels
{
    
    //TODO: Crawler logic Pending
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
            set
            {
                if (SetProperty(ref _selectedMod, value))
                {
                    OnPropertyChanged(nameof(CanToggleActivation));
                    OnPropertyChanged(nameof(CanCrawlSelectedMod));
                }
            }
        }

        public ModdedApp SelectedApp
        {
            get => _selectedApp;
            set => SetProperty(ref _selectedApp, value);
        }

        public bool CanCrawlSelectedMod =>
    SelectedMod != null &&
    SelectedMod.Installed != null &&
    SelectedMod.Installed.IsUsed;

        // This returns true only if the mod has an installation record
        public bool CanToggleActivation => SelectedMod?.Installed != null;

        // --- Commands ---

        public ICommand NavToRetiredCommand { get; }
        public ICommand AddModShellCommand { get; }
        public ICommand EditModShellCommand { get; }
        public ICommand CrawlAppCommand { get; }
        public ICommand GoToCrawlerCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ToggleModActivationCommand { get; }
        public ICommand HardWipeCommand { get; }

        public LibraryViewModel(INavigationService navigationService, IStorageService storageService)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            Mods = new ObservableCollection<ModItemViewModel>();

            // Initialize Commands
            AddModShellCommand = new RelayCommand(_ => RegisterNewMod());
            EditModShellCommand = new RelayCommand(_ => EditSelectedModShell());

            // Map the rest of the buttons
            CrawlAppCommand = new RelayCommand(_ => CrawlAllMods());
            GoToCrawlerCommand = new RelayCommand(obj => ExecuteCrawl(obj));
            ShowHistoryCommand = new RelayCommand(_ => ViewModHistory());
            ToggleModActivationCommand = new RelayCommand(_ => ToggleModActivation());
            HardWipeCommand = new RelayCommand(_ => HardWipeSelectedMod());
            // For mods that were Hard Wiped (UnusedModHistory)
            NavToRetiredCommand = new RelayCommand(_ => _navigationService.NavigateTo<RetiredModsViewModel, ModdedApp>(SelectedApp));

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
            _navigationService.NavigateTo<ModHistoryViewModel, (Mod, ModdedApp)>((SelectedMod.Shell, SelectedApp));
        }

        private async void ToggleModActivation()
        {
            if (SelectedMod?.Installed == null) return;

            bool currentlyActive = SelectedMod.Installed.IsUsed;
            string action = currentlyActive ? "Deactivate" : "Activate";

            var result = MessageBox.Show($"{action} {SelectedMod.Shell.Name}?",
                $"{action} Mod", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Toggle the state
                SelectedMod.Installed.IsUsed = !currentlyActive;

                // Notify the UI that the Crawl button status changed
                OnPropertyChanged(nameof(CanCrawlSelectedMod));

                // Save to DB
                await _storageService.UpdateModShellAsync(SelectedMod.Shell);

                // Refresh UI
                LoadLibrary();
            }
        }

        private async void HardWipeSelectedMod()
        {
            if (SelectedMod == null) return;

            // 1. Confirm the destructive action
            var result = MessageBox.Show(
                $"Are you sure you want to HARD WIPE '{SelectedMod.Shell.Name}'?\n\n" +
                "This will:\n" +
                "• Delete all current installation data.\n" +
                "• Delete available version lists.\n" +
                "• Move the Mod Identity to the Retired Archive.\n\n" +
                "You can restore the shell later, but files will be gone.",
                "Confirm Hard Wipe",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 2. Capture a reason (Optional TODO: Replace with a custom Dialog view)
                    // For now, we use a default or a simple prompt logic
                    string reason = "User manually retired the mod.";

                    // 3. Execute the service call with the parent App context
                    // This populates the AppName and AppVersion in the history record
                    await _storageService.HardWipeModAsync(SelectedMod.Shell, SelectedApp);

                    // 4. Refresh the UI
                    SelectedMod = null; // Clear selection
                    LoadLibrary();

                    MessageBox.Show("Mod successfully retired to the archive.", "Operation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during wipe: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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