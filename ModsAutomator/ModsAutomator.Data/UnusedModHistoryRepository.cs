using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using System.Data;

namespace ModsAutomator.Data
{
    public class UnusedModHistoryRepository : BaseRepository, IUnusedModHistoryRepository
    {
        public UnusedModHistoryRepository(IConnectionFactory factory) : base(factory) { }

        public Task<UnusedModHistory?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM UnusedModHistory WHERE Id = @Id;";
                return await conn.QuerySingleOrDefaultAsync<UnusedModHistory>(
                    new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<UnusedModHistory>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM UnusedModHistory;";
                return await conn.QueryAsync<UnusedModHistory>(
                    new CommandDefinition(sql, transaction: trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<UnusedModHistory?> InsertAsync(UnusedModHistory entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
            INSERT INTO UnusedModHistory (ModId, ModdedAppId, Name, AppName, AppVersion, RemovedAt, Reason, Description, RootSourceUrl, WatcherXPath, ModNameRegex, VersionXPath, ReleaseDateXPath, SizeXPath, DownloadUrlXPath, SupportedAppVersionsXPath, PackageFilesNumberXPath,  Author)
            VALUES (@ModId, @ModdedAppId, @Name, @AppName, @AppVersion, @RemovedAt, @Reason, @Description, @RootSourceUrl, @WatcherXPath, @ModNameRegex, @VersionXPath, @ReleaseDateXPath, @SizeXPath, @DownloadUrlXPath, @SupportedAppVersionsXPath, @PackageFilesNumberXPath, @Author);
            SELECT last_insert_rowid();"; // Added this

                var newId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
                {
                    entity.ModId,
                    entity.ModdedAppId,
                    entity.Name,
                    entity.AppName,
                    entity.AppVersion,
                    entity.RemovedAt,
                    entity.Reason,
                    entity.Description,
                    entity.RootSourceUrl,
                    entity.WatcherXPath,
                    entity.ModNameRegex,
                    entity.VersionXPath,
                    entity.ReleaseDateXPath,
                    entity.SizeXPath,
                    entity.DownloadUrlXPath,
                    entity.SupportedAppVersionsXPath,
                    entity.PackageFilesNumberXPath,
                    entity.Author
                }, trans, cancellationToken: cancellationToken));

                entity.Id = newId; // Populate the ID back to the object
                return (UnusedModHistory?)entity;
            }, true, connection, transaction);
        }

        public Task<UnusedModHistory?> UpdateAsync(UnusedModHistory entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
          throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "DELETE FROM UnusedModHistory WHERE Id = @Id;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        public Task<IEnumerable<UnusedModHistory>> FindByModdedAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM UnusedModHistory WHERE ModdedAppId = @AppId;";
                return await conn.QueryAsync<UnusedModHistory>(
                    new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM UnusedModHistory WHERE ModdedAppId = @AppId;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }
    }

}
