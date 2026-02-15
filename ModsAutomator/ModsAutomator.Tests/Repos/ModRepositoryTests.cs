using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Data;
using Xunit;

namespace ModsAutomator.Tests.Repos
{
    public class ModRepositoryTests : BaseRepositoryTest
    {
        private readonly ModRepository _repo;

        public ModRepositoryTests()
        {
            _repo = new ModRepository(FactoryMock.Object);
        }

        private async Task<int> SeedParentAppAsync()
        {
            const string sql = "INSERT INTO ModdedApp (Name) VALUES ('Test App'); SELECT last_insert_rowid();";
            return await Connection.QuerySingleAsync<int>(sql);
        }

        [Fact]
        public async Task InsertAsync_ShouldAssignIncrementalPriority()
        {
            int appId = await SeedParentAppAsync();
            var mod1 = new Mod { Id = Guid.NewGuid(), AppId = appId, Name = "First" };
            var mod2 = new Mod { Id = Guid.NewGuid(), AppId = appId, Name = "Second" };

            await _repo.InsertAsync(mod1, Connection);
            await _repo.InsertAsync(mod2, Connection);

            var result1 = await Connection.QuerySingleAsync<Mod>("SELECT PriorityOrder FROM Mod WHERE Id = @Id", new { mod1.Id });
            var result2 = await Connection.QuerySingleAsync<Mod>("SELECT PriorityOrder FROM Mod WHERE Id = @Id", new { mod2.Id });

            Assert.Equal(0, result1.PriorityOrder);
            Assert.Equal(1, result2.PriorityOrder);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReorderRemainingMods()
        {
            int appId = await SeedParentAppAsync();
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            for (int i = 0; i < 3; i++)
            {
                await Connection.ExecuteAsync(
                    "INSERT INTO Mod (Id, AppId, Name, PriorityOrder, IsUsed, IsDeprecated) VALUES (@Id, @AppId, @Name, @P, 1, 0)",
                    new { Id = ids[i], AppId = appId, Name = $"M{i}", P = i });
            }

            await _repo.DeleteAsync(ids[1], Connection);

            var p0 = await Connection.QuerySingleAsync<int>("SELECT PriorityOrder FROM Mod WHERE Id = @Id", new { Id = ids[0] });
            var p2 = await Connection.QuerySingleAsync<int>("SELECT PriorityOrder FROM Mod WHERE Id = @Id", new { Id = ids[2] });

            Assert.Equal(0, p0);
            Assert.Equal(1, p2);
        }

        [Fact]
        public async Task SaveModWithConfigAsync_ShouldBeAtomic()
        {
            int appId = await SeedParentAppAsync();
            var mod = new Mod { Id = Guid.NewGuid(), AppId = appId, Name = "AtomicMod" };
            var config = new ModCrawlerConfig { ModId = mod.Id, VersionXPath = "//test" };

            await _repo.SaveModWithConfigAsync(mod, config, Connection);

            var modCount = await Connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Mod WHERE Id = @Id", new { mod.Id });
            var configCount = await Connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM ModCrawlerConfig WHERE ModId = @Id", new { Id = mod.Id });

            Assert.Equal(1, modCount);
            Assert.Equal(1, configCount);
        }

        [Fact]
        public async Task GetWatcherSummaryStatsAsync_ShouldReturnCorrectCounts()
        {
            // Note: This test will reveal the "FROM Mods" bug in your current repo code
            int appId = await SeedParentAppAsync();
            await Connection.ExecuteAsync(@"
                INSERT INTO Mod (Id, AppId, Name, IsUsed, IsWatchable, WatcherStatus, PriorityOrder, IsDeprecated) 
                VALUES 
                (@g1, @appId, 'M1', 1, 1, @status, 0, 0),
                (@g2, @appId, 'M2', 1, 0, @status, 1, 0),
                (@g3, @appId, 'M3', 0, 1, @status, 2, 0)",
                new { appId, g1 = Guid.NewGuid(), g2 = Guid.NewGuid(), g3 = Guid.NewGuid(), status = (int)WatcherStatusType.UpdateFound });

            var stats = await _repo.GetWatcherSummaryStatsAsync(appId, Connection);

            Assert.Equal(2, stats.ActiveCount); // M1 and M2 (IsUsed = 1)
            Assert.Equal(1, stats.PotentialUpdatesCount); // Only M1 (Used + Watchable + UpdateFound)
        }

        [Fact]
        public async Task UpdateModWithConfigAsync_ShouldUpdateBothTables()
        {
            int appId = await SeedParentAppAsync();
            var modId = Guid.NewGuid();
            await Connection.ExecuteAsync("INSERT INTO Mod (Id, AppId, Name, PriorityOrder, IsUsed, IsDeprecated) VALUES (@Id, @AppId, 'N', 0, 1, 0)", new { Id = modId, AppId = appId });
            await Connection.ExecuteAsync("INSERT INTO ModCrawlerConfig (ModId, VersionXPath) VALUES (@Id, 'old')", new { Id = modId });

            var mod = new Mod { Id = modId, Description = "UpdatedDesc" };
            var config = new ModCrawlerConfig { ModId = modId, VersionXPath = "new" };

            await _repo.UpdateModWithConfigAsync(mod, config, Connection);

            var desc = await Connection.ExecuteScalarAsync<string>("SELECT Description FROM Mod WHERE Id = @Id", new { Id = modId });
            var xpath = await Connection.ExecuteScalarAsync<string>("SELECT VersionXPath FROM ModCrawlerConfig WHERE ModId = @Id", new { Id = modId });

            Assert.Equal("UpdatedDesc", desc);
            Assert.Equal("new", xpath);
        }

        [Fact]
        public async Task GetWatchableModsByAppIdAsync_ShouldFilterCorrectly()
        {
            int appId = await SeedParentAppAsync();
            await Connection.ExecuteAsync(@"
                INSERT INTO Mod (Id, AppId, Name, IsUsed, IsWatchable, PriorityOrder, IsDeprecated) 
                VALUES (@g1, @appId, 'Yes', 1, 1, 0, 0), (@g2, @appId, 'No', 1, 0, 1, 0)",
                new { appId, g1 = Guid.NewGuid(), g2 = Guid.NewGuid() });

            var results = await _repo.GetWatchableModsByAppIdAsync(appId, Connection);

            Assert.Single(results);
            Assert.Equal("Yes", results.First().Name);
        }
    }
}