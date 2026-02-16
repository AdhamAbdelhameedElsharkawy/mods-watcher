using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class LibraryViewModel : BaseViewModel, IInitializable<ModdedApp>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IWatcherService _watcherService;
        private readonly IDialogService _dialogService;
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
                    //OnPropertyChanged(nameof(CanCrawlSelectedMod));
                }
            }
        }

        public ModdedApp SelectedApp
        {
            get => _selectedApp;
            set
            {
                if (SetProperty(ref _selectedApp, value))
                {
                    UpdateModsWithAppVersion();
                }
            }
        }
        //Not used anymore
        //public bool CanCrawlSelectedMod =>
        //    SelectedMod != null &&
        //    SelectedMod.Installed != null &&
        //    SelectedMod.Installed.IsUsed;

        public bool CanToggleActivation => SelectedMod?.Installed != null;

        // --- Commands ---
        public ICommand NavToRetiredCommand { get; }
        public ICommand AddModShellCommand { get; }
        public ICommand EditModShellCommand { get; }
        public ICommand SyncAllModsCommand { get; }
        public ICommand SyncSingleModCommand { get; }
        public ICommand FullSyncSingleModCommand { get; }
        public ICommand NavToVersionsManagerCommand { get; }
        public ICommand NavToSingleModVersionsCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ToggleActivationCommand { get; }
        public ICommand HardWipeCommand { get; }

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        // NEW: Installation Management Commands
        public ICommand SetupManualInstallCommand { get; }
        public ICommand EditInstallationCommand { get; }

        public LibraryViewModel(INavigationService navigationService, IStorageService storageService, IWatcherService watcherService, IDialogService dialogService)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            _watcherService = watcherService;
            _dialogService = dialogService;
            Mods = new ObservableCollection<ModItemViewModel>();

            // Initialization & Setup
            AddModShellCommand = new RelayCommand(async _ => await RegisterNewMod());
            EditModShellCommand = new RelayCommand(async _ => await EditSelectedModShellAsync());
            SyncAllModsCommand = new RelayCommand(async _ => await SyncAllWatchableModsAsync());
            SyncSingleModCommand = new RelayCommand(async mod => await SyncSingleModAsync(mod as ModItemViewModel));

            FullSyncSingleModCommand = new RelayCommand(
    async obj =>
    {
        var target = obj as ModItemViewModel ?? SelectedMod;
        if (target != null)
        {
            await RunFullSync(target);
        }
    },
    obj =>
    {
        var target = obj as ModItemViewModel ?? SelectedMod;
        return target != null &&
               target.IsUsed &&
               target.Shell.IsWatchable &&
               target.Shell.IsCrawlable &&
               target.Config != null;
    }
);

            // NEW: Installation Logic
            SetupManualInstallCommand = new RelayCommand(async _ => await SetupManualInstallationAsync());
            EditInstallationCommand = new RelayCommand(async _ => await EditInstallationDataAsync());

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
            MoveUpCommand = new RelayCommand(async obj => await MoveModOrder(obj as ModItemViewModel, -1));
            MoveDownCommand = new RelayCommand(async obj => await MoveModOrder(obj as ModItemViewModel, 1));

        }

        public void Initialize(ModdedApp app)
        {
            SelectedApp = app;
            LoadLibrary();
        }

        private async Task LoadLibrary()
        {
            if (SelectedApp == null) return;
            Mods.Clear();

            // 1. Get the list of tuples: (Mod shell, InstalledMod installed, ModCrawlerConfig config)
            var libraryData = await _storageService.GetFullModsByAppId(SelectedApp.Id);

            // 2. Sort the tuples by the shell's PriorityOrder
            var sortedData = libraryData.OrderBy(x => x.Shell.PriorityOrder);

            // 3. Iterate over the SORTED data
            foreach (var (shell, installed, config) in sortedData)
            {
                // Use SelectedApp.Version (or your specific property name) for the constructor
                Mods.Add(new ModItemViewModel(shell, installed, config, SelectedApp.InstalledVersion));
            }
        }

        private void UpdateModsWithAppVersion()
        {
            if (Mods == null || SelectedApp == null) return;

            foreach (var mod in Mods)
            {
                mod.AppVersion = SelectedApp.InstalledVersion;
            }
        }

        // --- NEW LOGIC METHODS ---

        private async Task SetupManualInstallationAsync()
        {
            if (SelectedMod == null || SelectedMod.Installed != null) return;

            // 1. Create a blank Entity linked to this shell's ID
            var newInstallation = new InstalledMod
            {
                Id = SelectedMod.Shell.Id, // Linking property
                InstalledVersion = "1.0.0",
                InstalledDate = DateOnly.FromDateTime(DateTime.Now),
                PackageType = PackageType.Zip, // Default
            };

            // 2. Open Dialog
            if (await ShowInstallationDialog(newInstallation))
            {
                // 3. Persist new record
                await _storageService.SaveInstalledModAsync(newInstallation);

                // 4. Full reload to rebuild the triad in the UI
                await LoadLibrary();
            }
        }

        private async Task EditInstallationDataAsync()
        {
            if (SelectedMod?.Installed == null) return;

            // Open Dialog with the existing reference
            if (await ShowInstallationDialog(SelectedMod.Installed))
            {
                // Persist updates
                await _storageService.UpdateInstalledModAsync(SelectedMod.Installed);

                // Refresh UI components
                SelectedMod.RefreshSummary();
                OnPropertyChanged(nameof(SelectedMod));
            }
        }

        private async Task<bool> ShowInstallationDialog(InstalledMod installed)
        {
            var vm = new ModInstallationDialogViewModel(installed);
            var dialog = new Views.ModInstallationDialog
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };

            // ShowDialog blocks execution until Close(result) is called in VM
            var result = dialog.ShowDialog();
            return result == true;
        }

        // --- EXISTING METHODS ---

        private void ExecuteNavToVersions(object? obj)
        {
            var target = obj as ModItemViewModel ?? SelectedMod;
            if (target == null) return;
            _navigationService.NavigateTo<AvailableVersionsViewModel, (Mod? Shell, ModdedApp App)>((target.Shell, SelectedApp));
        }

        private void ViewModHistory()
        {
            if (SelectedMod == null) return;
            _navigationService.NavigateTo<ModHistoryViewModel, (Mod, ModdedApp)>((SelectedMod.Shell, SelectedApp));
        }

        private async void ToggleModActivation()
        {
            if (SelectedMod?.Installed == null) return;

            bool currentlyActive = SelectedMod.IsUsed;
            string action = currentlyActive ? "Deactivate" : "Activate";

            // Using the IDialogService pattern
            if (_dialogService.ShowConfirmation($"{action} {SelectedMod.Shell.Name}?", $"{action} Mod"))
            {
                // 1. Update the Shell property
                SelectedMod.IsUsed = !currentlyActive;

                // 2. Persist to the correct table (Mod Shell)
                await _storageService.UpdateModShellAsync(SelectedMod.Shell);

                // 3. Refresh UI notifications
                SelectedMod.RefreshSummary();
                OnPropertyChanged(nameof(CanToggleActivation));
            }
            
        }

        private async void HardWipeSelectedMod()
        {
            if (SelectedMod == null) return;

            // Using your IDialogService for consistency
            if (_dialogService.ShowConfirmation($"Are you sure you want to HARD WIPE '{SelectedMod.Shell.Name}'?", "Confirm Hard Wipe"))
            {
                await _storageService.HardWipeModAsync(SelectedMod.Shell, SelectedApp);
                SelectedMod = null;
                await LoadLibrary();
            }
            
        }

        private async Task RegisterNewMod()
        {
            var vm = new ModShellDialogViewModel(_storageService, SelectedApp.Id);
            var dialog = new Views.ModShellDialog { DataContext = vm, Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true) await LoadLibrary();
        }

        private async Task EditSelectedModShellAsync()
        {
            if (SelectedMod == null) return;
            var config = await _storageService.GetModCrawlerConfigByModIdAsync(SelectedMod.Shell.Id);
            var vm = new ModShellDialogViewModel(_storageService, SelectedApp.Id, SelectedMod.Shell, config);
            var dialog = new Views.ModShellDialog { DataContext = vm, Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true) await LoadLibrary();
        }

        private async Task SyncAllWatchableModsAsync()
        {
            var targetMods = Mods.Where(m => m.IsUsed && m.Shell.IsWatchable && m.Config != null).ToList();
            if (targetMods.Any())
            {
                var watchList = targetMods.Select(m => (m.Shell, m.Config)).ToList();
                await _watcherService.RunStatusCheckAsync(watchList);
                foreach (var mod in targetMods) mod.RefreshSummary();
            }
        }

        private async Task SyncSingleModAsync(ModItemViewModel? mod)
        {
            if (mod == null || !mod.Shell.IsWatchable || mod.Config == null) return;
            var watchList = new List<(Mod, ModCrawlerConfig)> { (mod.Shell, mod.Config) };
            await _watcherService.RunStatusCheckAsync(watchList);
            mod.RefreshSummary();
        }

        private async Task MoveModOrder(ModItemViewModel? mod, int direction)
        {
            if (mod == null) return;

            int oldIndex = Mods.IndexOf(mod);
            int newIndex = oldIndex + direction;

            // Boundary check
            if (newIndex < 0 || newIndex >= Mods.Count) return;

            var targetMod = Mods[newIndex];

            // 1. Swap the PriorityOrder values in the Shell entities
            int tempOrder = mod.Shell.PriorityOrder;
            mod.Shell.PriorityOrder = targetMod.Shell.PriorityOrder;
            targetMod.Shell.PriorityOrder = tempOrder;

            // 2. Persist changes to DB
            await _storageService.UpdateModShellAsync(mod.Shell);
            await _storageService.UpdateModShellAsync(targetMod.Shell);

            // 3. Update the UI collection (Swap positions)
            Mods.Move(oldIndex, newIndex);
        }

        public async Task RunFullSync(ModItemViewModel modItem)
        {
            try
            {
                // 1. WATCHER CHECK
                bool needsWatcher = DateTime.Now - modItem.Shell.LastWatched > TimeSpan.FromHours(6);

                if (needsWatcher)
                {
                    modItem.Shell.WatcherStatus = WatcherStatusType.Checking;
                    var watchBundle = new List<(Mod, ModCrawlerConfig)> { (modItem.Shell, modItem.Config!) };
                    await _watcherService.RunStatusCheckAsync(watchBundle);

                    if (modItem.Shell.WatcherStatus != WatcherStatusType.UpdateFound)
                    {
                        modItem.Shell.WatcherStatus = WatcherStatusType.Idle;
                        modItem.RefreshSummary(); // Update UI
                        return;
                    }
                }

                // 2. STAGE 1: LINK EXTRACTION
                modItem.Shell.WatcherStatus = WatcherStatusType.Checking;
                var extractedLinks = await _watcherService.ExtractLinksAsync(modItem.Shell.RootSourceUrl, modItem.Config!);

                if (extractedLinks == null || !extractedLinks.Any())
                {
                    _dialogService.ShowInfo("No matching links found.", "Scan Complete");
                    modItem.Shell.WatcherStatus = WatcherStatusType.Idle;
                    modItem.RefreshSummary();
                    return;
                }

                // 3. SELECTION DIALOG
                var selectedLinks = await _dialogService.ShowLinkSelectorAsync(extractedLinks);
                if (selectedLinks == null || !selectedLinks.Any())
                {
                    modItem.Shell.WatcherStatus = WatcherStatusType.Idle;
                    modItem.RefreshSummary();
                    return;
                }

                // 4. STAGE 2: DEEP PARSE
                var availableMods = new List<AvailableMod>();
                foreach (var link in selectedLinks)
                {
                    var detail = await _watcherService.ParseModDetailsAsync(link.Url, modItem.Config!);
                    if (detail != null) availableMods.Add(detail);
                }

                // 5. VERSION SELECTION & PROMOTION
                if (availableMods.Any())
                {
                    var (primary, chosenMods) = await _dialogService.ShowVersionSelectorAsync(availableMods);

                    if (chosenMods != null && chosenMods.Any())
                    {
                        await _storageService.ProcessCrawlResultsAsync(
                            SelectedApp.InstalledVersion,
                            modItem.Shell.Id,
                            primary,
                            chosenMods);

                        modItem.Shell.WatcherStatus = WatcherStatusType.Idle;
                        modItem.Shell.LastWatched = DateTime.Now;
                        modItem.RefreshSummary();
                    }
                    else
                    {
                        // User backed out of the final selection
                        modItem.Shell.WatcherStatus = WatcherStatusType.Idle;
                        modItem.RefreshSummary();
                    }
                }
            }
            catch (Exception ex)
            {
                modItem.Shell.WatcherStatus = WatcherStatusType.Error;
                modItem.RefreshSummary();
                _dialogService.ShowError($"Crawl failed: {ex.Message}");
            }
        }
    }
}