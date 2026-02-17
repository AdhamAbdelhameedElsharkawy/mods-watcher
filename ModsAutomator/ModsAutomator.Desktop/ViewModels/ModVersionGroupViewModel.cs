using ModsAutomator.Core.Entities;
using System.Collections.ObjectModel;

namespace ModsAutomator.Desktop.ViewModels
{
    /// <summary>
    /// Helper class to represent the Mod group in the UI
    /// </summary>
    public class ModVersionGroupViewModel : BaseViewModel
    {
        public Guid ModId { get; set; }
        public string ModName { get; set; }
        public string RootSourceUrl { get; set; }

        private bool _isOverrideEnabled;
        public bool IsOverrideEnabled
        {
            get => _isOverrideEnabled;
            set => SetProperty(ref _isOverrideEnabled, value);
        }

        // The collection of wrapped versions
        public ObservableCollection<AvailableVersionItemViewModel> Versions { get; set; } = new();
    }
}
