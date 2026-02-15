using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Services.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModShellDialogViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;
        public Mod Shell { get; }
        public ModCrawlerConfig Config { get; }
        public bool IsEditMode { get; }

        // UI logic: Lock Identity fields if in Edit Mode
        public bool CanEditIdentity => !IsEditMode;
        public string DialogTitle => IsEditMode ? "Edit Mod Identity" : "Register New Mod";

        #region Editable Properties

        public string Name
        {
            get => Shell.Name;
            set { Shell.Name = value; OnPropertyChanged(); }
        }

        public string? Author
        {
            get => Shell.Author;
            set { Shell.Author = value; OnPropertyChanged(); }
        }

        public string RootSourceUrl
        {
            get => Shell.RootSourceUrl;
            set { Shell.RootSourceUrl = value; OnPropertyChanged(); }
        }

        public string? Description
        {
            get => Shell.Description;
            set { Shell.Description = value; OnPropertyChanged(); }
        }

        #endregion

        #region Cascading Flag Logic

        public bool IsUsed
        {
            get => Shell.IsUsed;
            set
            {
                if (Shell.IsUsed != value)
                {
                    Shell.IsUsed = value;
                    OnPropertyChanged();

                    // Cascade: If not used, it cannot be watched
                    if (!value) IsWatchable = false;
                }
            }
        }

        public bool IsWatchable
        {
            get => Shell.IsWatchable;
            set
            {
                if (Shell.IsWatchable != value)
                {
                    Shell.IsWatchable = value;
                    OnPropertyChanged();

                    // Cascade: If not watchable, it cannot be crawled
                    if (!value) IsCrawlable = false;
                }
            }
        }

        public bool IsCrawlable
        {
            get => Shell.IsCrawlable;
            set
            {
                if (Shell.IsCrawlable != value)
                {
                    Shell.IsCrawlable = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Config Properties (Stage-Wise)

        // STAGE 1: The Watcher (Required if IsWatchable)
        public string WatcherXPath
        {
            get => Config.WatcherXPath;
            set { Config.WatcherXPath = value; OnPropertyChanged(); }
        }

        // STAGE 2: Link Discovery (Required if IsCrawlable)
        public string LinksCollectionXPath
        {
            get => Config.LinksCollectionXPath;
            set { Config.LinksCollectionXPath = value; OnPropertyChanged(); }
        }

        // STAGE 3: Data Scraper (Optional/Advanced if IsCrawlable)
        public string? VersionXPath
        {
            get => Config.VersionXPath;
            set { Config.VersionXPath = value; OnPropertyChanged(); }
        }

        public string? DownloadUrlXPath
        {
            get => Config.DownloadUrlXPath;
            set { Config.DownloadUrlXPath = value; OnPropertyChanged(); }
        }

        public string? ReleaseDateXPath
        {
            get => Config.ReleaseDateXPath;
            set { Config.ReleaseDateXPath = value; OnPropertyChanged(); }
        }

        public string? SizeXPath
        {
            get => Config.SizeXPath;
            set { Config.SizeXPath = value; OnPropertyChanged(); }
        }

        public string? SupportedAppVersionsXPath
        {
            get => Config.SupportedAppVersionsXPath;
            set { Config.SupportedAppVersionsXPath = value; OnPropertyChanged(); }
        }

        public string? PackageFilesNumberXPath
        {
            get => Config.PackageFilesNumberXPath;
            set { Config.PackageFilesNumberXPath = value; OnPropertyChanged(); }
        }

        #endregion

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ModShellDialogViewModel(IStorageService storageService, int appId, Mod? existingMod = null, ModCrawlerConfig? existingConfig = null)
        {
            _storageService = storageService;
            IsEditMode = existingMod != null;

            // Initialize Shell: Link to AppId and set default watcher state
            Shell = existingMod ?? new Mod
            {
                AppId = appId,
                Id = Guid.NewGuid(),
                WatcherStatus = WatcherStatusType.Idle,
                PriorityOrder = int.MaxValue
            };

            // Ensure Config is linked to ModId
            Config = existingConfig ?? new ModCrawlerConfig { ModId = Shell.Id };

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => Close(false));
        }

        private async Task SaveAsync()
        {
            try
            {
                if (IsEditMode)
                {
                    await _storageService.UpdateModWithConfigAsync(Shell, Config);
                }
                else
                {
                    await _storageService.SaveModWithConfigAsync(Shell, Config);
                }
                Close(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close(bool result)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = result;
                    window.Close();
                }
            }
        }
    }
}