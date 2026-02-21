namespace ModsWatcher.Core.Entities
{
    public class InstalledModHistory
    {
        public int InternalId { get; set; }

        public Guid ModId { get; init; }

        public string Version { get; init; } = "";

        public string AppVersion { get; init; } = "";

        public DateOnly InstalledAt { get; set; }

        public DateOnly? RemovedAt { get; set; }

        public string? DownloadUrl { get; set; } = "";
        //TODO:Unused, consider removing or repurposing
        public bool IsRollbackTarget { get; set; }
    }
}
