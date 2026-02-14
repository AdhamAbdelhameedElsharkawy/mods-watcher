using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using System.Data;
using System.Threading;
using System.Transactions;


namespace ModsAutomator.Data
{
    public class ModRepository : BaseRepository, IModRepository
    {

        public ModRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }


        public Task<bool> DeleteAsync(Guid id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM Mod WHERE Id = @Id;";

                var affected = await conn.ExecuteAsync(
                    new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));

                return affected > 0;
            }, true, connection, transaction);
        }

        // Bulk delete all Mods for an App (Part of HardWipeApp)
        public Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM Mod WHERE AppId = @AppId;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        public Task<IEnumerable<Mod>> GetByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"SELECT * FROM Mod WHERE AppId = @AppId;";

                return await conn.QueryAsync<Mod>(
                    new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<Mod?> GetByIdAsync(Guid id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                SELECT * FROM Mod
                WHERE Id = @Id;";

                return await conn.QuerySingleOrDefaultAsync<Mod>(
                    new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<Mod?> InsertAsync(Mod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                INSERT INTO Mod
                (Id, AppId, Name, Author, RootSourceUrl, IsDeprecated, Description, IsUsed, IsWatchable, IsCrawlable, LastWatched, WatcherStatus, LastWatcherHash)
                VALUES
               (@Id, @AppId, @Name, @Author, @RootSourceUrl, @IsDeprecated, @Description, @IsUsed, @IsWatchable, @IsCrawlable, @LastWatched, @WatcherStatus, @LastWatcherHash);";

                await conn.ExecuteAsync(
                    new CommandDefinition(sql, new
                    {
                        entity.Id,
                        entity.AppId,
                        entity.Name,
                        entity.Author,
                        entity.RootSourceUrl,
                        entity.IsDeprecated,
                        entity.Description,
                        entity.IsUsed,
                        entity.IsWatchable,
                        entity.IsCrawlable,
                        entity.LastWatched,
                        entity.WatcherStatus,
                        entity.LastWatcherHash
                    }, trans, cancellationToken: cancellationToken));

                return (Mod?)entity;
            }, true, connection, transaction);
        }

        public Task<IEnumerable<Mod>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                SELECT * FROM Mod;";

                return await conn.QueryAsync<Mod>(
                    new CommandDefinition(sql, transaction: trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<Mod?> UpdateAsync(Mod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                UPDATE Mod SET
                    RootSourceUrl = @RootSourceUrl,
                    IsDeprecated = @IsDeprecated,
                    Description = @Description,
                    IsUsed = @IsUsed,
                    IsWatchable = @IsWatchable,
                    IsCrawlable = @IsCrawlable,
                    LastWatched = @LastWatched,
                    WatcherStatus = @WatcherStatus,
                    LastWatcherHash = @LastWatcherHash
                WHERE Id = @Id;";

                await conn.ExecuteAsync(
                    new CommandDefinition(sql, new
                    {
                        entity.Id,
                        entity.RootSourceUrl,
                        entity.IsDeprecated,
                        entity.Description,
                        entity.IsUsed,
                        entity.IsWatchable,
                        entity.IsCrawlable,
                        entity.LastWatched,
                        entity.WatcherStatus,
                        entity.LastWatcherHash

                    }, trans, cancellationToken: cancellationToken));

                return (Mod?)entity;
            }, true, connection, transaction);
        }
    }
}

