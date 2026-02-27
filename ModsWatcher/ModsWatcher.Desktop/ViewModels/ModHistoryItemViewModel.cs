using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Services;

namespace ModsWatcher.Desktop.ViewModels
{
    public class ModHistoryItemViewModel : BaseViewModel
    {
        private readonly string _currentAppVersion;
        private readonly Func<bool> _isOverrideActive;
        private readonly CommonUtils _commonUtils;

        public InstalledModHistory History { get; }

        public ModHistoryItemViewModel(InstalledModHistory history, string currentAppVersion, Func<bool> isOverrideActive, CommonUtils commonUtils, ILogger logger) : base(logger)
        {
            History = history;
            _currentAppVersion = currentAppVersion;
            _isOverrideActive = isOverrideActive;
            _commonUtils = commonUtils;
        }

        // Logical Check: Does this history entry match the current game version?
        public bool IsCompatible => _commonUtils.IsModCompatibleWithAppVersion(History.AppVersion, _currentAppVersion);

        // UI Logic: Should the rollback button be clickable?
        public bool CanRollback => IsCompatible || _isOverrideActive();

        // Call this when the "Override" checkbox in the parent VM changes
        // or when the filter logic needs to refresh the UI state
        public void RefreshCompatibility()
        {
            OnPropertyChanged(nameof(IsCompatible));
            OnPropertyChanged(nameof(CanRollback));
        }
    }
}