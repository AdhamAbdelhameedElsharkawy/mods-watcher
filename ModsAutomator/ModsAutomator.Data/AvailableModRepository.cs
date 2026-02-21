using Dapper;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Interfaces;
using System.Data;

namespace ModsWatcher.Data
{
    public class AvailableModRepository : BaseRepository, IAvailableModRepository
    {
        public AvailableModRepository(IConnectionFactory factory) : base(factory) { }

        // Centralized SQL to handle the Guid/Int ID mapping and JOIN logic
        private const string BaseSelectSql = @"
            SELECT 
                m.Id,                -- Maps to AvailableMod.Id (Guid)
                am.Id AS InternalId, -- Internal Auto-increment
                m.AppId, m.Name, m.RootSourceUrl, m.IsDeprecated, m.Description, m.IsUsed,
                m.IsWatchable, m.IsCrawlable, m.LastWatched, m.WatcherStatus, m.LastWatcherHash,
                am.AvailableVersion, am.ReleaseDate, am.SizeMB, am.DownloadUrl, 
                am.PackageType, am.PackageFilesNumber, am.SupportedAppVersions, am.LastCrawled, am.CrawledModUrl
            FROM AvailableMod am
            JOIN Mod m ON m.Id = am.ModId";

        public Task<AvailableMod?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE am.Id = @Id;";
                return await conn.QuerySingleOrDefaultAsync<AvailableMod>(
                    new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<AvailableMod>> FindByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE am.ModId = @ModId;";
                return await conn.QueryAsync<AvailableMod>(
                    new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<AvailableMod>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                return await conn.QueryAsync<AvailableMod>(
                    new CommandDefinition(BaseSelectSql, transaction: trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<AvailableMod?> InsertAsync(AvailableMod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                    INSERT INTO AvailableMod (ModId, AvailableVersion, ReleaseDate, SizeMB, DownloadUrl, PackageType, PackageFilesNumber, SupportedAppVersions, LastCrawled, CrawledModUrl)
                    VALUES (@ModId, @AvailableVersion, @ReleaseDate, @SizeMB, @DownloadUrl, @PackageType, @PackageFilesNumber, @SupportedAppVersions, @LastCrawled, @CrawledModUrl);";

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    ModId = entity.Id, // Ensure we use the Guid from entity.InternalId
                    entity.AvailableVersion,
                    entity.ReleaseDate,
                    entity.SizeMB,
                    entity.DownloadUrl,
                    PackageType = (byte)entity.PackageType,
                    entity.PackageFilesNumber,
                    entity.SupportedAppVersions,
                    entity.LastCrawled,
                    entity.CrawledModUrl
                }, trans, cancellationToken: cancellationToken));

                return (AvailableMod?)entity;
            }, true, connection, transaction);
        }

        public Task<AvailableMod?> UpdateAsync(AvailableMod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "DELETE FROM AvailableMod WHERE Id = @Id;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        // Single Mod Cleanup
        public Task<bool> DeleteByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM AvailableMod WHERE ModId = @ModId;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        // Bulk App Cleanup (Using the Subquery strategy)
        public Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM AvailableMod
                             WHERE ModId IN (SELECT Id FROM Mod WHERE AppId = @AppId);";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }
    }
}