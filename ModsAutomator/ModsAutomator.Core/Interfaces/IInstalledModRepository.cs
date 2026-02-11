using ModsAutomator.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ModsAutomator.Core.Interfaces
{
    public interface IInstalledModRepository : IRepository<InstalledMod, int>
    {

        Task<InstalledMod?> FindByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        public Task<(int ActiveCount, decimal TotalSize, int IncompatibleCount)> GetAppSummaryStatsAsync(int appId, string targetVersion, IDbConnection? connection = null);

        Task<bool> DeleteByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
