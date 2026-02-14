using ModsAutomator.Core.Entities;
using System.Collections.ObjectModel;

namespace ModsAutomator.Desktop.ViewModels
{
    /// <summary>
    /// Helper class to represent the Mod group in the UI
    /// </summary>
    public class ModGroupViewModel : BaseViewModel
    {
        public Guid ModId { get; set; }
        public string ModName { get; set; }
        public string RootSourceUrl { get; set; }
        public ObservableCollection<AvailableMod> AvailableVersions { get; set; } = new();
    }
}
