using ModsAutomator.Core.Entities;
using System.Data;

namespace ModsAutomator.Core.Interfaces
{
    public interface IModRepository : IRepository<Mod, Guid>
    {
        Task<IEnumerable<Mod>> GetByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
