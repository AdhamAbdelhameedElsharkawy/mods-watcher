using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Data;
using ModsAutomator.Tests.Repos;
using Xunit;

namespace ModsAutomator.Tests.Repos
{
    public class UnusedModHistoryRepositoryTests : BaseRepositoryTest
    {
        private readonly UnusedModHistoryRepository _repo;

        public UnusedModHistoryRepositoryTests()
        {
            _repo = new UnusedModHistoryRepository(FactoryMock.Object);
        }

        [Fact]
        public async Task InsertAsync_ShouldSaveSnapshot_WithAllRequiredFields()
        {
            // Arrange
            var history = new UnusedModHistory
            {
                ModId = Guid.NewGuid(),
                ModdedAppId = 1,
                Name = "Snapshot Mod",
                Version = "1.0.0",
                AppVersion = "2.0",
                RemovedAt = new DateOnly(2026, 2, 5),
                Reason = "Cleanup"
            };

            // Act
            var result = await _repo.InsertAsync(history, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Snapshot Mod", result.Name);
            Assert.NotEqual(Guid.Empty, result.ModId);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntry_WithPopulatedNonNullableFields()
        {
            // Arrange
            var modId = Guid.NewGuid();
            await Connection.ExecuteAsync(@"
                INSERT INTO UnusedModHistory (ModId, ModdedAppId, Name, Version, AppVersion) 
                VALUES (@ModId, 1, 'Test Mod', '1.0', '1.5')",
                new { ModId = modId });

            int dbId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var result = await _repo.GetByIdAsync(dbId, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dbId, result.Id);
            Assert.Equal(modId, result.ModId);
            Assert.Equal("Test Mod", result.Name);
        }

        [Fact]
        public async Task QueryAllAsync_ShouldReturnAllHistoryRecords()
        {
            // Arrange
            await Connection.ExecuteAsync(@"
                INSERT INTO UnusedModHistory (ModId, ModdedAppId, Name, Version, AppVersion) VALUES 
                (@G1, 1, 'Mod A', 'v1', 'v1'), 
                (@G2, 1, 'Mod B', 'v2', 'v2')",
                new { G1 = Guid.NewGuid(), G2 = Guid.NewGuid() });

            // Act
            var results = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.True(results.Count() >= 2);
            Assert.All(results, m => Assert.False(string.IsNullOrEmpty(m.Name)));
        }

        [Fact]
        public async Task FindByModdedAppIdAsync_ShouldFilterByApp()
        {
            // Arrange
            int targetAppId = 99;
            await Connection.ExecuteAsync(@"
                INSERT INTO UnusedModHistory (ModId, ModdedAppId, Name, Version, AppVersion) 
                VALUES (@G1, @AppId, 'Target Mod', '1', '1')",
                new { G1 = Guid.NewGuid(), AppId = targetAppId });

            await Connection.ExecuteAsync(@"
                INSERT INTO UnusedModHistory (ModId, ModdedAppId, Name, Version, AppVersion) 
                VALUES (@G1, 1, 'Other Mod', '1', '1')",
                new { G1 = Guid.NewGuid() });

            // Act
            var results = await _repo.FindByModdedAppIdAsync(targetAppId, Connection);

            // Assert
            Assert.Single(results);
            Assert.Equal("Target Mod", results.First().Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntryByInternalId()
        {
            // Arrange
            await Connection.ExecuteAsync(@"
                INSERT INTO UnusedModHistory (ModId, ModdedAppId, Name, Version, AppVersion) 
                VALUES (@G, 1, 'Delete Me', '0', '0')",
                new { G = Guid.NewGuid() });

            int dbId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var deleted = await _repo.DeleteAsync(dbId, Connection);

            // Assert
            Assert.True(deleted);
            var exists = await Connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM UnusedModHistory WHERE Id = @Id", new { Id = dbId });
            Assert.False(exists);
        }
    }
}