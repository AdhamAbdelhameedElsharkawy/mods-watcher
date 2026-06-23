using Dapper;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Interfaces;
using System.Data;

namespace ModsWatcher.Data
{
    public class ModDependencyRepository : BaseRepository, IModDependencyRepository
    {
        public ModDependencyRepository(IConnectionFactory factory) : base(factory) { }

        public Task AddAsync(ModDependency dependency, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    INSERT INTO ModDependency (DependentModId, ParentModId)
                    VALUES (@DependentModId, @ParentModId);";

                await c.ExecuteAsync(new CommandDefinition(sql, dependency, t, cancellationToken: cancellationToken));
                return true;
            }, true, connection, transaction);

        public Task DeleteAsync(Guid dependentModId, Guid parentModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    DELETE FROM ModDependency
                    WHERE DependentModId = @DependentModId AND ParentModId = @ParentModId;";

                await c.ExecuteAsync(new CommandDefinition(sql, new { DependentModId = dependentModId, ParentModId = parentModId }, t, cancellationToken: cancellationToken));
                return true;
            }, true, connection, transaction);

        public Task<IEnumerable<ModDependency>> GetDependentsAsync(Guid parentModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    SELECT DependentModId, ParentModId
                    FROM ModDependency
                    WHERE ParentModId = @ParentModId;";

                return await c.QueryAsync<ModDependency>(
                    new CommandDefinition(sql, new { ParentModId = parentModId }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<IEnumerable<ModDependency>> GetParentsAsync(Guid dependentModId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    SELECT DependentModId, ParentModId
                    FROM ModDependency
                    WHERE DependentModId = @DependentModId;";

                return await c.QueryAsync<ModDependency>(
                    new CommandDefinition(sql, new { DependentModId = dependentModId }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<IEnumerable<ModDependency>> GetAllAncestorsAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    WITH RECURSIVE AncestorChain AS (
                        SELECT DependentModId, ParentModId
                        FROM ModDependency
                        WHERE DependentModId = @ModId

                        UNION ALL

                        SELECT md.DependentModId, md.ParentModId
                        FROM ModDependency md
                        INNER JOIN AncestorChain ac ON md.DependentModId = ac.ParentModId
                    )
                    SELECT DependentModId, ParentModId FROM AncestorChain;";

                return await c.QueryAsync<ModDependency>(
                    new CommandDefinition(sql, new { ModId = modId }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);

        public Task<IEnumerable<ModDependency>> GetAllDescendantsAsync(Guid modId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(async (c, t) =>
            {
                const string sql = @"
                    WITH RECURSIVE DescendantChain AS (
                        SELECT DependentModId, ParentModId
                        FROM ModDependency
                        WHERE ParentModId = @ModId

                        UNION ALL

                        SELECT md.DependentModId, md.ParentModId
                        FROM ModDependency md
                        INNER JOIN DescendantChain dc ON md.ParentModId = dc.DependentModId
                    )
                    SELECT DependentModId, ParentModId FROM DescendantChain;";

                return await c.QueryAsync<ModDependency>(
                    new CommandDefinition(sql, new { ModId = modId }, t, cancellationToken: cancellationToken));
            }, false, connection, transaction);
    }
}