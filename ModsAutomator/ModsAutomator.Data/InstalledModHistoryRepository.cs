using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ModsAutomator.Data
{
    public class InstalledModHistoryRepository : BaseRepository, IInstalledModHistoryRepository
    {
        public InstalledModHistoryRepository(IConnectionFactory factory) : base(factory) { }

        public Task<InstalledModHistory?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM InstalledModHistory WHERE Id = @Id;";
                return await conn.QuerySingleOrDefaultAsync<InstalledModHistory>(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<InstalledModHistory>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM InstalledModHistory;";
                return await conn.QueryAsync<InstalledModHistory>(new CommandDefinition(sql, transaction: trans, cancellationToken: cancellationToken));
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
(@ModId, @Version, @AppVersion, @InstalledAt, @RemovedAt, @LocalFilePath, @IsRollbackTarget);";

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    entity.ModId,
                    entity.Version,
                    entity.AppVersion,
                    entity.InstalledAt,
                    entity.RemovedAt,
                    entity.LocalFilePath,
                    entity.IsRollbackTarget
                }, trans, cancellationToken: cancellationToken));

                return (InstalledModHistory?)entity;
            }, true, connection, transaction);
        }

        public Task<InstalledModHistory?> UpdateAsync(InstalledModHistory entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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

        public Task<IEnumerable<InstalledModHistory>> FindByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM InstalledModHistory WHERE ModId = @ModId;";
                return await conn.QueryAsync<InstalledModHistory>(new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }
    }

}
