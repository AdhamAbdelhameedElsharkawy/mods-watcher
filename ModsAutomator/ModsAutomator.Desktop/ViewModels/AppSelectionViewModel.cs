using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.Services;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Windows;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class AppSelectionViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;
        private readonly INavigationService _navigationService;
        private readonly IWatcherService _watcherService;
        private readonly IDialogService _dialogService;
        private readonly CommonUtils _commonUtils;

        public ObservableCollection<ModdedAppItemViewModel> ModdedApps { get; } = new();

        // Global Actions
        public ICommand AddAppCommand { get; }

        // Card Actions
        public ICommand EditAppCommand { get; }
        public ICommand DeleteAppCommand { get; }
        public ICommand SelectAppCommand { get; }
        public ICommand SyncAppModsCommand { get; }

        public AppSelectionViewModel(IStorageService storageService, INavigationService navigationService, IWatcherService watcherService,
            IDialogService dialogService, CommonUtils commonUtils)
        {
            _storageService = storageService;
            _navigationService = navigationService;
            _watcherService = watcherService;
            _dialogService = dialogService;
            _commonUtils = commonUtils;

            // 1. Navigation to Library
            SelectAppCommand = new RelayCommand(o =>
            {
                if (o is ModdedAppItemViewModel wrapper)
                    _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(wrapper.App);
            });

            // 2. Delete Logic
            DeleteAppCommand = new RelayCommand(async o =>
            {
                if (o is ModdedAppItemViewModel wrapper)
                {



                    if (_dialogService.ShowConfirmation($"Are you sure you want to HARD WIPE '{wrapper.App.Name}'?\n\n" +
                        "This will permanently delete:\n" +
                        "• The App record\n" +
                        "• All Mod Shells\n" +
                        "• All Installation History\n" +
                        "• All Unused/Retired Mod snapshots\n\n" +
                        "This action cannot be undone.",
                        "Point of No Return"))
                    {
                        try
                        {
                            // Trigger the bulk wipe logic 
                            await _storageService.HardWipeAppAsync(wrapper.App.Id);
                            // Remove from the UI collection
                            ModdedApps.Remove(wrapper);
                        }
                        catch (Exception ex)
                        {
                            _dialogService.ShowError($"Failed to wipe app: {ex.Message}", "Error");
                        }
                    }


                }
            });

            // 3. Command Placeholders
            AddAppCommand = new RelayCommand(_ => AddNewApp());
            EditAppCommand = new RelayCommand(o => EditApp(o as ModdedAppItemViewModel));
            SyncAppModsCommand = new RelayCommand(o => SyncAppWatcherAsync(o as ModdedAppItemViewModel));

            LoadApps();
        }

        private async Task LoadApps()
        {
            // Fetch the DTOs (Data + Stats combined)
            var summaries = await _storageService.GetAllAppSummariesAsync();

            ModdedApps.Clear();

            foreach (var dto in summaries)
            {
                // Map the DTO to the small VM
                var wrapper = new ModdedAppItemViewModel(dto.App)
                {
                    ActiveModsCount = dto.ActiveCount,
                    PotentialUpdatesCount = dto.PotentialUpdatesCount
                };

                ModdedApps.Add(wrapper);
            }
        }

        private void AddNewApp() {
            // Create the ViewModel for the dialog (Add Mode)
            var dialogVM = new AppDialogViewModel(_storageService);

            // Create the View (the Window)
            var dialog = new Views.AddAppDialog
            {
                DataContext = dialogVM,
                Owner = System.Windows.Application.Current.MainWindow // Keeps it centered
            };

            if (dialog.ShowDialog() == true)
            {
                // Refresh the list if the user saved successfully
                LoadApps();
            }
        }
        private void EditApp(ModdedAppItemViewModel? item) {
            if (item == null) return;

            // Create ViewModel in Edit Mode
            var dialogVM = new AppDialogViewModel(_storageService, item.App);

            var dialog = new Views.AddAppDialog
            {
                DataContext = dialogVM,
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                LoadApps();
            }
        }

        private async Task SyncAppWatcherAsync(ModdedAppItemViewModel? item)
        {
            if (item == null) return;

            try
            {
                // 1. UI Feedback: Start loading state on the app card
                item.IsSyncing = true;
                this.IsBusy = true;

                BusyMessage = $"Checking All Watchable Mods for '{item.Name}'...";

                // 2. Data Fetch: Use the bundle logic we just finalized
                var bundle = await _storageService.GetWatchableBundleByAppIdAsync(item.App.Id);
                List<(Mod mod, ModCrawlerConfig config)> modsToCheck = new List<(Mod mod, ModCrawlerConfig config)>();

                if (bundle.Any())
                {
                    // 3. Execution: Run Stage 1 (Hash comparison & Status Update)
                   
                    foreach (var (mod, config) in bundle)
                    {
                        bool canCheck = _commonUtils.CanCheckModWatcherStatus(mod);

                        if (canCheck)
                        {
                            modsToCheck.Add((mod, config));
                        }
                        else
                        {
                            bool forceCheck = _dialogService.ShowConfirmation(
                                $"Mod: {mod.Name} was checked recently ({mod.LastWatched:t}). Check anyway?",
                                "Recent Check Detected");

                            if (forceCheck)
                            {
                                modsToCheck.Add((mod, config));
                            }
                        }
                    }
                    await _watcherService.RunStatusCheckAsync(modsToCheck);
                }

                // 4. Refresh: Update the UI to show new PotentialUpdatesCount/ActiveCount
                BusyMessage = $"Checking Completed for {modsToCheck.Count} Mods...";
                await LoadApps();
            }
            catch (Exception ex)
            {
                // Add logging or user notification here
            }
            finally
            {
                // 5. UI Feedback: Stop loading state
                item.IsSyncing = false;
                this.IsBusy = false;
                BusyMessage = string.Empty;
            }
        }
    }
}