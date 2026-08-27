using Dapper;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Interfaces;
using System.Data;

namespace ModsWatcher.Data
{
    public class ModPackageMemberRepository : BaseRepository, IModPackageMemberRepository
    {
        public ModPackageMemberRepository(IConnectionFactory factory) : base(factory) { }

        // Alias Id to InternalId so Dapper maps it correctly to your entity
        private const string BaseSelectSql = @"
            SELECT
                Id AS InternalId,
                MainModId, Name, Notes, Url, PriorityOrder
            FROM ModPackageMember";

        public Task<ModPackageMember?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE Id = @Id;";
                return await conn.QuerySingleOrDefaultAsync<ModPackageMember>(
                    new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<ModPackageMember>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                return await conn.QueryAsync<ModPackageMember>(
                    new CommandDefinition(BaseSelectSql, transaction: trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<ModPackageMember>> FindByMainModIdAsync(Guid mainModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE MainModId = @MainModId ORDER BY PriorityOrder;";
                return await conn.QueryAsync<ModPackageMember>(
                    new CommandDefinition(sql, new { MainModId = mainModId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<ModPackageMember?> InsertAsync(ModPackageMember entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                    INSERT INTO ModPackageMember (MainModId, Name, Notes, Url, PriorityOrder)
                    VALUES (@MainModId, @Name, @Notes, @Url, @PriorityOrder);
                    SELECT last_insert_rowid();";

                var internalId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
                {
                    entity.MainModId,
                    entity.Name,
                    entity.Notes,
                    entity.Url,
                    entity.PriorityOrder
                }, trans, cancellationToken: cancellationToken));

                entity.InternalId = internalId;
                return (ModPackageMember?)entity;
            }, true, connection, transaction);
        }

        public Task<ModPackageMember?> UpdateAsync(ModPackageMember entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                    UPDATE ModPackageMember
                    SET Name = @Name, Notes = @Notes, Url = @Url, PriorityOrder = @PriorityOrder
                    WHERE Id = @InternalId;";

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    entity.InternalId,
                    entity.Name,
                    entity.Notes,
                    entity.Url,
                    entity.PriorityOrder
                }, trans, cancellationToken: cancellationToken));

                return (ModPackageMember?)entity;
            }, true, connection, transaction);
        }

        public Task<bool> DeleteAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "DELETE FROM ModPackageMember WHERE Id = @Id;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        // Single Mod Cleanup
        public Task<bool> DeleteByMainModIdAsync(Guid mainModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM ModPackageMember WHERE MainModId = @MainModId;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { MainModId = mainModId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        // Bulk App Cleanup (Using the Subquery strategy)
        public Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM ModPackageMember
                             WHERE MainModId IN (SELECT Id FROM Mod WHERE AppId = @AppId);";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        public Task<IEnumerable<Guid>> GetMainModIdsByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                    SELECT DISTINCT pm.MainModId
                    FROM ModPackageMember pm
                    INNER JOIN Mod m ON m.Id = pm.MainModId
                    WHERE m.AppId = @AppId;";

                return await conn.QueryAsync<Guid>(
                    new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }
    }
}
