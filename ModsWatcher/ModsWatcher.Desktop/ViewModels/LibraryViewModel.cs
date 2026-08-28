using GongSolutions.Wpf.DragDrop;
using Microsoft.Extensions.Logging;
using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Desktop.Enums;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class LibraryViewModel : BaseViewModel, IInitializable<(ModdedApp, ModItemViewModel)>, IDropTarget
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IWatcherService _watcherService;
        private readonly IDialogService _dialogService;
        private readonly CommonUtils _commonUtils;
        private ModdedApp _selectedApp;
        private ModItemViewModel _selectedMod;

        public ObservableCollection<ModItemViewModel> Mods { get; set; }

        // The list actually bound to the UI — Mods filtered by SelectedStateFilter.
        public ObservableCollection<ModItemViewModel> FilteredMods { get; } = new();

        public IReadOnlyList<ModStateFilterOption> StateFilterOptions { get; } = new List<ModStateFilterOption>
        {
            new() { Value = ModStateFilter.Active, Label = "Active" },
            new() { Value = ModStateFilter.Inactive, Label = "Inactive" },
            new() { Value = ModStateFilter.DependencyParent, Label = "Dependency — Parent" },
            new() { Value = ModStateFilter.DependencyChild, Label = "Dependency — Child" },
            new() { Value = ModStateFilter.Package, Label = "Package" },
            new() { Value = ModStateFilter.VersionMismatch, Label = "Version Mismatch" },
            new() { Value = ModStateFilter.Watchable, Label = "Watchable" },
            new() { Value = ModStateFilter.Crawlable, Label = "Crawlable" },
            new() { Value = ModStateFilter.UpdateAvailable, Label = "Update Available" },
            new() { Value = ModStateFilter.All, Label = "All" },
        };

        private ModStateFilterOption _selectedStateFilter;
        public ModStateFilterOption SelectedStateFilter
        {
            get => _selectedStateFilter;
            set
            {
                if (SetProperty(ref _selectedStateFilter, value))
                    ApplyStateFilter();
            }
        }

        // Reordering (drag-drop, Move Up/Down) relies on index positions within the full
        // list, which are meaningless once a filter hides some mods — only safe on "All".
        public bool CanReorder => SelectedStateFilter?.Value == ModStateFilter.All;

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

        public ICommand NavToDependenciesCommand { get; }

        public ICommand NavToAlternativesCommand { get; }

        public ICommand NavToPackageCommand { get; }

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
            _selectedStateFilter = StateFilterOptions.First(o => o.Value == ModStateFilter.Active);

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
                _navigationService.NavigateTo<AvailableVersionsViewModel, (ModItemViewModel? Shell, ModdedApp App)>((null, SelectedApp)));
            NavToSingleModVersionsCommand = new RelayCommand(obj => ExecuteNavToVersions(obj));

            NavToAppsCommand = new RelayCommand(_ => _navigationService.NavigateTo<AppSelectionViewModel>());

            NavToDependenciesCommand = new RelayCommand(_ =>
            {
                if (SelectedMod != null)
                    _navigationService.NavigateTo<ModDependenciesViewModel, (ModdedApp, ModItemViewModel)>((SelectedApp, SelectedMod));
            });

            NavToAlternativesCommand = new RelayCommand(_ =>
            {
                if (SelectedMod != null)
                    _navigationService.NavigateTo<ModAlternativesViewModel, (ModdedApp, ModItemViewModel)>((SelectedApp, SelectedMod));
            });

            NavToPackageCommand = new RelayCommand(_ =>
            {
                if (SelectedMod != null)
                    _navigationService.NavigateTo<ModPackageViewModel, (ModdedApp, ModItemViewModel)>((SelectedApp, SelectedMod));
            });

            // Misc Actions
            //TODO: not binding to anything currently, but we can add a "View History" button in the UI if we want to surface this more prominently instead of hiding it in the versions dialog
            ShowHistoryCommand = new RelayCommand(_ => ViewModHistory());
            ToggleActivationCommand = new RelayCommand(_ => ToggleModActivation());
            HardWipeCommand = new RelayCommand(_ => HardWipeSelectedMod());
            NavToRetiredCommand = new RelayCommand(_ =>
                _navigationService.NavigateTo<RetiredModsViewModel, (ModdedApp, ModItemViewModel)>((SelectedApp, SelectedMod)));
            MoveUpCommand = new RelayCommand(async obj => await MoveModOrder(obj as ModItemViewModel, -1));
            MoveDownCommand = new RelayCommand(async obj => await MoveModOrder(obj as ModItemViewModel, 1));
            OpenUrlCommand = new RelayCommand(obj => ExecuteOpenUrl(obj as string));
            CopyUrlCommand = new RelayCommand(obj => ExecuteCopyUrl(obj as string));

        }

        public void Initialize((ModdedApp, ModItemViewModel) data)
        {
            SelectedApp = data.Item1;
            
            LoadLibrary();

            if (data.Item2 != null)
            {
                // 2. Find the object in the NEW list that matches the old one by ID/Path
                // Assuming your ModItemViewModel has a unique property like 'Id' or 'ModPath'
                SelectedMod = Mods.FirstOrDefault(m => m.Shell.Id == data.Item2.Shell.Id);
            }
        }

        private async Task LoadLibrary()
        {
            if (SelectedApp == null) return;
            Mods.Clear();
            

            // 1. Get the list of tuples: (Mod shell, InstalledMod installed, ModCrawlerConfig config)
            var libraryData = await _storageService.GetFullModsByAppId(SelectedApp.Id);

            // 2. Sort the tuples by the shell's PriorityOrder
            var sortedData = libraryData.OrderBy(x => x.Shell.PriorityOrder);

            // Batched lookups so the "ALT"/"PACKAGE"/"PARENT"/"CHILD" badges don't require an N+1 query per mod
            var modsWithAlternatives = await _storageService.GetModIdsWithAlternativesByAppIdAsync(SelectedApp.Id);
            var packageMainModIds = await _storageService.GetPackageMainModIdsByAppIdAsync(SelectedApp.Id);
            var dependencyRoles = await _storageService.GetDependencyRolesByAppIdAsync(SelectedApp.Id);
            var modsWithAvailableVersions = await _storageService.GetModIdsWithAvailableVersionsByAppIdAsync(SelectedApp.Id);

            // 3. Iterate over the SORTED data
            foreach (var (shell, installed, config) in sortedData)
            {
                // Use SelectedApp.Version (or your specific property name) for the constructor
                Mods.Add(new ModItemViewModel(shell, installed, config, SelectedApp.InstalledVersion, _commonUtils, _logger)
                {
                    HasAlternatives = modsWithAlternatives.Contains(shell.Id),
                    IsPackage = packageMainModIds.Contains(shell.Id),
                    IsDependencyParent = dependencyRoles.Parents.Contains(shell.Id),
                    IsDependencyChild = dependencyRoles.Children.Contains(shell.Id),
                    HasAvailableVersions = modsWithAvailableVersions.Contains(shell.Id)
                });
            }

            ApplyStateFilter();
        }

        // Rebuilds FilteredMods (what the UI actually shows) from Mods, based on
        // SelectedStateFilter. Text search then narrows within this filtered set.
        private void ApplyStateFilter()
        {
            FilteredMods.Clear();

            IEnumerable<ModItemViewModel> source = SelectedStateFilter?.Value switch
            {
                ModStateFilter.Active => Mods.Where(m => m.IsUsed),
                ModStateFilter.Inactive => Mods.Where(m => !m.IsUsed),
                ModStateFilter.DependencyParent => Mods.Where(m => m.IsDependencyParent),
                ModStateFilter.DependencyChild => Mods.Where(m => m.IsDependencyChild),
                ModStateFilter.Package => Mods.Where(m => m.IsPackage),
                ModStateFilter.VersionMismatch => Mods.Where(m => !m.IsCompatible),
                ModStateFilter.Watchable => Mods.Where(m => m.Shell.IsWatchable),
                ModStateFilter.Crawlable => Mods.Where(m => m.Shell.IsCrawlable),
                ModStateFilter.UpdateAvailable => Mods.Where(m => m.HasUpdate),
                _ => Mods
            };

            foreach (var mod in source)
                FilteredMods.Add(mod);

            OnPropertyChanged(nameof(CanReorder));
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
            _navigationService.NavigateTo<AvailableVersionsViewModel, (ModItemViewModel? Shell, ModdedApp App)>((target, SelectedApp));
        }

        private void ViewModHistory()
        {
            if (SelectedMod == null) return;
            _navigationService.NavigateTo<ModHistoryViewModel, (ModItemViewModel, ModdedApp)>((SelectedMod, SelectedApp));
        }

        private async void ToggleModActivation()
        {
            if (SelectedMod?.Shell == null) return;

            bool currentlyActive = SelectedMod.IsUsed;
            string action = currentlyActive ? "Deactivate" : "Activate";

            // Only check dependency impact when deactivating a mod that others depend on
            if (currentlyActive)
            {
                var impactTree = await _storageService.GetDependencyImpactTreeAsync(SelectedMod.Shell.Id);
                if (impactTree != null)
                {
                    var result = ShowDependencyImpactDialog(SelectedMod.Shell.Name, impactTree, isDeactivation: true);
                    switch (result)
                    {
                        case DependencyImpactAction.Cancel:
                            return;
                        case DependencyImpactAction.RemoveDependent:
                            await RetireAllDependentsAsync(impactTree);
                            break;
                        case DependencyImpactAction.DeactivateDependent:
                            await DeactivateAllDependentsAsync(impactTree);
                            break;
                            // BreakDependency: fall through to deactivate, relation stays intact
                    }
                }
            }
            else
            {
                // Activating: if this mod belongs to an alternative group, only one member
                // of the group may be active at a time — check for a conflict first.
                var group = (await _storageService.GetAlternativeGroupAsync(SelectedMod.Shell.Id)).ToList();
                var activeAlternative = group.FirstOrDefault(g => g.IsActive);

                if (activeAlternative != null)
                {
                    if (!await SwapActiveAlternativeAsync(SelectedMod, activeAlternative))
                        return;

                    SelectedMod.IsUsed = true;
                    await _storageService.UpdateModShellAsync(SelectedMod.Shell);
                    SelectedMod.RefreshSummary();
                    OnPropertyChanged(nameof(CanToggleActivation));
                    await LoadLibrary();
                    return;
                }
            }

            if (_dialogService.ShowConfirmation($"{action} {SelectedMod.Shell.Name}?", $"{action} Mod"))
            {
                SelectedMod.IsUsed = !currentlyActive;
                await _storageService.UpdateModShellAsync(SelectedMod.Shell);
                SelectedMod.RefreshSummary();
                OnPropertyChanged(nameof(CanToggleActivation));
                await LoadLibrary();
            }
        }

        private async void HardWipeSelectedMod()
        {
            if (SelectedMod == null) return;

            // Check dependency impact before proceeding
            var impactTree = await _storageService.GetDependencyImpactTreeAsync(SelectedMod.Shell.Id);
            if (impactTree != null)
            {
                var result = ShowDependencyImpactDialog(SelectedMod.Shell.Name, impactTree, isDeactivation: false);
                switch (result)
                {
                    case DependencyImpactAction.Cancel:
                        return;
                    case DependencyImpactAction.RemoveDependent:
                        await RetireAllDependentsAsync(impactTree);
                        break;
                    case DependencyImpactAction.BreakDependency:
                        await BreakAllDependenciesAsync(impactTree);
                        break;
                        // DeactivateDependent not applicable for hard wipe
                }
            }

            string? reason = _dialogService.ShowPrompt(
                $"Why are you retiring '{SelectedMod.Shell.Name}'?",
                "Retirement Reason");

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

        private DependencyImpactAction ShowDependencyImpactDialog(string modName, DependencyTreeNodeDto tree, bool isDeactivation)
        {
            var vm = new DependencyImpactDialogViewModel(modName, tree, isDeactivation, _logger);
            var dialog = new Views.DependencyImpactDialog
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
            return vm.SelectedAction;
        }

        private bool ShowAlternativeConflictDialog(string newModName, string activeModName)
        {
            var vm = new AlternativeConflictDialogViewModel(newModName, activeModName, _logger);
            var dialog = new Views.AlternativeConflictDialog
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
            return vm.Confirmed;
        }

        // Notifies the user another mod in the alternative group is active, confirms the
        // swap, then deactivates the outgoing mod (honoring its own dependency impact)
        // before the caller activates the new one. Returns false if the user cancelled.
        private async Task<bool> SwapActiveAlternativeAsync(ModItemViewModel newMod, ModAlternativeDisplayDto activeAlternative)
        {
            if (!ShowAlternativeConflictDialog(newMod.Shell.Name, activeAlternative.ModName))
                return false;

            var oldMod = Mods.FirstOrDefault(m => m.Shell.Id == Guid.Parse(activeAlternative.ModId));
            if (oldMod == null)
                return true;

            var impactTree = await _storageService.GetDependencyImpactTreeAsync(oldMod.Shell.Id);
            if (impactTree != null)
            {
                var result = ShowDependencyImpactDialog(oldMod.Shell.Name, impactTree, isDeactivation: true);
                switch (result)
                {
                    case DependencyImpactAction.Cancel:
                        return false;
                    case DependencyImpactAction.RemoveDependent:
                        await RetireAllDependentsAsync(impactTree);
                        break;
                    case DependencyImpactAction.DeactivateDependent:
                        await DeactivateAllDependentsAsync(impactTree);
                        break;
                        // BreakDependency: fall through to deactivate, relation stays intact
                }
            }

            oldMod.IsUsed = false;
            await _storageService.UpdateModShellAsync(oldMod.Shell);
            return true;
        }

        private async Task RetireAllDependentsAsync(DependencyTreeNodeDto tree)
        {
            foreach (var child in tree.Children)
            {
                var childGuid = Guid.Parse(child.ModId);
                var mod = Mods.FirstOrDefault(m => m.Shell.Id == childGuid);
                if (mod != null)
                    await _storageService.HardWipeModAsync(mod.Shell, SelectedApp, mod.Config, "Retired due to parent mod removal");

                await RetireAllDependentsAsync(child);
            }
        }

        private async Task DeactivateAllDependentsAsync(DependencyTreeNodeDto tree)
        {
            foreach (var child in tree.Children)
            {
                var childGuid = Guid.Parse(child.ModId);
                var mod = Mods.FirstOrDefault(m => m.Shell.Id == childGuid);
                if (mod != null)
                {
                    mod.IsUsed = false;
                    await _storageService.UpdateModShellAsync(mod.Shell);
                }
                await DeactivateAllDependentsAsync(child);
            }
        }

        private async Task BreakAllDependenciesAsync(DependencyTreeNodeDto tree)
        {
            foreach (var child in tree.Children)
            {
                await _storageService.RemoveDependencyAsync(Guid.Parse(child.ModId), Guid.Parse(tree.ModId));
                await BreakAllDependenciesAsync(child);
            }
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
                        Loading.BusyMessage = $"Checking Completed for {nonCheckedMods.Count} Mods...";
                        _dialogService.ShowInfo($"Checking Completed for {nonCheckedMods.Count} Mods...");
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

            if (!CanReorder)
            {
                _dialogService.ShowInfo("Switch the filter to 'All' before reordering mods.", "Reordering Unavailable");
                return;
            }

            // Only reachable on the "All" filter, where FilteredMods mirrors Mods exactly —
            // so index positions here are safe and unambiguous.
            int oldIndex = FilteredMods.IndexOf(mod);
            int newIndex = oldIndex + direction;

            if (oldIndex < 0 || newIndex < 0 || newIndex >= FilteredMods.Count) return;

            var targetMod = FilteredMods[newIndex];

            // 1. Swap the PriorityOrder values
            int tempOrder = mod.PriorityOrder;
            mod.PriorityOrder = targetMod.PriorityOrder;
            targetMod.PriorityOrder = tempOrder;

            // 2. Persist changes
            await _storageService.UpdateModShellAsync(mod.Shell);
            await _storageService.UpdateModShellAsync(targetMod.Shell);

            // 3. Reload so Mods/FilteredMods reflect the new PriorityOrder-sorted order
            await LoadLibrary();
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

                    // RunStatusCheckAsync only updates the hash (and sets UpdateFound) on a
                    // real difference. Comparing the hash before/after this call is the
                    // reliable way to tell "this check found something new" apart from
                    // "nothing changed" — WatcherStatus itself can't be used for that, since
                    // it gets force-set to Checking right below regardless of the outcome.
                    string? hashBeforeCheck = modItem.Shell.LastWatcherHash;

                    modItem.Shell.WatcherStatus = WatcherStatusType.Checking;
                    var watchBundle = new List<(Mod, ModCrawlerConfig)> { (modItem.Shell, modItem.Config!) };
                    await _watcherService.RunStatusCheckAsync(watchBundle);

                    bool checkFoundNoRealChange = modItem.Shell.LastWatcherHash == hashBeforeCheck;

                    if (checkFoundNoRealChange)
                    {
                        if (!modItem.Shell.IsCrawlable)
                        {
                            // Non-crawlable mods have no deep scan to offer — once the watcher
                            // check itself finds nothing new, there's nothing further for Run
                            // Full Sync to do, so finalize here instead of asking "scan
                            // anyway?" for a scan that can't happen, and instead of falling
                            // through into the (skipped, for non-crawlable) block below that
                            // would otherwise leave this mod's status stuck unresolved.
                            await FinalizeSyncState(modItem, WatcherStatusType.Idle);
                            Loading.IsBusy = false;
                            Loading.BusyMessage = string.Empty;
                            return;
                        }

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
                    else if (!modItem.Shell.IsCrawlable)
                    {
                        // A genuine new change was found on a non-crawlable mod — nothing
                        // more to do, but refresh so the UPDATE badge appears immediately
                        // rather than waiting for the next full library reload.
                        modItem.RefreshSummary();
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

            if (!CanReorder)
            {
                _dialogService.ShowError("Switch the filter to 'All' before reordering mods.");
                return;
            }

            try
            {
                Loading.IsBusy = true;
                Loading.BusyMessage = "Saving order...";

                // Only reachable on the "All" filter, where FilteredMods mirrors Mods exactly —
                // dropInfo.InsertIndex is a position in that same list, so indices line up.
                int oldIndex = FilteredMods.IndexOf(sourceItem);
                int targetIndex = dropInfo.InsertIndex;
                if (targetIndex > oldIndex) targetIndex--;
                if (oldIndex == targetIndex) return;

                var reordered = FilteredMods.ToList();

                // Redistribute the exact set of PriorityOrder values already in use across
                // the new order, rather than renumbering from 0 — keeps this safe even if
                // the underlying values aren't a clean contiguous 0..n-1 range.
                var existingOrders = reordered.Select(m => m.PriorityOrder).OrderBy(x => x).ToList();

                reordered.RemoveAt(oldIndex);
                reordered.Insert(targetIndex, sourceItem);

                for (int i = 0; i < reordered.Count; i++)
                {
                    reordered[i].PriorityOrder = existingOrders[i];
                    await _storageService.UpdateModShellAsync(reordered[i].Shell);
                }

                _logger.LogInformation("Library reordered via Drag-and-Drop.");

                await LoadLibrary();
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