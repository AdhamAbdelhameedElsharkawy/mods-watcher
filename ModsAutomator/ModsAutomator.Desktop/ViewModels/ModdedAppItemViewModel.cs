using ModsAutomator.Core.Entities;
using System.Windows;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModdedAppItemViewModel : BaseViewModel
    {
        public ModdedApp App { get; }

        // Row 0 & 1 Identity
        public string Name => App.Name;

        // Ensure this points to the correct entity property
        public string InstalledVersion => App.InstalledVersion;

        // Row 2 Stats: Active Mods
        private int _activeModsCount;
        public int ActiveModsCount
        {
            get => _activeModsCount;
            set => SetProperty(ref _activeModsCount, value);
        }

        // Row 2 Stats: Total Size (Changed to decimal for consistency)
        private decimal _totalUsedSizeMB;
        public decimal TotalUsedSizeMB
        {
            get => _totalUsedSizeMB;
            set => SetProperty(ref _totalUsedSizeMB, value);
        }

        // Incompatibility Logic
        private int _incompatibleCount;
        public int IncompatibleCount
        {
            get => _incompatibleCount;
            set
            {
                if (SetProperty(ref _incompatibleCount, value))
                {
                    OnPropertyChanged(nameof(IncompatibleCountVisibility));
                }
            }
        }

        public Visibility IncompatibleCountVisibility =>
            IncompatibleCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        public ModdedAppItemViewModel(ModdedApp app)
        {
            App = app;
        }
    }
}