using ModsWatcher.Desktop.Enums;

namespace ModsWatcher.Desktop.ViewModels
{
    /// <summary>
    /// A single entry in the Library's state-filter dropdown — pairs the filter value
    /// with the label shown to the user.
    /// </summary>
    public class ModStateFilterOption
    {
        public ModStateFilter Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
