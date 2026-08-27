namespace ModsWatcher.Core.DTO
{
    /// <summary>
    /// A node in the dependency impact tree, built when a parent mod is about
    /// to be deactivated, retired, or purged. Carries the mod's display name
    /// alongside its ID so the UI can render a readable tree without additional lookups.
    /// Children represent mods that depend on this node's mod.
    /// </summary>
    public class DependencyTreeNodeDto
    {
        public string ModId { get; set; } = string.Empty;
        public string ModName { get; set; } = string.Empty;
        public List<DependencyTreeNodeDto> Children { get; set; } = new();

        public bool HasChildren => Children.Count > 0;
    }
}