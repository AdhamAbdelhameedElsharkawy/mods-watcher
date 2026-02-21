using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Services;

namespace ModsWatcher.Desktop.ViewModels
{
    public class AvailableVersionItemViewModel : BaseViewModel
    {
        private readonly CommonUtils _commonUtils;
        
        public AvailableMod Entity { get; }

        public bool IsInstalled { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public bool IsCompatible { get; }

        public AvailableVersionItemViewModel(AvailableMod entity, string currentAppVersion, string? installedVersion, CommonUtils commonUtils, ILogger logger) : base(logger)
        {
            Entity = entity;
            _commonUtils = commonUtils;

            // UI Trigger: Calculate compatibility once on load
            if (string.IsNullOrEmpty(entity.SupportedAppVersions) || string.IsNullOrEmpty(currentAppVersion))
            {
                IsCompatible = false;
            }
            else
            {
                IsCompatible = entity.SupportedAppVersions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Any(v => _commonUtils.IsModCompatibleWithAppVersion(v, currentAppVersion));
            }

            IsInstalled = !string.IsNullOrEmpty(installedVersion) &&
                      entity.AvailableVersion == installedVersion;
        }
    }
}
