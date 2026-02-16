using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using System;
using System.IO;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModItemViewModel : BaseViewModel
    {
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

        /// <summary>
        /// Returns the locally installed version or a placeholder if no installation exists.
        /// </summary>
        public string Version => Installed?.InstalledVersion ?? "Not Installed";

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
                    return Installed.InstalledVersion == AppVersion;

                // 3. Split the CSV and check for an exact match on any of the versions
                var supportedList = Installed.SupportedAppVersions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var version in supportedList)
                {
                    if (version.Trim().Equals(AppVersion, StringComparison.OrdinalIgnoreCase))
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

        // --- UI Logic Methods ---

        /// <summary>
        /// Manually triggers property changes for all calculated UI strings.
        /// </summary>
        public void RefreshSummary()
        {
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(IsCompatible));
            OnPropertyChanged(nameof(Version));
            OnPropertyChanged(nameof(IsUsed));
        }

        /// <summary>
        /// Aggregated status string for the UI Card.
        /// Format: [Active/Disabled] | [Compatibility] | [Watcher Status]
        /// </summary>
        public string Summary
        {
            get
            {
                if (Installed == null)
                    return "Pending Setup";

                string activeStatus = IsUsed ? "Active" : "Disabled";
                string compatibilityStatus = IsCompatible ? "Ok" : "VERSION MISMATCH";

                // Using your existing WatcherStatus Enum logic
                string watcherResult = Shell.WatcherStatus switch
                {
                    WatcherStatusType.UpdateFound => "UPDATE FOUND",
                    WatcherStatusType.Error => "Watcher: ERROR",
                    WatcherStatusType.Checking => "Watcher: SYNCING...",
                    _ => "Up to date"
                };

                return $"{activeStatus} | {compatibilityStatus} | {watcherResult}";
            }
        }

        // --- Constructor ---

        public ModItemViewModel(Mod shell, InstalledMod installed, ModCrawlerConfig config, string appVersion)
        {
            Shell = shell;
            Installed = installed;
            Config = config;
            AppVersion = appVersion;

            RefreshSummary();
        }
    }
}