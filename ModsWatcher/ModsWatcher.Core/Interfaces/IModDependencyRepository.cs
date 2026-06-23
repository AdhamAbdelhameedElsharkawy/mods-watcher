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
    }
}