using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using System.Data;

namespace ModsAutomator.Data
{
    public class ModdedAppRepository : BaseRepository, IModdedAppRepository
    {
        public ModdedAppRepository(IConnectionFactory factory) : base(factory) { }

        public Task<ModdedApp?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = "SELECT * FROM ModdedApp WHERE Id = @Id;";
                return await c.QuerySingleOrDefaultAsync<ModdedApp>(
                    new CommandDefinition(sql, new { Id = id }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<IEnumerable<ModdedApp>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = "SELECT * FROM ModdedApp;";
                return await c.QueryAsync<ModdedApp>(
                    new CommandDefinition(sql, transaction: t, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<ModdedApp?> InsertAsync(ModdedApp e, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    => ExecuteAsync(async (c, t) =>
    {
        const string sql = @"
            INSERT INTO ModdedApp (Name, Description, LatestVersion, InstalledVersion, LastUpdatedDate)
            VALUES (@Name, @Description, @LatestVersion, @InstalledVersion, @LastUpdatedDate);
            SELECT last_insert_rowid();"; // Get the new ID

        e.Id = await c.ExecuteScalarAsync<int>(new CommandDefinition(sql, e, t, cancellationToken: cancellationToken));
        return (ModdedApp?)e;
    }, true, connection, transaction);


        public Task<ModdedApp?> UpdateAsync(ModdedApp e, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
UPDATE ModdedApp SET
    Description = @Description,
    LatestVersion = @LatestVersion,
    InstalledVersion = @InstalledVersion,
    LastUpdatedDate = @LastUpdatedDate
WHERE Id = @Id;";

                await c.ExecuteAsync(new CommandDefinition(sql, e, t, cancellationToken: cancellationToken));
                return (ModdedApp?)e;
            }, true, connection, transaction);

        public Task<bool> DeleteAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = "DELETE FROM ModdedApp WHERE Id = @Id;";
                var rows = await c.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, t, cancellationToken: cancellationToken));
                return rows > 0;
            }, true, connection, transaction);

        public Task<ModdedApp?> FindByNameAsync(string name, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = "SELECT * FROM ModdedApp WHERE Name = @Name;";
                return await c.QuerySingleOrDefaultAsync<ModdedApp>(
                    new CommandDefinition(sql, new { Name = name }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);
    }

}
