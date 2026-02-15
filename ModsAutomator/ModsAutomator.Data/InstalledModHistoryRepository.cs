using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using System.Data;

namespace ModsAutomator.Data
{
    public class InstalledModHistoryRepository : BaseRepository, IInstalledModHistoryRepository
    {
        public InstalledModHistoryRepository(IConnectionFactory factory) : base(factory) { }

        // Alias Id to InternalId so Dapper maps it correctly to your entity
        private const string BaseSelectSql = @"
            SELECT 
                Id AS InternalId, 
                ModId, Version, AppVersion, InstalledAt, RemovedAt, LocalFilePath, IsRollbackTarget
            FROM InstalledModHistory";

        public Task<InstalledModHistory?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE Id = @Id;";
                return await conn.QuerySingleOrDefaultAsync<InstalledModHistory>(
                    new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<InstalledModHistory>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                return await conn.QueryAsync<InstalledModHistory>(
                    new CommandDefinition(BaseSelectSql, transaction: trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<InstalledModHistory>> FindByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE ModId = @ModId;";
                return await conn.QueryAsync<InstalledModHistory>(
                    new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<InstalledModHistory?> InsertAsync(InstalledModHistory entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
            INSERT INTO InstalledModHistory
            (ModId, Version, AppVersion, InstalledAt, RemovedAt, LocalFilePath, IsRollbackTarget)
            VALUES
            (@ModId, @Version, @AppVersion, @InstalledAt, @RemovedAt, @LocalFilePath, @IsRollbackTarget);
            SELECT last_insert_rowid();"; // Capture the new ID

                var internalId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
                {
                    entity.ModId,
                    entity.Version,
                    entity.AppVersion,
                    entity.InstalledAt,
                    entity.RemovedAt,
                    entity.LocalFilePath,
                    entity.IsRollbackTarget
                }, trans, cancellationToken: cancellationToken));

                entity.InternalId = internalId; // Populate the entity
                return (InstalledModHistory?)entity;
            }, true, connection, transaction);
        }

        public Task<InstalledModHistory?> UpdateAsync(InstalledModHistory entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("History records are immutable.");
        }

        public Task<bool> DeleteAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "DELETE FROM InstalledModHistory WHERE Id = @Id;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }


        // Single Mod Cleanup
        public Task<bool> DeleteByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM InstalledModHistory WHERE ModId = @ModId;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        // Bulk App Cleanup (Using the Subquery strategy)
        public Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM InstalledModHistory 
                             WHERE ModId IN (SELECT Id FROM Mod WHERE AppId = @AppId);";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }
    }
}