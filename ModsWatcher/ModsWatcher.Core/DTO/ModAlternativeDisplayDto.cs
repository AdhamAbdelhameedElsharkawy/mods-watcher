namespace ModsWatcher.Core.DTO
{
    /// <summary>
    /// Lightweight display model for a single mod within an alternative group.
    /// Carries the mod name and active state alongside its ID so the view can
    /// render readable labels and highlight the currently-active member
    /// without additional lookups.
    /// </summary>
    public class ModAlternativeDisplayDto
    {
        public string ModId { get; set; } = string.Empty;
        public string ModName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
