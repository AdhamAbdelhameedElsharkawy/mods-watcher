namespace ModsWatcher.Core.DTO
{
    /// <summary>
    /// Lightweight display model for a single dependency relation row.
    /// Carries the mod name alongside its ID so the view can render
    /// readable labels without additional lookups.
    /// </summary>
    public class ModDependencyDisplayDto
    {
        public string ModId { get; set; } = string.Empty;
        public string ModName { get; set; } = string.Empty;
    }
}