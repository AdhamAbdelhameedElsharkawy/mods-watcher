using ModsWatcher.Core.Entities;
using System.Windows;

namespace ModsWatcher.Desktop.ViewModels
{
    public class ModdedAppItemViewModel : BaseViewModel
    {
        public ModdedApp App { get; }

        // Row 0 & 1 Identity
        public string Name => App.Name;
        public string InstalledVersion => App.InstalledVersion;

        // Row 2 Stats: Active Mods
        private int _activeModsCount;
        public int ActiveModsCount
        {
            get => _activeModsCount;
            set => SetProperty(ref _activeModsCount, value);
        }

        public string LatestVersion => App.LatestVersion;

        // Convert DateOnly to DateTime so WPF StringFormat works correctly
        public DateTime LastUpdatedDate => App.LastUpdatedDate.ToDateTime(TimeOnly.MinValue);

        // Row 2 Stats: Potential Updates (Replaced Size/Incompatible logic)
        private int _potentialUpdatesCount;
        public int PotentialUpdatesCount
        {
            get => _potentialUpdatesCount;
            set => SetProperty(ref _potentialUpdatesCount, value);
        }

        private bool _isSyncing;
        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        public ModdedAppItemViewModel(ModdedApp app)
        {
            App = app;
        }
    }
}