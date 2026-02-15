using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data;
using ModsAutomator.Data.Interfaces;
using System.Data;

public class ModCrawlerConfigRepository : BaseRepository, IModCrawlerConfigRepository
{
    public ModCrawlerConfigRepository(IConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<ModCrawlerConfig?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"SELECT * FROM ModCrawlerConfig WHERE Id = @Id;";
            return await conn.QuerySingleOrDefaultAsync<ModCrawlerConfig>(
                new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
        }, false, connection, transaction);
    }

    public Task<IEnumerable<ModCrawlerConfig>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"SELECT * FROM ModCrawlerConfig;";
            return await conn.QueryAsync<ModCrawlerConfig>(
                new CommandDefinition(sql, trans, cancellationToken: cancellationToken));
        }, false, connection, transaction);
    }

    public Task<ModCrawlerConfig?> InsertAsync(ModCrawlerConfig entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"
                INSERT INTO ModCrawlerConfig 
                (ModId, WatcherXPath, ModNameRegex, VersionXPath, ReleaseDateXPath, SizeXPath, DownloadUrlXPath, SupportedAppVersionsXPath, PackageFilesNumberXPath)
                VALUES (@ModId, @WatcherXPath, @ModNameRegex, @VersionXPath, @ReleaseDateXPath, @SizeXPath, @DownloadUrlXPath, @SupportedAppVersionsXPath, @PackageFilesNumberXPath);
                SELECT last_insert_rowid();";

            var newId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, entity, trans, cancellationToken: cancellationToken));
            entity.Id = newId;
            return (ModCrawlerConfig?)entity;
        }, true, connection, transaction);
    }

    public Task<ModCrawlerConfig?> UpdateAsync(ModCrawlerConfig entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"
                UPDATE ModCrawlerConfig SET
                    WatcherXPath = @WatcherXPath,
                    ModNameRegex = @ModNameRegex,
                    VersionXPath = @VersionXPath,
                    ReleaseDateXPath = @ReleaseDateXPath,
                    SizeXPath = @SizeXPath,
                    DownloadUrlXPath = @DownloadUrlXPath,
                    SupportedAppVersionsXPath = @SupportedAppVersionsXPath,
                    PackageFilesNumberXPath = @PackageFilesNumberXPath
                WHERE Id = @Id;";

            await conn.ExecuteAsync(new CommandDefinition(sql, entity, trans, cancellationToken: cancellationToken));
            return (ModCrawlerConfig?)entity;
        }, true, connection, transaction);
    }

    public Task<bool> DeleteAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"DELETE FROM ModCrawlerConfig WHERE Id = @Id;";
            var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            return affected > 0;
        }, true, connection, transaction);
    }

    public Task<ModCrawlerConfig?> GetByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"SELECT * FROM ModCrawlerConfig WHERE ModId = @ModId;";
            return await conn.QuerySingleOrDefaultAsync<ModCrawlerConfig>(
                new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
        }, false, connection, transaction);
    }

    public Task<bool> DeleteByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"DELETE FROM ModCrawlerConfig WHERE ModId = @ModId;";
            var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
            return affected > 0;
        }, true, connection, transaction);
    }

    public Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, trans) =>
        {
            const string sql = @"DELETE FROM ModCrawlerConfig 
                             WHERE ModId IN (SELECT Id FROM Mod WHERE AppId = @AppId);";
            var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
            return affected > 0;
        }, true, connection, transaction);
    }
}