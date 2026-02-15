using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;

namespace ModsAutomator.Services.Interfaces
{
    public interface IWatcherService
    {
        /// <summary>
        /// Performs Stage 1 Check: Scrapes, hashes, and updates Mod status in DB.
        /// </summary>
        Task RunStatusCheckAsync(IEnumerable<(Mod Shell, ModCrawlerConfig Config)> bundle);

        /// <summary>
        /// Stage 2, retrive all links from the root URL using the provided XPath in the config, and return them for user selection.
        /// </summary>
        /// <param name="rootUrl"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        Task<List<CrawledLink>> ExtractLinksAsync(string rootUrl, ModCrawlerConfig config);

        /// <summary>
        /// Stage 3, construct an AvailableMod by scraping the provided URL using the XPaths in the config, and return it for user confirmation before adding to watchlist.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        Task<AvailableMod?> ParseModDetailsAsync(string url, ModCrawlerConfig config);
    }
}
