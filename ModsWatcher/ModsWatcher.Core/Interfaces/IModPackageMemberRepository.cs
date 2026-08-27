using ModsWatcher.Core.Entities;
using System.Data;

namespace ModsWatcher.Core.Interfaces
{
    public interface IModPackageMemberRepository : IRepository<ModPackageMember, int>
    {
        Task<IEnumerable<ModPackageMember>> FindByMainModIdAsync(Guid mainModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByMainModIdAsync(Guid mainModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        // All distinct MainModId values that currently have at least one member — used to
        // batch-derive which mods in an app are packages, mirroring the alternatives badge lookup.
        Task<IEnumerable<Guid>> GetMainModIdsByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
