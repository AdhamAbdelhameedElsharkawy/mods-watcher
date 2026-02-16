using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace ModsAutomator.Core.Entities
{
    public class UnusedModHistory
    {
        public int Id { get; set; }

        public Guid ModId { get; init; }

        public int ModdedAppId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string AppName { get; init; } = "";

        public string AppVersion { get; init; } = "";

        public DateOnly? RemovedAt { get; set; }

        //TODO:Prompet for adding a reason
        public string? Reason { get; set; } = "";

        public string? Description { get; set; } = "";

        public string RootSourceUrl { get; set; } = "";

        //CrawlerConfig

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

        public string Author { get; set; } = string.Empty;

    }
}
