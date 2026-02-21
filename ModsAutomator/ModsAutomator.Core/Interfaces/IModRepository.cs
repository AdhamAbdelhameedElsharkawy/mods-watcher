using ModsWatcher.Core.Entities;
using System.Data;

namespace ModsWatcher.Core.Interfaces
{
    public interface IModRepository : IRepository<Mod, Guid>
    {
        Task<IEnumerable<Mod>> GetByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<(int ActiveCount, int PotentialUpdatesCount)> GetWatcherSummaryStatsAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        Task UpdateModWithConfigAsync(Mod mod, ModCrawlerConfig config, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task SaveModWithConfigAsync(Mod mod, ModCrawlerConfig config, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<Mod>> GetWatchableModsByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
