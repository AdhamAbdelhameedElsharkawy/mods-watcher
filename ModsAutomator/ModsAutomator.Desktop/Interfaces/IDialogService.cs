using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;

namespace ModsAutomator.Desktop.Interfaces
{
    public interface IDialogService
    {
        bool ShowConfirmation(string message, string title);
        void ShowError(string message, string title = "Error");
        void ShowInfo(string message, string title = "Information");

        Task<List<CrawledLink>> ShowLinkSelectorAsync(IEnumerable<CrawledLink> links);

        Task<(AvailableMod? Primary, List<AvailableMod> Selected)> ShowVersionSelectorAsync(List<AvailableMod> availableMods);
    }
}
