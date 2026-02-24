namespace ModsWatcher.Services.Config
{
    public class WatcherSettings
    {
        public int CheckingThresholdHours { get; set; } = 6; // Default fallback
        public string PlayWrightDebugPath { get; set; } = string.Empty;
        public string PlayWrightUserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        public bool PlayWrightHeadless { get; set; } = true; // Default to headless mode
        public int PlayWrightPageTimeout { get; set; } = 30000; // Default timeout in milliseconds
        public int PlayWrightSelectorTimeout { get; set; } = 10000; // Default navigation timeout in milliseconds
        //Not used yet, but could be useful for future features like retry logic or rate limiting
        public byte PlayWrightRetries { get; set; } = 3; // Default max retry attempts for failed checks

    }
}
