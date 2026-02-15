using ModsAutomator.Core.Entities;

namespace ModsAutomator.Services.Interfaces
{
    public interface IWatcherService
    {
        /// <summary>
        /// Performs Stage 1 Check: Scrapes, hashes, and updates Mod status in DB.
        /// </summary>
        Task RunStatusCheckAsync(IEnumerable<(Mod Shell, ModCrawlerConfig Config)> bundle);
    }
}
