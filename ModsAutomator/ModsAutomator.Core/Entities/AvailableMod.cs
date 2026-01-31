using ModsAutomator.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    /// <summary>
    /// This class represents an available Mod version that can be installed or updated for an application
    /// </summary>
    public class AvailableMod
    {
        private int _id;
        private int _modId;
        private string _version = string.Empty;
        private string _supportedAppVersion = string.Empty;
        private DateOnly _releaseDate;
        private decimal _sizeMB;
        private string _downloadUrl = string.Empty;
        private PackageType _packageType;
        private int _packageFilesNumber;
        public int Id { get => _id; set => _id = value; }
        public int ModId { get => _modId; set => _modId = value; }
        public string AvailableVersion { get => _version; set => _version = value; }
        public DateOnly ReleaseDate { get => _releaseDate; set => _releaseDate = value; }
        public decimal SizeMB { get => _sizeMB; set => _sizeMB = value; }
        public string SupportedAppVersion { get => _supportedAppVersion; set => _supportedAppVersion = value; }
        public string DownloadUrl { get => _downloadUrl; set => _downloadUrl = value; }
        public PackageType PackageType { get => _packageType; set => _packageType = value; }
        public int PackageFilesNumber { get => _packageFilesNumber; set => _packageFilesNumber = value; }
    }
}
