using ModsWatcher.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ModsWatcher.Core.Interfaces
{
    public interface IModdedAppRepository : IRepository<ModdedApp, int>
    {
        Task<ModdedApp?> FindByNameAsync(string name, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
