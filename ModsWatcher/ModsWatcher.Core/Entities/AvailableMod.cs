using ModsWatcher.Core.Enums;

namespace ModsWatcher.Core.Entities
{
    /// <summary>
    /// This class represents an available Mod version that can be installed or updated for an application
    /// </summary>
    public class AvailableMod : Mod
    {
        private int _internalId;
        private string? _version = string.Empty;
        private DateOnly? _releaseDate;
        private decimal? _sizeMB;
        private string? _downloadUrl = string.Empty;
        private PackageType _packageType;
        private int? _packageFilesNumber;
        private string? _supportedAppVersions = string.Empty;
        //Page where the mod was crawled, useful for debugging and tracking purposes
        private string? _crawledModUrl = string.Empty;
        private DateTime _lastCrawled;

        public string? AvailableVersion { get => _version; set => _version = value; }
        public DateOnly? ReleaseDate { get => _releaseDate; set => _releaseDate = value; }
        public decimal? SizeMB { get => _sizeMB; set => _sizeMB = value; }
        public string? DownloadUrl { get => _downloadUrl; set => _downloadUrl = value; }
        public PackageType PackageType { get => _packageType; set => _packageType = value; }
        public int? PackageFilesNumber { get => _packageFilesNumber; set => _packageFilesNumber = value; }
        public string? SupportedAppVersions { get => _supportedAppVersions; set => _supportedAppVersions = value; }
        public int InternalId { get => _internalId; set => _internalId = value; }
        public DateTime LastCrawled { get => _lastCrawled; set => _lastCrawled = value; }
        public string? CrawledModUrl { get => _crawledModUrl; set => _crawledModUrl = value; }
    }
}
