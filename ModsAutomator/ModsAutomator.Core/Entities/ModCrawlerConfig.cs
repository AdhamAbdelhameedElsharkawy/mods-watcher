using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    public class ModCrawlerConfig
    {
        public int Id { get; set; }
        public Guid ModId { get; set; } // FK to Mod.Id

        // 1. The Watcher (Manual Trigger)
        // This XPath points to a version string or a "Last Updated" text on the root page
        public string WatcherXPath { get; set; } = string.Empty;

        
        public string ModNameRegex { get; set; } = string.Empty;

        // 3. The Data Scraper (Auto-filling AvailableMod)
        // These map directly to your AvailableMod properties
        public string? VersionXPath { get; set; }
        public string? ReleaseDateXPath { get; set; }
        public string? SizeXPath { get; set; }
        public string? DownloadUrlXPath { get; set; }
        public string? SupportedAppVersionsXPath { get; set; }
        public string? PackageFilesNumberXPath { get; set; }
    }
}
