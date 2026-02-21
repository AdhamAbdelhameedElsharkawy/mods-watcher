using Dapper;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Data;
using ModsWatcher.Tests.Repos;
using Xunit;

namespace ModsWatcher.Tests.Repos
{
    public class AvailableModRepositoryTests : BaseRepositoryTest
    {
        private readonly AvailableModRepository _repo;

        public AvailableModRepositoryTests()
        {
            _repo = new AvailableModRepository(FactoryMock.Object);
        }

        private async Task<(int AppId, Guid ModId)> SeedDatabaseAsync()
        {
            const string appSql = "INSERT INTO ModdedApp (Name) VALUES ('Witcher 3'); SELECT last_insert_rowid();";
            int appId = await Connection.QuerySingleAsync<int>(appSql);

            Guid modId = Guid.NewGuid();
            const string modSql = "INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES (@Id, @AppId, 'Fast Travel', 1, 0)";
            await Connection.ExecuteAsync(modSql, new { Id = modId, AppId = appId });

            return (appId, modId);
        }

        [Fact]
        public async Task InsertAsync_ShouldSaveAvailableMod_AndMapGuidCorrectly()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            var availableMod = new AvailableMod
            {
                Id = ids.ModId,
                AvailableVersion = "1.0.2",
                SizeMB = 2.5m,
                DownloadUrl = "http://cdn.mods.com/file.zip",
                PackageType = PackageType.Zip
            };

            // Act
            await _repo.InsertAsync(availableMod, Connection);

            // Assert
            var results = await _repo.FindByModIdAsync(ids.ModId, Connection);
            var result = results.FirstOrDefault();

            Assert.NotNull(result);
            Assert.Equal(ids.ModId, result.Id);
            Assert.Equal("1.0.2", result.AvailableVersion);
            Assert.Equal("Fast Travel", result.Name); // Verifies JOIN logic works
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectModByInternalId()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync(
                "INSERT INTO AvailableMod (ModId, AvailableVersion) VALUES (@ModId, '2.1')",
                new { ModId = ids.ModId });
            int internalId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var result = await _repo.GetByIdAsync(internalId, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ids.ModId, result.Id);
            Assert.Equal("2.1", result.AvailableVersion);
        }

        [Fact]
        public async Task FindByModIdAsync_ShouldReturnMultipleVersionsForSameMod()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync(@"
                INSERT INTO AvailableMod (ModId, AvailableVersion) VALUES 
                (@ModId, '1.0'), 
                (@ModId, '1.1')", new { ModId = ids.ModId });

            // Act
            var results = await _repo.FindByModIdAsync(ids.ModId, Connection);

            // Assert
            Assert.Equal(2, results.Count());
            Assert.All(results, m => Assert.Equal(ids.ModId, m.Id));
        }

        [Fact]
        public async Task QueryAllAsync_ShouldReturnEverythingInDatabase()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync(
                "INSERT INTO AvailableMod (ModId, AvailableVersion) VALUES (@ModId, 'BETA')",
                new { ModId = ids.ModId });

            // Act
            var results = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, m => m.AvailableVersion == "BETA");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveAvailableVersion()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync(
                "INSERT INTO AvailableMod (ModId) VALUES (@ModId)",
                new { ModId = ids.ModId });
            int internalId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var deleted = await _repo.DeleteAsync(internalId, Connection);
            var exists = await Connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM AvailableMod WHERE Id = @Id", new { Id = internalId });

            // Assert
            Assert.True(deleted);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteByAppIdAsync_ShouldWipeDataUsingSubquery()
        {
            // Arrange
            var ids = await SeedDatabaseAsync(); // Seeds AppId and ModId
            int appId = ids.AppId;
            Guid modId = ids.ModId;

            await Connection.ExecuteAsync(
                "INSERT INTO AvailableMod (ModId, AvailableVersion) VALUES (@ModId, 'v1')",
                new { ModId = modId });

            // Act
            var result = await _repo.DeleteByAppIdAsync(appId, Connection);

            // Assert
            Assert.True(result);
            var count = await Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM AvailableMod WHERE ModId = @ModId", new { ModId = modId });
            Assert.Equal(0, count);
        }
    }
}