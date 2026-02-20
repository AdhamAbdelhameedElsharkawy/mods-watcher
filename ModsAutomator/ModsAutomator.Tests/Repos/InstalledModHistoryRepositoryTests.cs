using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Data;
using ModsAutomator.Tests.Repos;
using Xunit;

namespace ModsAutomator.Tests.Repos
{
    public class InstalledModHistoryRepositoryTests : BaseRepositoryTest
    {
        private readonly InstalledModHistoryRepository _repo;

        public InstalledModHistoryRepositoryTests()
        {
            _repo = new InstalledModHistoryRepository(FactoryMock.Object);
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
        public async Task InsertAsync_ShouldSaveRecord_AndReturnEntity()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            var history = new InstalledModHistory { ModId = ids.ModId, Version = "1.0.0", DownloadUrl = "http://google.com" };

            // Act
            var result = await _repo.InsertAsync(history, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1.0.0", result.Version);
            Assert.Equal("http://google.com", result.DownloadUrl);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectEntry_UsingInternalId()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync(
                "INSERT INTO InstalledModHistory (ModId, Version) VALUES (@ModId, 'v1')",
                new { ModId = ids.ModId });
            int dbId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var result = await _repo.GetByIdAsync(dbId, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dbId, result.InternalId);
            Assert.Equal("v1", result.Version);
        }

        [Fact]
        public async Task QueryAllAsync_ShouldReturnAllHistoryRecords()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync(@"
                INSERT INTO InstalledModHistory (ModId, Version) VALUES 
                (@ModId, 'v1'), 
                (@ModId, 'v2')", new { ModId = ids.ModId });

            // Act
            var results = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.True(results.Count() >= 2);
        }

        [Fact]
        public async Task FindByModIdAsync_ShouldReturnOnlySpecificModHistory()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            var modId2 = Guid.NewGuid(); // Just for differentiation in query
            await Connection.ExecuteAsync("INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) SELECT @Id, AppId, 'Mod2', @IsUsed, @IsDeprecated FROM Mod LIMIT 1", new { Id = modId2, IsUsed = true, IsDeprecated = false });

            await Connection.ExecuteAsync("INSERT INTO InstalledModHistory (ModId, Version) VALUES (@Id, 'Target')", new { Id = ids.ModId });
            await Connection.ExecuteAsync("INSERT INTO InstalledModHistory (ModId, Version) VALUES (@Id, 'Other')", new { Id = modId2 });

            // Act
            var results = await _repo.FindByModIdAsync(ids.ModId, Connection);

            // Assert
            Assert.Single(results);
            Assert.Equal("Target", results.First().Version);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntryFromDatabase()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync("INSERT INTO InstalledModHistory (ModId) VALUES (@ModId)", new { ModId = ids.ModId });
            int dbId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var deleted = await _repo.DeleteAsync(dbId, Connection);

            // Assert
            Assert.True(deleted);
            var exists = await Connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM InstalledModHistory WHERE Id = @Id", new { Id = dbId });
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteByAppIdAsync_ShouldWipeDataUsingSubquery()
        {
            /// Arrange
            var ids = await SeedDatabaseAsync(); // Seeds AppId and ModId
            int appId = ids.AppId;
            Guid modId = ids.ModId;

            await Connection.ExecuteAsync(
                "INSERT INTO InstalledModHistory (ModId, Version) VALUES (@ModId, 'v1')",
                new { ModId = modId });

            // Act
            var result = await _repo.DeleteByAppIdAsync(appId, Connection);

            // Assert
            Assert.True(result);
            var count = await Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM InstalledModHistory WHERE ModId = @ModId", new { ModId = modId });
            Assert.Equal(0, count);
        }
    }
}