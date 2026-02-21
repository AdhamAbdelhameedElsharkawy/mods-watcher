using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class ModShellDialogViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;
        public Mod Shell { get; }
        public ModCrawlerConfig Config { get; }
        public bool IsEditMode { get; }

        // UI logic: Lock Identity fields if in Edit Mode
        public bool CanEditIdentity => !IsEditMode;
        public string DialogTitle => IsEditMode ? "Edit Mod Identity" : "Register New Mod";

        #region Editable Properties

        [Required(ErrorMessage = "Mod Name is required.")]
        public string Name
        {
            get => Shell.Name;
            set { Shell.Name = value; ValidateProperty(value); OnPropertyChanged(); }
        }

        public string? Author
        {
            get => Shell.Author;
            set { Shell.Author = value; OnPropertyChanged(); }
        }

        [Required(ErrorMessage = "URL is required.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public string RootSourceUrl
        {
            get => Shell.RootSourceUrl;
            set { Shell.RootSourceUrl = value; ValidateProperty(value); OnPropertyChanged(); }
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
                    ValidateWatcherXpath();

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
                    ValidateCrawlableConfigs();
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
            set { Config.WatcherXPath = value; ValidateWatcherXpath(); OnPropertyChanged(); }
        }

        // STAGE 2: Link Discovery (Required if IsCrawlable)
        public string ModNameRegex
        {
            get => Config.ModNameRegex;
            set { Config.ModNameRegex = value; OnPropertyChanged(); }
        }

        // STAGE 3: Data Scraper (Optional/Advanced if IsCrawlable)
        public string? VersionXPath
        {
            get => Config.VersionXPath;
            set { Config.VersionXPath = value; ValidateCrawlableConfigs(); OnPropertyChanged(); }
        }

        public string? DownloadUrlXPath
        {
            get => Config.DownloadUrlXPath;
            set { Config.DownloadUrlXPath = value; ValidateCrawlableConfigs(); OnPropertyChanged(); }
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
            set { Config.SupportedAppVersionsXPath = value; ValidateCrawlableConfigs(); OnPropertyChanged(); }
        }

        public string? PackageFilesNumberXPath
        {
            get => Config.PackageFilesNumberXPath;
            set { Config.PackageFilesNumberXPath = value; OnPropertyChanged(); }
        }

        #endregion

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ModShellDialogViewModel(IStorageService storageService, int appId, IDialogService dialogService, ILogger logger, Mod? existingMod = null, ModCrawlerConfig? existingConfig = null) : base(logger)
        {
            _storageService = storageService;
            IsEditMode = existingMod != null;
            _dialogService = dialogService;

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

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !HasErrors);
            CancelCommand = new RelayCommand(_ => Close(false));

            ValidateAll();
        }

        private async Task SaveAsync()
        {
            try
            {
                _logger.LogInformation("Saving mod: {ModName} (EditMode: {IsEditMode})", Shell.Name, IsEditMode);

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
                _dialogService.ShowError($"Failed to save mod: {ex.Message}");
                _logger.LogError(ex, "Error saving mod: {ModName}", Shell.Name);

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

        private void ValidateWatcherXpath()
        {
            const string errorMsg = "Watcher Xpath is required.";

            if (IsWatchable && string.IsNullOrEmpty(WatcherXPath))
            {
                    AddCustomError(nameof(WatcherXPath), errorMsg);
            }
            else
            {
                RemoveCustomError(nameof(WatcherXPath), errorMsg);
            }
        }


        private void ValidateCrawlableConfigs()
        {
            const string errorMsg = "At least one XPath is required when Crawlable is enabled.";

            // 1. Always clear the error from all three properties first
            RemoveCustomError(nameof(VersionXPath), errorMsg);
            RemoveCustomError(nameof(DownloadUrlXPath), errorMsg);
            RemoveCustomError(nameof(SupportedAppVersionsXPath), errorMsg);

            // 2. If IsCrawlable is checked, verify the "At least one" rule
            if (IsCrawlable)
            {
                bool anyFilled = !string.IsNullOrWhiteSpace(VersionXPath) ||
                                 !string.IsNullOrWhiteSpace(DownloadUrlXPath) ||
                                 !string.IsNullOrWhiteSpace(SupportedAppVersionsXPath);

                if (!anyFilled)
                {
                    // 3. Mark all three as invalid so the user sees red borders on all 
                    // OR just pick one to host the message.
                    AddCustomError(nameof(VersionXPath), errorMsg);
                    AddCustomError(nameof(DownloadUrlXPath), errorMsg);
                    AddCustomError(nameof(SupportedAppVersionsXPath), errorMsg);
                }
            }
        }

        
    }
}