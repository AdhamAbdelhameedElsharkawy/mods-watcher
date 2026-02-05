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

        private async Task<Guid> SeedModAsync()
        {
            // FK Requirements: ModdedApp -> Mod -> InstalledModHistory
            const string appSql = "INSERT INTO ModdedApp (Name) VALUES ('TestApp'); SELECT last_insert_rowid();";
            int appId = await Connection.QuerySingleAsync<int>(appSql);

            Guid modId = Guid.NewGuid();
            await Connection.ExecuteAsync(
                "INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES (@Id, @AppId, 'TestMod', 1, 0)",
                new { Id = modId, AppId = appId });

            return modId;
        }

        [Fact]
        public async Task InsertAsync_ShouldSaveRecord_AndReturnEntity()
        {
            // Arrange
            var modId = await SeedModAsync();
            var history = new InstalledModHistory { ModId = modId, Version = "1.0.0" };

            // Act
            var result = await _repo.InsertAsync(history, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1.0.0", result.Version);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectEntry_UsingInternalId()
        {
            // Arrange
            var modId = await SeedModAsync();
            await Connection.ExecuteAsync(
                "INSERT INTO InstalledModHistory (ModId, Version) VALUES (@ModId, 'v1')",
                new { ModId = modId });
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
            var modId = await SeedModAsync();
            await Connection.ExecuteAsync(@"
                INSERT INTO InstalledModHistory (ModId, Version) VALUES 
                (@ModId, 'v1'), 
                (@ModId, 'v2')", new { ModId = modId });

            // Act
            var results = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.True(results.Count() >= 2);
        }

        [Fact]
        public async Task FindByModIdAsync_ShouldReturnOnlySpecificModHistory()
        {
            // Arrange
            var modId1 = await SeedModAsync();
            var modId2 = Guid.NewGuid(); // Just for differentiation in query
            await Connection.ExecuteAsync("INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) SELECT @Id, AppId, 'Mod2', @IsUsed, @IsDeprecated FROM Mod LIMIT 1", new { Id = modId2, IsUsed = true, IsDeprecated = false });

            await Connection.ExecuteAsync("INSERT INTO InstalledModHistory (ModId, Version) VALUES (@Id, 'Target')", new { Id = modId1 });
            await Connection.ExecuteAsync("INSERT INTO InstalledModHistory (ModId, Version) VALUES (@Id, 'Other')", new { Id = modId2 });

            // Act
            var results = await _repo.FindByModIdAsync(modId1, Connection);

            // Assert
            Assert.Single(results);
            Assert.Equal("Target", results.First().Version);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntryFromDatabase()
        {
            // Arrange
            var modId = await SeedModAsync();
            await Connection.ExecuteAsync("INSERT INTO InstalledModHistory (ModId) VALUES (@ModId)", new { ModId = modId });
            int dbId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var deleted = await _repo.DeleteAsync(dbId, Connection);

            // Assert
            Assert.True(deleted);
            var exists = await Connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM InstalledModHistory WHERE Id = @Id", new { Id = dbId });
            Assert.False(exists);
        }
    }
}