using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using System.Data;

namespace ModsAutomator.Data
{
    public class InstalledModRepository : BaseRepository, IInstalledModRepository
    {
        private readonly IModRepository _modRepository;

        public InstalledModRepository(IConnectionFactory connectionFactory, IModRepository modRepository)
            : base(connectionFactory)
        {
            _modRepository = modRepository;
        }

        public Task<bool> DeleteAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "DELETE FROM InstalledMod WHERE Id = @Id;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);

        private const string BaseSelectSql = @"
            SELECT 
                m.Id,                -- Maps to Entity.Id (Guid)
                im.Id AS InternalId, -- Alias the auto-increment ID
                m.AppId, m.Name, m.RootSourceUrl, m.IsDeprecated, m.Description, m.IsUsed,
                im.InstalledVersion, im.InstalledDate, im.InstalledSizeMB,
                im.PackageType, im.PackageFilesNumber, im.SupportedAppVersions, im.PriorityOrder
            FROM InstalledMod im
            JOIN Mod m ON m.Id = im.ModId";

        public Task<InstalledMod?> FindByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE im.ModId = @ModId;";
                return await conn.QuerySingleOrDefaultAsync<InstalledMod>(
                    new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<InstalledMod?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (conn, trans) =>
            {
                string sql = $"{BaseSelectSql} WHERE im.Id = @Id;";
                return await conn.QuerySingleOrDefaultAsync<InstalledMod>(
                    new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<IEnumerable<InstalledMod>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (conn, trans) =>
            {
                return await conn.QueryAsync<InstalledMod>(
                    new CommandDefinition(BaseSelectSql, transaction: trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<InstalledMod?> InsertAsync(InstalledMod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (conn, trans) =>
            {
                var existingMod = await _modRepository.GetByIdAsync(entity.Id, conn, trans, cancellationToken);
                if (existingMod == null)
                {
                    await _modRepository.InsertAsync(entity, conn, trans, cancellationToken);
                }

                const string sql = @"
                    INSERT INTO InstalledMod
                    (ModId, InstalledVersion, InstalledDate, InstalledSizeMB, PackageType, PackageFilesNumber, SupportedAppVersions, PriorityOrder)
                    VALUES
                    (@ModId, @InstalledVersion, @InstalledDate, @InstalledSizeMB, @PackageType, @PackageFilesNumber, @SupportedAppVersions, @PriorityOrder);";

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    ModId = entity.Id,
                    entity.InstalledVersion,
                    entity.InstalledDate,
                    entity.InstalledSizeMB,
                    PackageType = (int)entity.PackageType,
                    entity.PackageFilesNumber,
                    entity.SupportedAppVersions,
                    entity.PriorityOrder
                }, trans, cancellationToken: cancellationToken));

                return (InstalledMod?)entity;
            }, true, connection, transaction);

        public Task<InstalledMod?> UpdateAsync(InstalledMod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
                    UPDATE InstalledMod SET
                        InstalledVersion = @InstalledVersion,
                        InstalledDate = @InstalledDate,
                        InstalledSizeMB = @InstalledSizeMB,
                        PackageType = @PackageType,
                        PackageFilesNumber = @PackageFilesNumber,
                        SupportedAppVersions = @SupportedAppVersions,
                        PriorityOrder = @PriorityOrder
                    WHERE ModId = @ModId;";

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    ModId = entity.Id,
                    entity.InstalledVersion,
                    entity.InstalledDate,
                    entity.InstalledSizeMB,
                    PackageType = (int)entity.PackageType,
                    entity.PackageFilesNumber,
                    entity.SupportedAppVersions,
                    entity.PriorityOrder
                }, trans, cancellationToken: cancellationToken));

                return (InstalledMod?)entity;
            }, true, connection, transaction);

        public Task<(int ActiveCount, decimal TotalSize, int IncompatibleCount)> GetAppSummaryStatsAsync(int appId, string targetVersion, IDbConnection? connection = null)

        {

            // Explicitly type the lambda return to match the Task signature

            return ExecuteAsync<(int, decimal, int)>(async (conn, trans) =>

            {

                var wrappedVersion = $",{targetVersion},";



                const string sql = @"

            SELECT 

                COUNT(*) as ActiveCount, 

                IFNULL(SUM(InstalledSizeMB), 0) as TotalSize,

                SUM(CASE WHEN ',' || SupportedAppVersions || ',' NOT LIKE '%' || @Target || '%' THEN 1 ELSE 0 END) as IncompatibleCount

            FROM InstalledMod im

            JOIN Mod m ON m.Id = im.ModId

            WHERE m.AppId = @AppId AND m.IsUsed = 1";



                var result = await conn.QuerySingleAsync(sql, new { AppId = appId, Target = wrappedVersion }, trans);



                return (

                    (int)(result.ActiveCount ?? 0),

                    Convert.ToDecimal(result.TotalSize ?? 0),

                    (int)(result.IncompatibleCount ?? 0)

                );

            }, false, connection);

        }

        // Single Mod Cleanup
        public Task<bool> DeleteByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM InstalledMod WHERE ModId = @ModId;";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }

        // Bulk App Cleanup (Using the Subquery strategy)
        public Task<bool> DeleteByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"DELETE FROM InstalledMod 
                             WHERE ModId IN (SELECT Id FROM Mod WHERE AppId = @AppId);";
                var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { AppId = appId }, trans, cancellationToken: cancellationToken));
                return affected > 0;
            }, true, connection, transaction);
        }
    }
}