using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    /// <summary>
    /// This class represents a modded application e.g ATS, ETS2
    /// </summary>
    public class ModdedApp
    {
        private int _id;
        private string _name = string.Empty;
        private string? _description;
        private string _latestVersion = string.Empty;
        private string _installedVersion = string.Empty;
        private DateOnly _lastUpdatedDate;


        public int Id { get => this._id; init { this._id = value; } }
        public string Name { get => _name; set => _name = value; }
        public string? Description { get => _description; set => _description = value; }
        public string LatestVersion { get => _latestVersion; set => _latestVersion = value; }
        public string InstalledVersion { get => _installedVersion; set => _installedVersion = value; }
        public DateOnly LastUpdatedDate { get => _lastUpdatedDate; set => _lastUpdatedDate = value; }
    }
}
