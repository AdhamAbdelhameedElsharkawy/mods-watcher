using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class AppSelectionViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;
        private readonly INavigationService _navigationService;

        public ObservableCollection<ModdedAppItemViewModel> ModdedApps { get; } = new();

        // Global Actions
        public ICommand AddAppCommand { get; }

        // Card Actions
        public ICommand EditAppCommand { get; }
        public ICommand DeleteAppCommand { get; }
        public ICommand SelectAppCommand { get; }
        public ICommand CrawlAppModsCommand { get; }

        public AppSelectionViewModel(IStorageService storageService, INavigationService navigationService)
        {
            _storageService = storageService;
            _navigationService = navigationService;

            // 1. Navigation to Library
            SelectAppCommand = new RelayCommand(o =>
            {
                if (o is ModdedAppItemViewModel wrapper)
                    _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(wrapper.App);
            });

            // 2. Delete Logic
            DeleteAppCommand = new RelayCommand(async o =>
            {
                //TODO:Hard wipe for all mods on app/game deletion. Maybe add a confirmation dialog first? needs implementation.
                //if (o is ModdedAppItemViewModel wrapper)
                //{
                //    await _storageService.DeleteAppAsync(wrapper.App.InternalId);
                //    ModdedApps.Remove(wrapper);
                //}
            });

            // 3. Command Placeholders
            AddAppCommand = new RelayCommand(_ => AddNewApp());
            EditAppCommand = new RelayCommand(o => EditApp(o as ModdedAppItemViewModel));
            CrawlAppModsCommand = new RelayCommand(o => CrawlMods(o as ModdedAppItemViewModel));

            LoadApps();
        }

        private async void LoadApps()
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
                    TotalUsedSizeMB = dto.TotalSize,
                    IncompatibleCount = dto.IncompatibleCount
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

        //TODO:need implementation. Maybe add a confirmation dialog first? needs implementation.
        private void CrawlMods(ModdedAppItemViewModel? item) { throw new NotImplementedException(); }
    }
}