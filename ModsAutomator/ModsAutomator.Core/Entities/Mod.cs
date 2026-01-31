using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    /// <summary>
    /// This class represents an abstract Mod that is used in an app
    /// Root source URL is used by web crawler to check on latest versions of the Mod
    /// </summary>
    public class Mod
    {
        private int _id;
        private int _appId;
        private string _name = string.Empty;
        private string _rootSourceUrl = string.Empty;
        private bool _isDeprecated;

        public int Id { get => _id; set => _id = value; }
        public int AppId { get => _appId; set => _appId = value; }
        public string Name { get => _name; set => _name = value; }
        public string RootSourceUrl { get => _rootSourceUrl; set => _rootSourceUrl = value; }
        public bool IsDeprecated { get => _isDeprecated; set => _isDeprecated = value; }
    }
}
