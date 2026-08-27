using Dapper;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Interfaces;
using System.Data;

namespace ModsWatcher.Data
{
    public class ModAlternativeRepository : BaseRepository, IModAlternativeRepository
    {
        public ModAlternativeRepository(IConnectionFactory factory) : base(factory) { }

        public Task AddAsync(ModAlternative alternative, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    INSERT INTO ModAlternative (ModId, AlternativeModId)
                    VALUES (@ModId, @AlternativeModId);";

                await c.ExecuteAsync(new CommandDefinition(sql, alternative, t, cancellationToken: cancellationToken));
                return true;
            }, true, connection, transaction);

        public Task DeleteAsync(Guid modId, Guid alternativeModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    DELETE FROM ModAlternative
                    WHERE (ModId = @ModId AND AlternativeModId = @AlternativeModId)
                       OR (ModId = @AlternativeModId AND AlternativeModId = @ModId);";

                await c.ExecuteAsync(new CommandDefinition(sql, new { ModId = modId, AlternativeModId = alternativeModId }, t, cancellationToken: cancellationToken));
                return true;
            }, true, connection, transaction);

        public Task<IEnumerable<ModAlternative>> GetDirectAlternativesAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    SELECT ModId, AlternativeModId
                    FROM ModAlternative
                    WHERE ModId = @ModId OR AlternativeModId = @ModId;";

                return await c.QueryAsync<ModAlternative>(
                    new CommandDefinition(sql, new { ModId = modId }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<IEnumerable<ModAlternative>> GetAllByAppIdAsync(int appId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    SELECT ma.ModId, ma.AlternativeModId
                    FROM ModAlternative ma
                    INNER JOIN Mod m ON m.Id = ma.ModId
                    WHERE m.AppId = @AppId;";

                return await c.QueryAsync<ModAlternative>(
                    new CommandDefinition(sql, new { AppId = appId }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);
    }
}
