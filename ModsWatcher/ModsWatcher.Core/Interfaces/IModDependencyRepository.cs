using ModsWatcher.Core.Entities;
using System.Data;

namespace ModsWatcher.Core.Interfaces
{
    public interface IModDependencyRepository
    {
        Task AddAsync(ModDependency dependency, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid dependentModId, Guid parentModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ModDependency>> GetDependentsAsync(Guid parentModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ModDependency>> GetParentsAsync(Guid dependentModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ModDependency>> GetAllAncestorsAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ModDependency>> GetAllDescendantsAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ModDependency>> GetAllByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        // Deletes every relation touching modId, on either side (as dependent or as parent).
        // Foreign key ON DELETE CASCADE is not relied on — SQLite FK enforcement isn't enabled
        // for this app's connections — so hard-wipe flows must call this explicitly.
        Task<bool> DeleteAllForModAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteAllByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}