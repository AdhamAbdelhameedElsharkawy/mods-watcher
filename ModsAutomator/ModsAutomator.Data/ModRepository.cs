using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
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
                // 1. Fetch the AppId as an int
                const string fetchSql = "SELECT AppId FROM Mod WHERE Id = @id";
                var appId = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(fetchSql, new { id }, trans, cancellationToken: cancellationToken));

                // If the mod doesn't exist, nothing to delete
                if (appId == null) return false;

                // 2. Perform both operations inside the same transaction
                const string sql = @"
            DELETE FROM Mod WHERE Id = @id;

            UPDATE Mod 
            SET PriorityOrder = (
                SELECT COUNT(*) 
                FROM Mod m2 
                WHERE m2.AppId = Mod.AppId 
                  AND m2.PriorityOrder < Mod.PriorityOrder
            )
            WHERE AppId = @appId;";

                var affected = await conn.ExecuteAsync(
                    new CommandDefinition(sql, new { id, appId }, trans, cancellationToken: cancellationToken));

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

        public Task<(int ActiveCount, int PotentialUpdatesCount)> GetWatcherSummaryStatsAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
            SELECT 
                COUNT(CASE WHEN IsUsed = 1 THEN 1 END) as ActiveCount,
                COUNT(CASE 
                        WHEN IsUsed = 1 
                        AND IsWatchable = 1 
                        AND WatcherStatus = @UpdateStatus 
                        THEN 1 END) as PotentialUpdatesCount
            FROM Mod
            WHERE AppId = @AppId";

                var result = await conn.QuerySingleAsync(sql, new
                {
                    AppId = appId,
                    UpdateStatus = (int)WatcherStatusType.UpdateFound
                }, trans);

                return ((int)result.ActiveCount, (int)result.PotentialUpdatesCount);
            }, false, connection, transaction); // 'false' means no transaction required
        }

        public Task<Mod?> InsertAsync(Mod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
        INSERT INTO Mod
        (Id, AppId, Name, Author, RootSourceUrl, IsDeprecated, Description, IsUsed, IsWatchable, IsCrawlable, LastWatched, WatcherStatus, LastWatcherHash, PriorityOrder)
        VALUES
        (@Id, @AppId, @Name, @Author, @RootSourceUrl, @IsDeprecated, @Description, @IsUsed, @IsWatchable, @IsCrawlable, @LastWatched, @WatcherStatus, @LastWatcherHash, 
            (SELECT IFNULL(MAX(PriorityOrder), -1) + 1 FROM Mod WHERE AppId = @AppId)
        );";

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
                        WatcherStatus = (byte)entity.WatcherStatus,
                        entity.LastWatcherHash
                        // entity.PriorityOrder is removed here because the SQL subquery handles it
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
                    Name = @Name,
                    Author = @Author,
                    RootSourceUrl = @RootSourceUrl,
                    IsDeprecated = @IsDeprecated,
                    Description = @Description,
                    IsUsed = @IsUsed,
                    IsWatchable = @IsWatchable,
                    IsCrawlable = @IsCrawlable,
                    LastWatched = @LastWatched,
                    WatcherStatus = @WatcherStatus,
                    LastWatcherHash = @LastWatcherHash,
                    PriorityOrder = @PriorityOrder
                WHERE Id = @Id;";

                await conn.ExecuteAsync(
                    new CommandDefinition(sql, new
                    {
                        entity.Id,
                        entity.Name,
                        entity.Author,
                        entity.RootSourceUrl,
                        entity.IsDeprecated,
                        entity.Description,
                        entity.IsUsed,
                        entity.IsWatchable,
                        entity.IsCrawlable,
                        entity.LastWatched,
                        WatcherStatus = (byte)entity.WatcherStatus,
                        entity.LastWatcherHash,
                        entity.PriorityOrder

                    }, trans, cancellationToken: cancellationToken));

                return (Mod?)entity;
            }, true, connection, transaction);
        }

        public Task SaveModWithConfigAsync(Mod mod, ModCrawlerConfig config, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                // 1. Insert Mod
                const string modSql = @"
        INSERT INTO Mod
        (Id, AppId, Name, Author, RootSourceUrl, IsDeprecated, Description, IsUsed, IsWatchable, IsCrawlable, LastWatched, WatcherStatus, LastWatcherHash, PriorityOrder)
        VALUES
        (@Id, @AppId, @Name, @Author, @RootSourceUrl, @IsDeprecated, @Description, @IsUsed, @IsWatchable, @IsCrawlable, @LastWatched, @WatcherStatus, @LastWatcherHash, 
            (SELECT IFNULL(MAX(PriorityOrder), -1) + 1 FROM Mod WHERE AppId = @AppId)
        );";

                await conn.ExecuteAsync(new CommandDefinition(modSql, mod, trans, cancellationToken: cancellationToken));

                // 2. Insert Config
                const string configSql = @"
            INSERT INTO ModCrawlerConfig 
            (ModId, WatcherXPath, ModNameRegex, VersionXPath, ReleaseDateXPath, SizeXPath, DownloadUrlXPath, SupportedAppVersionsXPath, PackageFilesNumberXPath)
            VALUES 
            (@ModId, @WatcherXPath, @ModNameRegex, @VersionXPath, @ReleaseDateXPath, @SizeXPath, @DownloadUrlXPath, @SupportedAppVersionsXPath, @PackageFilesNumberXPath);";

                await conn.ExecuteAsync(new CommandDefinition(configSql, config, trans, cancellationToken: cancellationToken));

                return true;
            }, true, connection, transaction);
        }

        public Task UpdateModWithConfigAsync(Mod mod, ModCrawlerConfig config, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                // 1. Update Mod
                const string modSql = @"
            UPDATE Mod SET
                RootSourceUrl = @RootSourceUrl,
                Description = @Description,
                IsUsed = @IsUsed,
                IsWatchable = @IsWatchable,
                IsCrawlable = @IsCrawlable,
                LastWatched = @LastWatched,
                WatcherStatus = @WatcherStatus,
                LastWatcherHash = @LastWatcherHash,
                PriorityOrder = @PriorityOrder
            WHERE Id = @Id;";

                await conn.ExecuteAsync(new CommandDefinition(modSql, mod, trans, cancellationToken: cancellationToken));

                // 2. Update Config
                const string configSql = @"
            UPDATE ModCrawlerConfig SET
                WatcherXPath = @WatcherXPath,
                ModNameRegex = @ModNameRegex,
                VersionXPath = @VersionXPath,
                ReleaseDateXPath = @ReleaseDateXPath,
                SizeXPath = @SizeXPath,
                DownloadUrlXPath = @DownloadUrlXPath,
                SupportedAppVersionsXPath = @SupportedAppVersionsXPath,
                PackageFilesNumberXPath = @PackageFilesNumberXPath
            WHERE ModId = @ModId;";

                await conn.ExecuteAsync(new CommandDefinition(configSql, config, trans, cancellationToken: cancellationToken));

                return true;
            }, true, connection, transaction);
        }

        public Task<IEnumerable<Mod>> GetWatchableModsByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"SELECT * FROM Mod WHERE AppId = @AppId AND IsUsed = 1 AND IsWatchable = 1;";

                return await conn.QueryAsync<Mod>(
                    new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));

            }, false, connection, transaction); // Set to false: Read operations should not force a new transaction
        }
    }
}

