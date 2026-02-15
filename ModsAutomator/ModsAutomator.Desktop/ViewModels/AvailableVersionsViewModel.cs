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
    public class AvailableVersionsViewModel : BaseViewModel, IInitializable<(Mod? Shell, ModdedApp App)>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;

        private Mod? _shell;
        private ModdedApp _parentApp;

        public string ViewTitle => _shell != null ? $"MOD: {_shell.Name}" : $"{_parentApp.Name} - ALL MODS";

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public bool HasNoMods => !GroupedMods.Any();

        private bool _isReviewOverlayVisible;
        public bool IsReviewOverlayVisible
        {
            get => _isReviewOverlayVisible;
            set => SetProperty(ref _isReviewOverlayVisible, value);
        }

        public ObservableCollection<ModGroupViewModel> GroupedMods { get; } = new();

        public ICommand CrawlAllCommand { get; }
        public ICommand CrawlSingleModCommand { get; }
        public ICommand PromoteToInstalledCommand { get; }
        public ICommand OpenDownloadLinkCommand { get; }
        public ICommand BackCommand { get; }

        public AvailableVersionsViewModel(
            INavigationService navigationService,
            IStorageService storageService)
        {
            _navigationService = navigationService;
            _storageService = storageService;

            PromoteToInstalledCommand = new RelayCommand(async mod => await PromoteToInstalled((AvailableMod)mod));
            OpenDownloadLinkCommand = new RelayCommand(url => OpenWebPage((string)url));
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(_parentApp));

            
        }

        public void Initialize((Mod? Shell, ModdedApp App) data)
        {
            _shell = data.Shell;
            _parentApp = data.App;

            OnPropertyChanged(nameof(ViewTitle));

            // Run async load without blocking
            Task.Run(async () => await LoadInitialData());
        }

        //TODO: calculate highest last crawled date across all versions and display in UI, maybe with a warning icon if it's been too long since last crawl
        private async Task LoadInitialData()
        {
            try
            {
                IsScanning = true;

                App.Current.Dispatcher.Invoke(() => GroupedMods.Clear());

                var data = await _storageService.GetAvailableVersionsByAppIdAsync(_parentApp.Id, _shell?.Id);

                foreach (var group in data)
                {
                    var groupVm = new ModGroupViewModel
                    {
                        ModId = group.Shell.Id,
                        ModName = group.Shell.Name,
                        RootSourceUrl = group.Shell.RootSourceUrl,
                        AvailableVersions = new ObservableCollection<AvailableMod>(group.Versions)
                    };

                    App.Current.Dispatcher.Invoke(() => GroupedMods.Add(groupVm));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load available versions: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
                OnPropertyChanged(nameof(HasNoMods));
            }
        }

        //private async Task SyncAllUsedMods()
        //{
        //    try
        //    {
        //        IsScanning = true;
        //        App.Current.Dispatcher.Invoke(() => ReviewViewModel.ReviewItems.Clear());

        //        var webResults = await _crawlerService.GetLatestVersionsForAppAsync(_parentApp);

        //        foreach (var webMod in webResults)
        //        {
        //            var changes = await _storageService.CompareAndIdentifyChangesAsync(webMod.ModId, _parentApp.Id, webMod.Versions);

        //            foreach (var change in changes)
        //            {
        //                App.Current.Dispatcher.Invoke(() =>
        //                {
        //                    ReviewViewModel.ReviewItems.Add(new SyncReviewItemViewModel(change.Entity, change.Type));
        //                });
        //            }
        //        }

        //        IsReviewOverlayVisible = ReviewViewModel.ReviewItems.Any();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Sync failed: {ex.Message}");
        //    }
        //    finally
        //    {
        //        IsScanning = false;
        //    }
        //}

        

        private async Task PromoteToInstalled(AvailableMod selectedVersion)
        {
            var result = MessageBox.Show(
                $"Confirm installation of version {selectedVersion.AvailableVersion}?\n\nThis will update your local installation record.",
                "Promote Version", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsScanning = true;
                    await _storageService.PromoteAvailableToInstalledAsync(selectedVersion, _parentApp.InstalledVersion);
                    await LoadInitialData();
                    MessageBox.Show("Successfully promoted to installed.", "Update Complete");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during promotion: {ex.Message}");
                }
                finally
                {
                    IsScanning = false;
                }
            }
        }

        private void OpenWebPage(string url)
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
                MessageBox.Show($"Could not open browser: {ex.Message}");
            }
        }
    }
}