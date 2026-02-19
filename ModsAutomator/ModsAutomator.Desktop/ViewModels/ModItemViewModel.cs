using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Desktop.Services;
using System;
using System.IO;
using System.Windows.Markup;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModItemViewModel : BaseViewModel
    {
        
        private readonly CommonUtils _commonUtils;

        // --- Core Triad Properties ---
        public Mod Shell { get; set; }
        public InstalledMod Installed { get; set; } // Can be null if not setup
        public ModCrawlerConfig Config { get; set; } // Required for Watcher/Crawler

        // --- Reactive State Properties ---

        private string _appVersion = string.Empty;
        /// <summary>
        /// Injected from the parent LibraryViewModel to represent the current environment.
        /// Updating this triggers a re-evaluation of compatibility status.
        /// </summary>
        public string AppVersion
        {
            get => _appVersion;
            set
            {
                if (SetProperty(ref _appVersion, value))
                {
                    RefreshSummary();
                }
            }
        }

        public string Name => Shell.Name;

        public string Author => Shell.Author;

        /// <summary>
        /// Returns the locally installed version or a placeholder if no installation exists.
        /// </summary>
        public string Version => Installed?.InstalledVersion ?? "Not Installed";

        public string Description => Shell.Description ?? "";

        /// <summary>
        /// Logic: Returns true if:
        /// 1. There is no installation record (Shell only).
        /// 2. The AppVersion hasn't been set yet.
        /// 3. The Installed mod's SupportedAppVersions matches the current AppVersion.
        /// </summary>
        public bool IsCompatible
        {
            get
            {
                // 1. If no installation, we don't flag mismatch
                if (Installed == null || string.IsNullOrEmpty(AppVersion))
                    return true;

                // 2. If the string is empty/null in DB, we assume unknown/incompatible 
                // (or return true if you prefer a "don't care" approach)
                if (string.IsNullOrEmpty(Installed.SupportedAppVersions))
                    return false;

                // 3. Split the CSV and check for an exact match on any of the versions
                var supportedList = Installed.SupportedAppVersions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var version in supportedList)
                {
                    if (_commonUtils.IsModCompatibleWithAppVersion(version, AppVersion))
                        return true;
                }

                return false;
            }
        }

        public bool IsUsed
        {
            get => Shell?.IsUsed ?? false;
            set
            {
                if (Shell != null)
                {
                    Shell.IsUsed = value;
                    OnPropertyChanged(); // Notifies the Badge/Toggle
                    RefreshSummary();    // Updates the status text
                }
            }
        }

        public int PriorityOrder
        {
            get => Shell?.PriorityOrder ?? int.MaxValue; 
            set
            {
                if (Shell != null)
                {
                    Shell.PriorityOrder = value;
                    OnPropertyChanged();
                    RefreshSummary();
                }
            }

        }

        // --- UI Logic Methods ---

        /// <summary>
        /// Manually triggers property changes for all calculated UI strings.
        /// </summary>
        public void RefreshSummary()
        {
            OnPropertyChanged(nameof(IsCompatible));
            OnPropertyChanged(nameof(Version));
            OnPropertyChanged(nameof(IsUsed));
            OnPropertyChanged(nameof(PriorityOrder));
            OnPropertyChanged(nameof(Summary));
        }

        /// <summary>
        /// Aggregated status string for the UI Card.
        /// Format: [Active/Disabled] | [Compatibility] | [Watcher Status]
        /// </summary>
        public string Summary
        {
            get
            {
                if (Installed != null) {
                    string activeStatus = IsUsed ? "Active" : "Disabled";
                    string compatibilityStatus = IsCompatible ? "Ok" : "VERSION MISMATCH";

                    string watcherResult = Shell.WatcherStatus switch
                    {
                        WatcherStatusType.UpdateFound => "UPDATE FOUND",
                        WatcherStatusType.Error => "Watcher: ERROR",
                        WatcherStatusType.Checking => "Watcher: SYNCING...",
                        _ => "Up to date"
                    };

                    // Header line with statuses
                    string statusLine = $"{activeStatus} | {compatibilityStatus} | {watcherResult}";

                    // Add description on a new line if it exists
                    if (!string.IsNullOrWhiteSpace(Description))
                    {
                        return $"{statusLine}{Environment.NewLine}{Description}";
                    }

                    return statusLine;
                }
                else
                {
                    string pending = "Pending Setup";
                    // Add description on a new line if it exists
                    if (!string.IsNullOrWhiteSpace(Description))
                    {
                        return $"{pending}{Environment.NewLine}{Description}";
                    }

                    return pending;
                }



                
            }
        }

        // --- Constructor ---

        public ModItemViewModel(Mod shell, InstalledMod installed, ModCrawlerConfig config, string appVersion, CommonUtils commonUtils)
        {
            Shell = shell;
            Installed = installed;
            Config = config;
            AppVersion = appVersion;
            _commonUtils = commonUtils;


            RefreshSummary();
        }
    }
}