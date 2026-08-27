using ModsWatcher.Core.Entities;
using System.Data;

namespace ModsWatcher.Core.Interfaces
{
    public interface IModAlternativeRepository
    {
        Task AddAsync(ModAlternative alternative, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid modId, Guid alternativeModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ModAlternative>> GetDirectAlternativesAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ModAlternative>> GetAllByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
