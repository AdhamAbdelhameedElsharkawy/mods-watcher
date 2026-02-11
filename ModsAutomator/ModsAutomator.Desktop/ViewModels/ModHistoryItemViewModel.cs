using ModsAutomator.Core.Entities;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModHistoryItemViewModel : BaseViewModel
    {
        private readonly string _currentAppVersion;
        private readonly Func<bool> _isOverrideActive;

        public InstalledModHistory History { get; }

        public ModHistoryItemViewModel(InstalledModHistory history, string currentAppVersion, Func<bool> isOverrideActive)
        {
            History = history;
            _currentAppVersion = currentAppVersion;
            _isOverrideActive = isOverrideActive;
        }

        // Logical Check: Does this history entry match the current game version?
        public bool IsCompatible => History.AppVersion == _currentAppVersion;

        // UI Logic: Should the rollback button be clickable?
        public bool CanRollback => IsCompatible || _isOverrideActive();

        // Call this when the "Override" checkbox in the parent VM changes
        public void RefreshCompatibility()
        {
            OnPropertyChanged(nameof(CanRollback));
        }
    }
}