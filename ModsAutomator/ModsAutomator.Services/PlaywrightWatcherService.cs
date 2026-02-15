using Microsoft.Playwright;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

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
    }
}