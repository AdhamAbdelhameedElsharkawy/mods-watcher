using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;
using ModsAutomator.Services.Interfaces;

namespace ModsAutomator.Services
{
    public class CrawlerService : ICrawlerService
    {
        public Task<IEnumerable<WebCrawlResultDto>> GetLatestVersionsForAppAsync(ModdedApp app)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AvailableMod>> GetLatestVersionsForModAsync(Guid modId, string url, DateTime? lastCrawled)
        {
            if (lastCrawled.HasValue && (DateTime.Now - lastCrawled.Value).TotalHours < 6)
            {
                return new List<AvailableMod>();
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ModsAutomator/1.0");

                var html = await client.GetStringAsync(url);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var versions = new List<AvailableMod>();

                // Logic assumes a standard table or list structure; adjust XPath per site
                var nodes = doc.DocumentNode.SelectNodes("//div[@class='version-row']");

                if (nodes != null)
                {
                    //foreach (var node in nodes)
                    //{
                    //    versions.Add(new AvailableMod
                    //    {
                    //        Id = modId,
                    //        AvailableVersion = node.SelectSingleNode(".//span[@class='v-num']").InnerText.Trim(),
                    //        DownloadUrl = node.SelectSingleNode(".//a").GetAttributeValue("href", ""),
                    //        ReleaseDate = DateOnly.FromDateTime(DateTime.Parse(node.SelectSingleNode(".//span[@class='date']").InnerText)),
                    //        SizeMB = double.Parse(node.SelectSingleNode(".//span[@class='size']").InnerText.Replace("MB", "")),
                    //        PackageType = node.SelectSingleNode(".//span[@class='type']").InnerText.Trim(),
                    //        SupportedAppVersions = node.SelectSingleNode(".//span[@class='compat']").InnerText.Trim(),
                    //        LastCrawled = DateTime.Now
                    //    });
                    //}
                }

                return versions;
            }
            catch (Exception ex)
            {
                throw new Exception($"Scraping failed for {url}: {ex.Message}");
            }
        }
    }
}
