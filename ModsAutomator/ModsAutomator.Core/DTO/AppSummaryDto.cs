using ModsWatcher.Core.Entities;

namespace ModsWatcher.Core.DTO
{
    public class AppSummaryDto
    {
        public ModdedApp App { get; set; }
        public int ActiveCount { get; set; }
        public int PotentialUpdatesCount { get; set; }
    }
}
