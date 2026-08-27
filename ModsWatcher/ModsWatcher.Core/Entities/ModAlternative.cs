namespace ModsWatcher.Core.Entities
{
    /// <summary>
    /// Represents a mutually-exclusive relationship between two mods.
    /// ModId and AlternativeModId are alternatives to each other — at most one
    /// mod in the connected group they belong to may be active at any time.
    /// The relation is undirected; which mod is stored as ModId vs AlternativeModId
    /// is not significant.
    /// </summary>
    public class ModAlternative
    {
        private Guid _modId = Guid.Empty;
        private Guid _alternativeModId = Guid.Empty;

        public Guid ModId { get => _modId; set => _modId = value; }

        public Guid AlternativeModId { get => _alternativeModId; set => _alternativeModId = value; }
    }
}
