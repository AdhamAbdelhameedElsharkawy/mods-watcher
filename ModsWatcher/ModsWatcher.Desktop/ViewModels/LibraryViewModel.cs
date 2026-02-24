using GongSolutions.Wpf.DragDrop;
using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class LibraryViewModel : BaseViewModel, IInitializable<ModdedApp>, IDropTarget
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IWatcherService _watcherService;
        private readonly IDialogService _dialogService;
        private readonly CommonUtils _commonUtils;
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

        public ICommand NavToAppsCommand { get; }

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        public ICommand OpenUrlCommand { get; }
        public ICommand CopyUrlCommand { get; }

        // NEW: Installation Management Commands
        public ICommand SetupManualInstallCommand { get; }
        public ICommand EditInstallationCommand { get; }

        public LibraryViewModel(INavigationService navigationService, IStorageService storageService, IWatcherService watcherService, 
            IDialogService dialogService, CommonUtils commonUtils, ILogger logger) : base(logger)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            _watcherService = watcherService;
            _dialogService = dialogService;
            _commonUtils = commonUtils;
            Mods = new ObservableCollection<ModItemViewModel>();

            // Initialization & Setup
            AddModShellCommand = new RelayCommand(async _ => await RegisterNewMod());
            EditModShellCommand = new RelayCommand(async _ => await EditSelectedModShellAsync());
            SyncAllModsCommand = new RelayCommand(async _ => await SyncAllWatchableModsAsync());
            //Currently not used, but keeping for potential future use where we might want a quick "status check" without the full crawl flow
            //SyncSingleModCommand = new RelayCommand(async mod => await SyncSingleModAsync(mod as ModItemViewModel));

            FullSyncSingleModCommand = new RelayCommand(
    async obj =>
    {
        var target = obj as ModItemViewModel ?? SelectedMod;
        if (target == null) return;

        if (!target.Shell.IsWatchable)
        {
            _dialogService.ShowInfo($"'{target.Shell.Name}' is not watchable, so it cannot be synced. Please check the configuration (Is Watchable) or refer to the documentation.", "Not Watchable");
            return;

        }

        // NEW: Check for config and inform user instead of just being disabled
        if (target.Config == null)
        {
            _dialogService.ShowInfo(
                $"Cannot sync '{target.Shell.Name}' because the Crawler Configuration is missing. " +
                "Please click 'Edit Shell Metadata' to set it up.",
                "Configuration Required");
            return;
        }

        await RunFullSync(target);
    },
    obj => (obj as ModItemViewModel ?? SelectedMod)?.IsUsed ?? false // Only require IsUsed to be enabled
);

            // NEW: Installation Logic
            SetupManualInstallCommand = new RelayCommand(async _ => await SetupManualInstallationAsync());
            EditInstallationCommand = new RelayCommand(async _ => await EditInstallationDataAsync());

            // NAVIGATION FLOW
            NavToVersionsManagerCommand = new RelayCommand(_ =>
                _navigationService.NavigateTo<AvailableVersionsViewModel, (Mod? Shell, ModdedApp App)>((null, SelectedApp)));
            NavToSingleModVersionsCommand = new RelayCommand(obj => ExecuteNavToVersions(obj));

            NavToAppsCommand = new RelayCommand(_ => _navigationService.NavigateTo<AppSelectionViewModel>());

            // Misc Actions
            //TODO: not binding to anything currently, but we can add a "View History" button in the UI if we want to surface this more prominently instead of hiding it in the versions dialog
            ShowHistoryCommand = new RelayCommand(_ => ViewModHistory());
            ToggleActivationCommand = new RelayCommand(_ => ToggleModActivation());
            HardWipeCommand = new RelayCommand(_ => HardWipeSelectedMod());
            NavToRetiredCommand = new RelayCommand(_ =>
                _navigationService.NavigateTo<RetiredModsViewModel, ModdedApp>(SelectedApp));
            MoveUpCommand = new RelayCommand(async obj => await MoveModOrder(obj as ModItemViewModel, -1));
            MoveDownCommand = new RelayCommand(async obj => await MoveModOrder(obj as ModItemViewModel, 1));
            OpenUrlCommand = new RelayCommand(obj => ExecuteOpenUrl(obj as string));
            CopyUrlCommand = new RelayCommand(obj => ExecuteCopyUrl(obj as string));

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
                Mods.Add(new ModItemViewModel(shell, installed, config, SelectedApp.InstalledVersion, _commonUtils, _logger));
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

                await FinalizeSyncState(SelectedMod, WatcherStatusType.Idle);

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

                await FinalizeSyncState(SelectedMod, WatcherStatusType.Idle);

                // Refresh UI components
                SelectedMod.RefreshSummary();
                OnPropertyChanged(nameof(SelectedMod));
            }
        }

        private async Task<bool> ShowInstallationDialog(InstalledMod installed)
        {
            var vm = new ModInstallationDialogViewModel(installed, _logger);
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
            if (SelectedMod?.Shell == null) return;

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

            // Ask for the reason
            string? reason = _dialogService.ShowPrompt(
                $"Why are you retiring '{SelectedMod.Shell.Name}'?",
                "Retirement Reason");

            // If they close the dialog or hit cancel, reason is null. 
            // We can either abort or proceed with a default.
            if (reason == null) return;

            await _storageService.HardWipeModAsync(
                SelectedMod.Shell,
                SelectedApp,
                SelectedMod.Config,
                string.IsNullOrWhiteSpace(reason) ? "No reason provided" : reason
            );

            SelectedMod = null;
            await LoadLibrary();
        }

        private async Task RegisterNewMod()
        {
            var vm = new ModShellDialogViewModel(_storageService, SelectedApp.Id, _dialogService, _logger);
            var dialog = new Views.ModShellDialog { DataContext = vm, Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true) await LoadLibrary();
        }

        private async Task EditSelectedModShellAsync()
        {
            if (SelectedMod == null) return;
            var config = await _storageService.GetModCrawlerConfigByModIdAsync(SelectedMod.Shell.Id);
            var vm = new ModShellDialogViewModel(_storageService, SelectedApp.Id, _dialogService,_logger, SelectedMod.Shell, config);
            var dialog = new Views.ModShellDialog { DataContext = vm, Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true) await LoadLibrary();
        }

        private async Task SyncAllWatchableModsAsync()
        {
            try
            {
                Loading.IsBusy = true;
                Loading.BusyMessage = "Retrieving watchable mods...";

                _logger.LogInformation("Starting bulk sync for watchable mods of app {AppName} (ID: {AppId})", SelectedApp.Name, SelectedApp.Id);

                var targetMods = Mods.Where(m => m.IsUsed && m.Shell.IsWatchable && m.Config != null).ToList();
                if (targetMods.Any())
                {
                    Loading.BusyMessage = $"Checking for updates for {targetMods.Count} Mods...";

                    var watchList = targetMods.Select(m => (m.Shell, m.Config)).ToList();
                    var nonCheckedMods = new List<(Mod, ModCrawlerConfig)>();

                    if (watchList.Any())
                    {
                        foreach (var (mod, config) in watchList)
                        {
                            bool canCheck = _commonUtils.CanCheckModWatcherStatus(mod);

                            if (canCheck)
                            {
                                nonCheckedMods.Add((mod, config));
                            }
                            else
                            {
                                bool forceCheck = _dialogService.ShowConfirmation(
                                    $"Mod: {mod.Name} was checked recently ({mod.LastWatched:t}). Check anyway?",
                                    "Recent Check Detected");

                                if (forceCheck)
                                {
                                    nonCheckedMods.Add((mod, config));
                                }
                            }
                        }

                        await _watcherService.RunStatusCheckAsync(nonCheckedMods);
                        foreach (var mod in targetMods) mod.RefreshSummary();
                        Loading.BusyMessage = $"Checking for updates for {targetMods.Count} Mods Completed...";
                    }
                }
            }
            catch (Exception)
            {
                _dialogService.ShowError("An error occurred during synchronization. Please try again.");
                _logger.LogError("Error during bulk sync of watchable mods for app {AppName} (ID: {AppId})", SelectedApp.Name, SelectedApp.Id);
                throw;
            }
            finally
            {
                Loading.IsBusy = false;
                Loading.BusyMessage = "Not Busy...";
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

            if (newIndex < 0 || newIndex >= Mods.Count) return;

            var targetMod = Mods[newIndex];

            // 1. Swap the PriorityOrder values
            int tempOrder = mod.PriorityOrder;
            mod.PriorityOrder = targetMod.PriorityOrder;
            targetMod.PriorityOrder = tempOrder;

            // 2. Persist changes
            await _storageService.UpdateModShellAsync(mod.Shell);
            await _storageService.UpdateModShellAsync(targetMod.Shell);

            // 3. Update the UI collection position
            Mods.Move(oldIndex, newIndex);

            // 4. Refresh the properties so the UI stops showing "0"
            // We notify the UI that the Shell property on these specific objects is updated
            OnPropertyChanged(nameof(Mods));
            //mod.RefreshSummary();
            //targetMod.RefreshSummary();

        }

        public async Task RunFullSync(ModItemViewModel modItem)
        {
            try
            {
                // 1. WATCHER CHECK

                Loading.IsBusy = true;
                bool forceSync = false;
                Loading.BusyMessage = "Analyzing watcher status...";
                _logger.LogInformation("Initiating full sync for mod {ModName} (ID: {ModId}) of app {AppName} (ID: {AppId})", 
                    modItem.Shell.Name, modItem.Shell.Id, SelectedApp.Name, SelectedApp.Id);
                bool canCheck = _commonUtils.CanCheckModWatcherStatus(modItem.Shell);

                if (canCheck)
                {

                    Loading.BusyMessage = "Checking for updates...";

                    modItem.Shell.WatcherStatus = WatcherStatusType.Checking;
                    var watchBundle = new List<(Mod, ModCrawlerConfig)> { (modItem.Shell, modItem.Config!) };
                    await _watcherService.RunStatusCheckAsync(watchBundle);

                    if (modItem.Shell.WatcherStatus != WatcherStatusType.UpdateFound)
                    {
                       
                        forceSync = _dialogService.ShowConfirmation(
                                            "No new update detected by the watcher. Perform a deep scan anyway?",
                                            "No Update Found");

                        if (!forceSync)
                        {
                            await FinalizeSyncState(modItem, WatcherStatusType.Idle);
                            Loading.IsBusy = false; // Unlock to show dialog
                            Loading.BusyMessage = string.Empty;
                            return;
                        }
                    }
                }
                else
                {
                    // If recently checked, ask before jumping into the deep crawl
                    Loading.IsBusy = true;
                    forceSync = _dialogService.ShowConfirmation(
                        $"This mod was checked recently ({modItem.Shell.LastWatched:t}). Run full scan anyway?",
                        "Recent Check Detected");

                    if (!forceSync) {

                        await FinalizeSyncState(modItem, WatcherStatusType.Idle);
                        Loading.IsBusy = false;
                        Loading.BusyMessage = string.Empty;
                        return;
                    }
                    
                }

                Loading.BusyMessage = "Analyzing watcher status Completed...";
                Loading.IsBusy = false;
                _logger.LogInformation("Watcher status analysis completed for mod {ModName} (ID: {ModId}). Force sync: {ForceSync}", 
                    modItem.Shell.Name, modItem.Shell.Id, forceSync);


                if (modItem.Shell.IsCrawlable)
                {
                    Loading.IsBusy = true;
                    Loading.BusyMessage = "Extracting Links...";
                    _logger.LogInformation("Starting link extraction for mod {ModName} (ID: {ModId})", modItem.Shell.Name, modItem.Shell.Id);
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
                        await FinalizeSyncState(modItem, WatcherStatusType.Idle);
                        return;
                    }
                    _logger.LogInformation("{SelectedCount} links selected for deep parsing for mod {ModName} (ID: {ModId})", 
                        selectedLinks.Count(), modItem.Shell.Name, modItem.Shell.Id);
                    // 4. STAGE 2: DEEP PARSE
                    Loading.BusyMessage = $"Deep-parsing {selectedLinks.Count()} items...";
                    var availableMods = new List<AvailableMod>();
                    foreach (var link in selectedLinks)
                    {
                        var detail = await _watcherService.ParseModDetailsAsync(link.Url, modItem.Config!);
                        if (detail != null)
                        {
                            detail.Id = modItem.Shell.Id; // Link to the Shell for easier processing later

                            availableMods.Add(detail);
                        }
                    }

                    // 5. VERSION SELECTION & PROMOTION
                    if (availableMods.Any())
                    {
                        _logger.LogInformation("{AvailableCount} available versions found for mod {ModName} (ID: {ModId})", 
                            availableMods.Count(), modItem.Shell.Name, modItem.Shell.Id);
                        var (primary, chosenMods) = await _dialogService.ShowVersionSelectorAsync(availableMods);

                        if (chosenMods != null && chosenMods.Any())
                        {
                            InstalledMod? result = await _storageService.ProcessCrawlResultsAsync(
                                SelectedApp.InstalledVersion,
                                modItem.Shell.Id,
                                primary,
                                chosenMods);

                            if (result != null)
                            {
                                modItem.Installed = result;

                                _dialogService.ShowInfo($"Mod '{modItem.Name}' has been updated to version {result.InstalledVersion}.", "Update Successful");
                            }



                            await FinalizeSyncState(modItem, WatcherStatusType.Idle);
                        }
                        else
                        {
                            // User backed out of the final selection
                            await FinalizeSyncState(modItem, WatcherStatusType.Idle);
                        }
                    } 
                }
            }
            catch (Exception ex)
            {
                await FinalizeSyncState(modItem, WatcherStatusType.Error);
                _dialogService.ShowError($"Crawl failed: {ex.Message}");
                _logger.LogError(ex, "Error during full sync for mod {ModName} (ID: {ModId})", modItem.Shell.Name, modItem.Shell.Id);
            }
            finally
            {
                Loading.IsBusy = false;
                Loading.BusyMessage = string.Empty;
            }
        }

        private async Task FinalizeSyncState(ModItemViewModel modItem, WatcherStatusType status)
        {
            modItem.Shell.WatcherStatus = status;
            // We only update LastWatched if it wasn't an error
            if (status == WatcherStatusType.Idle)
            {
                modItem.Shell.LastWatched = DateTime.Now;
            }

            await _storageService.UpdateModShellAsync(modItem.Shell);
            modItem.RefreshSummary();
        }
        private void ExecuteOpenUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Could not open browser: {ex.Message}");
            }
        }

        private void ExecuteCopyUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            Clipboard.SetText(url);
            // Optional: You could add a temporary 'Copied!' status message here if you have a status bar
        }

        public void DragOver(IDropInfo dropInfo)
        {
            // Allow dragging if both source and target are ModItemViewModels
            if (dropInfo.Data is ModItemViewModel && dropInfo.TargetItem is ModItemViewModel)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        public async void Drop(IDropInfo dropInfo)
        {
            if (Loading.IsBusy)
            {
                _dialogService.ShowError("Please wait for the current operation to finish.");
                return;
            }

            if (dropInfo.Data is not ModItemViewModel sourceItem) return;

            try
            {
                Loading.IsBusy = true;
                Loading.BusyMessage = "Saving order...";

                int oldIndex = Mods.IndexOf(sourceItem);
                int targetIndex = dropInfo.InsertIndex;
                if (targetIndex > oldIndex) targetIndex--;
                if (oldIndex == targetIndex) return;

                // 1. Visual Move
                Mods.Move(oldIndex, targetIndex);
                for (int i = 0; i < Mods.Count; i++)
                {
                    Mods[i].PriorityOrder = i;
                    //Mods[i].RefreshSummary();
                }
                OnPropertyChanged(nameof(Mods));
                
                // 2. Just extract the Shells in their new order and send them off
                var shells = Mods.Select(vm => vm.Shell).ToList();
                await _storageService.UpdateModsOrderAsync(shells);

                _logger.LogInformation("Library reordered via Drag-and-Drop.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist reorder.");
                // Optional: Re-fetch or revert UI move here
            }
            finally
            {
                Loading.IsBusy = false;
                Loading.BusyMessage = string.Empty;
            }
        }


    }
}