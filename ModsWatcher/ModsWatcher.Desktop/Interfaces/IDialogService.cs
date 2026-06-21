using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;

namespace ModsWatcher.Desktop.Interfaces
{
    public interface IDialogService
    {
        bool ShowConfirmation(string message, string title);
        void ShowError(string message, string title = "Error");
        void ShowInfo(string message, string title = "Information");

        string? ShowPrompt(string message, string title);

        // Returns the full chosen path, or null if the user cancelled.
        string? ShowSaveFileDialog(string title, string filter, string defaultFileName = "");
        string? ShowOpenFileDialog(string title, string filter);

        Task<List<CrawledLink>> ShowLinkSelectorAsync(IEnumerable<CrawledLink> links);

        Task<(AvailableMod? Primary, List<AvailableMod> Selected)> ShowVersionSelectorAsync(List<AvailableMod> availableMods);
    }
}
