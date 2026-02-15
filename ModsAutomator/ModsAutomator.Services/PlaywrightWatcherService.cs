using Microsoft.Playwright;
using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ModsAutomator.Services
{
    public class PlaywrightWatcherService : IWatcherService, IAsyncDisposable
    {
        private readonly IStorageService _storageService;

        // Singleton instances to avoid process bloat
        private static IPlaywright? _playwright;
        private static IBrowser? _browser;
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public PlaywrightWatcherService(IStorageService storageService)
        {
            _storageService = storageService;
        }

        private async Task EnsureBrowserAsync()
        {
            if (_browser != null) return;

            await _lock.WaitAsync();
            try
            {
                if (_browser == null)
                {
                    _playwright = await Playwright.CreateAsync();
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                    {
                        Headless = true
                    });
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RunStatusCheckAsync(IEnumerable<(Mod Shell, ModCrawlerConfig Config)> bundle)
        {
            await EnsureBrowserAsync();

            // Create one context for the entire bundle to share cache/cookies if needed
            // Matching your fast Python UserAgent exactly
            await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            });

            foreach (var (shell, config) in bundle)
            {
                var page = await context.NewPageAsync();
                try
                {
                    // DOMContentLoaded is the secret to the speed you liked in Python
                    await page.GotoAsync(shell.RootSourceUrl, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });

                    var locator = page.Locator($"xpath={config.WatcherXPath}").First;
                    await locator.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = 5000
                    });

                    string content = (await locator.InnerTextAsync()).Trim();
                    string currentHash = GenerateMd5Hash(content);

                    if (shell.LastWatcherHash != currentHash)
                    {
                        shell.LastWatcherHash = currentHash;
                        shell.WatcherStatus = WatcherStatusType.UpdateFound;
                        shell.LastWatched = DateTime.UtcNow;

                        await _storageService.UpdateModShellAsync(shell);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Scrape Error] {shell.Name}: {ex.Message}");
                }
                finally
                {
                    await page.CloseAsync(); // Close tab, but keep browser process alive
                }
            }
        }

        public async Task<List<CrawledLink>> ExtractLinksAsync(string rootUrl, ModCrawlerConfig config)
        {
            await EnsureBrowserAsync();

            await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            });

            var page = await context.NewPageAsync();

            try
            {
                // 1. Exact same navigation logic
                await page.GotoAsync(rootUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });

                // 2. Exact same wait logic
                await page.WaitForSelectorAsync("a", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 10000 });

                // 3. Exact same full-page extraction logic
                var anchors = await page.QuerySelectorAllAsync("a");

                var crawledLinks = new List<CrawledLink>();

                // Use the Regex from your config if it exists
                var regex = !string.IsNullOrEmpty(config.ModNameRegex)
                            ? new Regex(config.ModNameRegex, RegexOptions.IgnoreCase)
                            : null;

                foreach (var anchor in anchors)
                {
                    var url = await anchor.GetAttributeAsync("href");
                    var text = await anchor.InnerTextAsync();

                    if (!string.IsNullOrEmpty(url) && url.StartsWith("http") && !string.IsNullOrWhiteSpace(text))
                    {
                        string cleanText = text.Trim();

                        // 4. Exact same filtering (Regex + ignore profile links)
                        if ((regex == null || regex.IsMatch(cleanText)) && !url.Contains("/user/"))
                        {
                            crawledLinks.Add(new CrawledLink
                            {
                                DisplayText = cleanText,
                                Url = url,
                                IsSelected = false
                            });
                        }
                    }
                }

                // 5. Deduplicate by URL and return
                return crawledLinks
                    .GroupBy(x => x.Url)
                    .Select(g => g.First())
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return new List<CrawledLink>();
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        public async Task<AvailableMod?> ParseModDetailsAsync(string url, ModCrawlerConfig config)
        {
            await EnsureBrowserAsync();

            // Using a fresh context for each deep scrape to avoid cache/state issues
            await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            });

            var page = await context.NewPageAsync();

            try
            {
                // 1. Navigation matching Python: domcontentloaded
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });

                // 2. The critical 1-second delay for JS rendering (Matches Python: wait_for_timeout)
                await page.WaitForTimeoutAsync(1000);

                var mod = new AvailableMod { CrawledModUrl = url };

                // 3. Extracting values using the exact logic from the Python loop
                mod.AvailableVersion = await GetXPathText(page, config.VersionXPath) ?? "Unknown";
                // Ensure supported versions is CSV formatted (cleaning up extra spaces/newlines)
                var rawSupported = await GetXPathText(page, config.SupportedAppVersionsXPath);
                mod.SupportedAppVersions = !string.IsNullOrWhiteSpace(rawSupported)
                    ? string.Join(", ", rawSupported.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
                    : string.Empty;
                mod.DownloadUrl = await GetXPathText(page, config.DownloadUrlXPath) ?? url;

                // New: Determine PackageType from the DownloadUrl
                mod.PackageType = GetPackageTypeFromUrl(mod.DownloadUrl);

                // Size Parsing (Python results[key] = await element.inner_text())
                var sizeStr = await GetXPathText(page, config.SizeXPath);
                mod.SizeMB = ParseSize(sizeStr);

                // Date Parsing
                var dateStr = await GetXPathText(page, config.ReleaseDateXPath);
                mod.ReleaseDate = DateOnly.TryParse(dateStr, out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);

                // File Count Parsing
                var fileCountStr = await GetXPathText(page, config.PackageFilesNumberXPath);
                mod.PackageFilesNumber = int.TryParse(fileCountStr, out var num) ? num : 1;

                return mod;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Deep Scrape Error] {url}: {ex.Message}");
                return null;
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        // THE HELPER METHOD: Matches the Python "if element.count() > 0" logic
        private async Task<string?> GetXPathText(IPage page, string? xpath)
        {
            if (string.IsNullOrEmpty(xpath)) return null;

            try
            {
                // Ensure the xpath= prefix is present (Matches Python: locator(f"xpath={xpath}"))
                string selector = xpath.StartsWith("//") || xpath.StartsWith("/") ? $"xpath={xpath}" : xpath;

                var locator = page.Locator(selector);

                // Matches Python: if await element.count() > 0:
                if (await locator.CountAsync() > 0)
                {
                    var text = await locator.First.InnerTextAsync();
                    return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                }
            }
            catch
            {
                // Matches Python: results[key] = "Error locating element"
                return null;
            }

            return null;
        }

        // Utility for safe size conversion
        private decimal ParseSize(string? input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            // Basic cleaning (removing 'MB', 'GB', spaces)
            var cleaned = new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return decimal.TryParse(cleaned, out var result) ? result : 0;
        }

        private string GenerateMd5Hash(string input)
        {
            byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes).ToLower(); // Lowercase matches Python's hashlib
        }

        public async ValueTask DisposeAsync()
        {
            // Optional: Close browser when service is disposed if not intended to stay resident
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }

        private PackageType GetPackageTypeFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return PackageType.Unknown;

            var extension = System.IO.Path.GetExtension(url).ToLower().Replace(".", "");

            return extension switch
            {
                "zip" => PackageType.Zip,
                "rar" => PackageType.Rar,
                "scs" => PackageType.Scs,
                _ => PackageType.Unknown
            };
        }
    }
}