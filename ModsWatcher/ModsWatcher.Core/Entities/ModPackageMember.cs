namespace ModsWatcher.Core.Entities
{
    /// <summary>
    /// A lightweight member of a mod package. Cannot function independently and carries
    /// none of the watcher/crawler/installed-version machinery a real Mod has — just
    /// enough to identify it and order it within the package.
    /// MainModId points at the package's main mod, which is a regular, fully-tracked Mod
    /// and is what the rest of the app (dependencies, alternatives, activation) interacts
    /// with on the package's behalf. A main mod is only considered a "package" while it
    /// has at least one member.
    /// </summary>
    public class ModPackageMember
    {
        public int InternalId { get; set; }

        public Guid MainModId { get; init; }

        public string Name { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string? Url { get; set; }

        public int PriorityOrder { get; set; }
    }
}
