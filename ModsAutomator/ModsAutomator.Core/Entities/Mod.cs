using ModsAutomator.Core.Enums;
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
        private Guid _id;
        private int _appId;
        private string _name = string.Empty;
        private string? _author = string.Empty;
        private string _rootSourceUrl = string.Empty;
        private bool _isDeprecated;
        private string? _description;
        private bool _isUsed;
        private bool _isWatchable;
        private bool _isCrawlable;
        private DateTime _lastWatched;
        private WatcherStatusType _watcherStatus;
        private string? _lastWatcherHash;
        private int _priorityOrder;

        public Guid Id { get => _id; set => _id = value; }
        public int AppId { get => _appId; init => _appId = value; }
        public string Name { get => _name; set => _name = value; }
        public string RootSourceUrl { get => _rootSourceUrl; set => _rootSourceUrl = value; }
        public bool IsDeprecated { get => _isDeprecated; set => _isDeprecated = value; }
        public string? Description { get => _description; set => _description = value; }
        public bool IsUsed { get => _isUsed; set => _isUsed = value; }
        public string? Author { get => _author; set => _author = value; }
        public bool IsWatchable { get => _isWatchable; set => _isWatchable = value; }
        public bool IsCrawlable { get => _isCrawlable; set => _isCrawlable = value; }
        public DateTime LastWatched { get => _lastWatched; set => _lastWatched = value; }
        public WatcherStatusType WatcherStatus { get => _watcherStatus; set => _watcherStatus = value; }
        public string? LastWatcherHash { get => _lastWatcherHash; set => _lastWatcherHash = value; }
        public int PriorityOrder { get => _priorityOrder; set => _priorityOrder = value; }
    }
}
