using ModsWatcher.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ModsWatcher.Core.Interfaces
{
    public interface IAvailableModRepository : IRepository<AvailableMod, int>
    {
        Task<IEnumerable<AvailableMod>> FindByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
