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
INSERT INTO UnusedModHistory (ModId, ModdedAppId, Name, AppName, AppVersion, RemovedAt, Reason, Description, RootSourceUrl)
VALUES (@ModId, @ModdedAppId, @Name, @AppName, @AppVersion, @RemovedAt, @Reason, @Description, @RootSourceUrl);";

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    entity.ModId,
                    entity.ModdedAppId,
                    entity.Name,
                    entity.AppName,
                    entity.AppVersion,
                    entity.RemovedAt,
                    entity.Reason,
                    entity.Description,
                    entity.RootSourceUrl
                }, trans, cancellationToken: cancellationToken));

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
