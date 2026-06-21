namespace ModsWatcher.Core.DTO
{
    /// <summary>
    /// Portable representation of a ModCrawlerConfig, used for exporting to
    /// and importing from a local JSON file. Deliberately excludes Id/ModId,
    /// since those are specific to the mod the config was originally created for.
    /// SourceModName is informational only - it identifies where the file came
    /// from, but is never applied back onto the importing mod.
    /// </summary>
    public class ModCrawlerConfigExportDto
    {
        public string SourceModName { get; set; } = string.Empty;

        public string WatcherXPath { get; set; } = string.Empty;

        public string ModNameRegex { get; set; } = string.Empty;

        public string? VersionXPath { get; set; }
        public string? ReleaseDateXPath { get; set; }
        public string? SizeXPath { get; set; }
        public string? DownloadUrlXPath { get; set; }
        public string? SupportedAppVersionsXPath { get; set; }
        public string? PackageFilesNumberXPath { get; set; }
    }
}