namespace ModsWatcher.Core.Enums
{
    public enum DependencyImpactAction
    {
        Cancel,
        RemoveDependent,
        DeactivateDependent,  // Only applicable when deactivating (not hard wipe)
        BreakDependency       // Only applicable for hard wipe
    }
}