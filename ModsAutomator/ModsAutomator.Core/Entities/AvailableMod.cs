using ModsAutomator.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    /// <summary>
    /// This class represents an available Mod version that can be installed or updated for an application
    /// </summary>
    public class AvailableMod : Mod
    {
        private int _id;
        private string _version = string.Empty;
        private DateOnly _releaseDate;
        private decimal _sizeMB;
        private string _downloadUrl = string.Empty;
        private PackageType _packageType;
        private int _packageFilesNumber;
        private string _supportedAppVersions = string.Empty;

        public string AvailableVersion { get => _version; set => _version = value; }
        public DateOnly ReleaseDate { get => _releaseDate; set => _releaseDate = value; }
        public decimal SizeMB { get => _sizeMB; set => _sizeMB = value; }
        public string DownloadUrl { get => _downloadUrl; set => _downloadUrl = value; }
        public PackageType PackageType { get => _packageType; set => _packageType = value; }
        public int PackageFilesNumber { get => _packageFilesNumber; set => _packageFilesNumber = value; }
        public string SupportedAppVersions { get => _supportedAppVersions; set => _supportedAppVersions = value; }
        public int Id1 { get => _id; set => _id = value; }
    }
}
