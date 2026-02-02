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
    public class AvailableModRepository : BaseRepository, IAvailableModRepository
    {
        public AvailableModRepository(IConnectionFactory factory) : base(factory) { }

        public Task<AvailableMod?> GetByIdAsync(int id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM AvailableMod WHERE Id = @Id;";
                return await conn.QuerySingleOrDefaultAsync<AvailableMod>(new CommandDefinition(sql, new { Id = id }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<IEnumerable<AvailableMod>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM AvailableMod;";
                return await conn.QueryAsync<AvailableMod>(new CommandDefinition(sql, transaction: trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

        public Task<AvailableMod?> InsertAsync(AvailableMod entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = @"
INSERT INTO AvailableMod (ModId, AvailableVersion, ReleaseDate, SizeMB, DownloadUrl, PackageType, PackageFilesNumber, SupportedAppVersions)
VALUES (@ModId, @AvailableVersion, @ReleaseDate, @SizeMB, @DownloadUrl, @PackageType, @PackageFilesNumber, @SupportedAppVersions);";

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    entity.Id, // ModId
                    entity.AvailableVersion,
                    entity.ReleaseDate,
                    entity.SizeMB,
                    entity.DownloadUrl,
                    PackageType = (int)entity.PackageType,
                    entity.PackageFilesNumber,
                    entity.SupportedAppVersions
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

        public Task<IEnumerable<AvailableMod>> FindByModIdAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(async (conn, trans) =>
            {
                const string sql = "SELECT * FROM AvailableMod WHERE ModId = @ModId;";
                return await conn.QueryAsync<AvailableMod>(new CommandDefinition(sql, new { ModId = modId }, trans, cancellationToken: cancellationToken));
            }, false, connection, transaction);
        }

      
    }

}
