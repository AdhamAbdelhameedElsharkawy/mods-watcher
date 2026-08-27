namespace ModsWatcher.Core.Entities
{
    /// <summary>
    /// Represents a directional dependency between two mods.
    /// DependentMod requires ParentMod to be active in order to function correctly.
    /// A mod can have multiple parents and multiple dependents.
    /// Circular references are prevented at the service layer before any insert.
    /// </summary>
    public class ModDependency
    {
        private Guid _dependentModId = Guid.Empty;
        private Guid _parentModId = Guid.Empty;

        /// <summary>
        /// The mod that has the dependency (requires the parent to be active).
        /// </summary>
        public Guid DependentModId { get => _dependentModId; set => _dependentModId = value; }

        /// <summary>
        /// The mod being depended upon. Cannot be deactivated/retired/purged
        /// without resolving this dependency first.
        /// </summary>
        public Guid ParentModId { get => _parentModId; set => _parentModId = value; }
    }
}