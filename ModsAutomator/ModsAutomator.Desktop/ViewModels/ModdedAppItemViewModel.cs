using ModsAutomator.Core.Entities;
using System.Windows;

namespace ModsAutomator.Desktop.ViewModels
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

        // Row 2 Stats: Potential Updates (Replaced Size/Incompatible logic)
        private int _potentialUpdatesCount;
        public int PotentialUpdatesCount
        {
            get => _potentialUpdatesCount;
            set => SetProperty(ref _potentialUpdatesCount, value);
        }

        public ModdedAppItemViewModel(ModdedApp app)
        {
            App = app;
        }
    }
}