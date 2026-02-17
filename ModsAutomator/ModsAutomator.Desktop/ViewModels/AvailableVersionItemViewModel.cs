using ModsAutomator.Core.Entities;

namespace ModsAutomator.Desktop.ViewModels
{
    public class AvailableVersionItemViewModel : BaseViewModel
    {
        public AvailableMod Entity { get; }

        public bool IsInstalled { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsCompatible { get; }

        public AvailableVersionItemViewModel(AvailableMod entity, string currentAppVersion, string? installedVersion)
        {
            Entity = entity;

            // UI Trigger: Calculate compatibility once on load
            if (string.IsNullOrEmpty(entity.SupportedAppVersions) || string.IsNullOrEmpty(currentAppVersion))
            {
                IsCompatible = false;
            }
            else
            {
                IsCompatible = entity.SupportedAppVersions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Any(v => v.Trim().Equals(currentAppVersion, StringComparison.OrdinalIgnoreCase));
            }

            IsInstalled = !string.IsNullOrEmpty(installedVersion) &&
                      entity.AvailableVersion == installedVersion;
        }
    }
}
