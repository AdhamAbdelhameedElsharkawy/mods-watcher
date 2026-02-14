using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;

namespace ModsAutomator.Services.Interfaces
{
    public interface ICrawlerService
    {
        // Matches VM: await _crawlerService.GetLatestVersionsForAppAsync(_parentApp)
        Task<IEnumerable<WebCrawlResultDto>> GetLatestVersionsForAppAsync(ModdedApp app);

        // Matches VM: await _crawlerService.GetLatestVersionsForModAsync(targetGroup.ModId, targetGroup.RootSourceUrl)
        Task<List<AvailableMod>> GetLatestVersionsForModAsync(Guid modId, string url, DateTime? lastCrawled);
    }

    
}