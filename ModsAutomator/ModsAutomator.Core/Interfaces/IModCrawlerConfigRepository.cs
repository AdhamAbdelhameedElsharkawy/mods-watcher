using ModsAutomator.Core.Entities;
using System.Data;


namespace ModsAutomator.Core.Interfaces
{
    public interface IModCrawlerConfigRepository : IRepository<ModCrawlerConfig, int>
    {
        Task<ModCrawlerConfig?> GetByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
