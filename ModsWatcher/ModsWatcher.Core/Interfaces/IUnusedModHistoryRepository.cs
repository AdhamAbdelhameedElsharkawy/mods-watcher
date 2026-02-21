using ModsWatcher.Core.Entities;
using System.Data;

namespace ModsWatcher.Core.Interfaces
{
    public interface IUnusedModHistoryRepository : IRepository<UnusedModHistory, int>
    {
        Task<IEnumerable<UnusedModHistory>> FindByModdedAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
