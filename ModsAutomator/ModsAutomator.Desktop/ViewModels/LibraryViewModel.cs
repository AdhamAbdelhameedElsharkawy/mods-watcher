using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

        public bool CanToggleActivation => SelectedMod?.Installed != null;

        // --- Commands ---
        public ICommand NavToRetiredCommand { get; }
        public ICommand AddModShellCommand { get; }
        public ICommand EditModShellCommand { get; }

        // Navigation to Versions Manager
        public ICommand NavToVersionsManagerCommand { get; } // Global (App Mode)
        public ICommand NavToSingleModVersionsCommand { get; } // Specific (Mod Mode)

        public ICommand ShowHistoryCommand { get; }
        public ICommand ToggleActivationCommand { get; }
        public ICommand HardWipeCommand { get; }

        public LibraryViewModel(INavigationService navigationService, IStorageService storageService)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            Mods = new ObservableCollection<ModItemViewModel>();

            // Initialization & Setup
            AddModShellCommand = new RelayCommand(_ => RegisterNewMod());
            EditModShellCommand = new RelayCommand(_ => EditSelectedModShell());

            // NAVIGATION FLOW
            NavToVersionsManagerCommand = new RelayCommand(_ =>
                _navigationService.NavigateTo<AvailableVersionsViewModel, (Mod? Shell, ModdedApp App)>((null, SelectedApp)));

            NavToSingleModVersionsCommand = new RelayCommand(obj => ExecuteNavToVersions(obj));

            // Misc Actions
            ShowHistoryCommand = new RelayCommand(_ => ViewModHistory());
            ToggleActivationCommand = new RelayCommand(_ => ToggleModActivation());
            HardWipeCommand = new RelayCommand(_ => HardWipeSelectedMod());
            NavToRetiredCommand = new RelayCommand(_ =>
                _navigationService.NavigateTo<RetiredModsViewModel, ModdedApp>(SelectedApp));
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

        private void ExecuteNavToVersions(object? obj)
        {
            // Use parameter if from card button, else use inspector selection
            var target = obj as ModItemViewModel ?? SelectedMod;
            if (target == null) return;

            _navigationService.NavigateTo<AvailableVersionsViewModel, (Mod? Shell, ModdedApp App)>(
                (target.Shell, SelectedApp));
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
                SelectedMod.Installed.IsUsed = !currentlyActive;

                // Persistence - Update the installation record state
                await _storageService.UpdateModShellAsync(SelectedMod.Shell);

                OnPropertyChanged(nameof(CanCrawlSelectedMod));
                LoadLibrary();
            }
        }

        private async void HardWipeSelectedMod()
        {
            if (SelectedMod == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to HARD WIPE '{SelectedMod.Shell.Name}'?\n\n" +
                "This will:\n" +
                "• Delete all current installation data.\n" +
                "• Delete available version lists.\n" +
                "• Move the Mod Identity to the Retired Archive.",
                "Confirm Hard Wipe", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _storageService.HardWipeModAsync(SelectedMod.Shell, SelectedApp);
                    SelectedMod = null;
                    LoadLibrary();
                    MessageBox.Show("Mod successfully retired.", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during wipe: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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