using ModsAutomator.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    /// <summary>
    /// This class represents an current installed Mod in an application, it has 1:1 relationship with Mod entity
    /// </summary>
    public class InstalledMod
    {
        private int _id;
        private int _modId;
        private string _installedVersion = string.Empty;
        private DateOnly _installedDate;
        private decimal _installedSizeMB;
        private PackageType _packageType;
        private int _packageFilesNumber;

        public int Id { get => _id; set => _id = value; }
        public int ModId { get => _modId; set => _modId = value; }
        public string InstalledVersion { get => _installedVersion; set => _installedVersion = value; }
        public DateOnly InstalledDate { get => _installedDate; set => _installedDate = value; }
        public decimal InstalledSizeMB { get => _installedSizeMB; set => _installedSizeMB = value; }
        public PackageType PackageType { get => _packageType; set => _packageType = value; }
        public int PackageFilesNumber { get => _packageFilesNumber; set => _packageFilesNumber = value; }
    }
}
