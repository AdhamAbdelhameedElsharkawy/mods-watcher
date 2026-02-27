using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using System.Collections.ObjectModel;

namespace ModsWatcher.Desktop.ViewModels
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

        // NEW: Source of truth for all versions
        public List<AvailableVersionItemViewModel> AllVersions { get; set; } = new();

        // NEW: Filtered collection for the UI ItemsControl
        private ObservableCollection<AvailableVersionItemViewModel> _displayedVersions = new();
        public ObservableCollection<AvailableVersionItemViewModel> DisplayedVersions
        {
            get => _displayedVersions;
            set => SetProperty(ref _displayedVersions, value);
        }

        // NEW: Options for the ComboBox
        public ObservableCollection<string> AppVersionFilterOptions { get; set; } = new();

        private string _selectedAppVersionFilter = "All";
        public string SelectedAppVersionFilter
        {
            get => _selectedAppVersionFilter;
            set
            {
                if (SetProperty(ref _selectedAppVersionFilter, value))
                    ApplyFilter();
            }
        }

        public ModVersionGroupViewModel(ILogger logger) : base(logger) { }

        public void InitializeFilters()
        {
            // Extract unique versions from the comma-separated strings in Entity.SupportedAppVersions
            var versions = AllVersions
                .SelectMany(v => v.Entity.SupportedAppVersions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                .Select(v => v.Trim())
                .Distinct()
                .OrderByDescending(v => v)
                .ToList();

            versions.Insert(0, "All");
            AppVersionFilterOptions = new ObservableCollection<string>(versions);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (SelectedAppVersionFilter == "All")
            {
                DisplayedVersions = new ObservableCollection<AvailableVersionItemViewModel>(AllVersions);
            }
            else
            {
                var filtered = AllVersions.Where(v =>
                    v.Entity.SupportedAppVersions != null &&
                    v.Entity.SupportedAppVersions.Split(',').Select(x => x.Trim()).Contains(SelectedAppVersionFilter));

                DisplayedVersions = new ObservableCollection<AvailableVersionItemViewModel>(filtered);
            }
        }
    }
}
