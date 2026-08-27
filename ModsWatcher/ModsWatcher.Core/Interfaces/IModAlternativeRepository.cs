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

        // Deletes every relation touching modId, on either side. Foreign key ON DELETE
        // CASCADE is not relied on — SQLite FK enforcement isn't enabled for this app's
        // connections — so hard-wipe flows must call this explicitly.
        Task<bool> DeleteAllForModAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteAllByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
