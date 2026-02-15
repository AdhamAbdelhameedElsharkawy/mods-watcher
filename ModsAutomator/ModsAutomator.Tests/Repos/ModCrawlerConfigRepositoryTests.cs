using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Data;
using Xunit;

namespace ModsAutomator.Tests.Repos
{
    public class ModCrawlerConfigRepositoryTests : BaseRepositoryTest
    {
        private readonly ModCrawlerConfigRepository _repo;

        public ModCrawlerConfigRepositoryTests()
        {
            _repo = new ModCrawlerConfigRepository(FactoryMock.Object);
        }

        private async Task<(int AppId, Guid ModId)> SeedParentHierarchyAsync()
        {
            const string appSql = "INSERT INTO ModdedApp (Name) VALUES ('Test App'); SELECT last_insert_rowid();";
            int appId = await Connection.QuerySingleAsync<int>(appSql);

            Guid modId = Guid.NewGuid();
            const string modSql = "INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES (@Id, @AppId, 'Test Mod', 1, 0)";
            await Connection.ExecuteAsync(modSql, new { Id = modId, AppId = appId });

            return (appId, modId);
        }

        [Fact]
        public async Task InsertAsync_ShouldSaveConfig()
        {
            // Arrange
            var (_, modId) = await SeedParentHierarchyAsync();
            var config = new ModCrawlerConfig
            {
                ModId = modId,
                WatcherXPath = "//div[@id='version']",
                ModNameRegex = "//a[@class='download-link']",
                VersionXPath = "//span/text()"
            };

            // Act
            var result = await _repo.InsertAsync(config, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);

            var saved = await Connection.QuerySingleOrDefaultAsync<ModCrawlerConfig>(
                "SELECT * FROM ModCrawlerConfig WHERE Id = @Id", new { Id = result.Id });

            Assert.NotNull(saved);
            Assert.Equal(modId, saved.ModId);
            Assert.Equal("//div[@id='version']", saved.WatcherXPath);
        }

        [Fact]
        public async Task GetByModIdAsync_ShouldReturnCorrectConfig()
        {
            // Arrange
            var (_, modId) = await SeedParentHierarchyAsync();
            await Connection.ExecuteAsync(@"
                INSERT INTO ModCrawlerConfig (ModId, WatcherXPath, ModNameRegex) 
                VALUES (@ModId, '//path1', '//path2')", new { ModId = modId });

            // Act
            var result = await _repo.GetByModIdAsync(modId, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("//path1", result.WatcherXPath);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyPaths()
        {
            // Arrange
            var (_, modId) = await SeedParentHierarchyAsync();
            const string insertSql = "INSERT INTO ModCrawlerConfig (ModId, WatcherXPath) VALUES (@ModId, 'old'); SELECT last_insert_rowid();";
            int id = await Connection.QuerySingleAsync<int>(insertSql, new { ModId = modId });

            var updateEntity = new ModCrawlerConfig
            {
                Id = id,
                WatcherXPath = "new_watcher",
                ModNameRegex = "new_links"
            };

            // Act
            await _repo.UpdateAsync(updateEntity, Connection);

            // Assert
            var result = await Connection.QuerySingleAsync<ModCrawlerConfig>(
                "SELECT * FROM ModCrawlerConfig WHERE Id = @Id", new { Id = id });
            Assert.Equal("new_watcher", result.WatcherXPath);
            Assert.Equal("new_links", result.ModNameRegex);
        }

        [Fact]
        public async Task DeleteByModIdAsync_ShouldRemoveConfig()
        {
            // Arrange
            var (_, modId) = await SeedParentHierarchyAsync();
            await Connection.ExecuteAsync("INSERT INTO ModCrawlerConfig (ModId) VALUES (@ModId)", new { ModId = modId });

            // Act
            var result = await _repo.DeleteByModIdAsync(modId, Connection);

            // Assert
            Assert.True(result);
            var exists = await Connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM ModCrawlerConfig WHERE ModId = @ModId", new { ModId = modId });
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteByAppIdAsync_ShouldWipeAllConfigsForApp()
        {
            // Arrange
            var (appId, modId) = await SeedParentHierarchyAsync();
            await Connection.ExecuteAsync("INSERT INTO ModCrawlerConfig (ModId) VALUES (@ModId)", new { ModId = modId });

            // Act
            var result = await _repo.DeleteByAppIdAsync(appId, Connection);

            // Assert
            Assert.True(result);
            var count = await Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM ModCrawlerConfig WHERE ModId = @ModId", new { ModId = modId });
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task QueryAllAsync_ShouldReturnEveryModConfig()
        {
            // Arrange
            var ids = await SeedParentHierarchyAsync();
            await Connection.ExecuteAsync("INSERT INTO ModCrawlerConfig (ModId) VALUES (@ModId)", new { ModId = ids.ModId });

            // Act
            var results = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.Single(results);
        }
    }
}