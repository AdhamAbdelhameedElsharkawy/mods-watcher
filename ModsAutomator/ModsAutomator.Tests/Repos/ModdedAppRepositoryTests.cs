using ModsAutomator.Core.Entities;
using ModsAutomator.Data;
using Dapper;

namespace ModsAutomator.Tests.Repos
{
    public class ModdedAppRepositoryTests : BaseRepositoryTest
    {
        private readonly ModdedAppRepository _repo;

        public ModdedAppRepositoryTests()
        {
            // Initialize the repository with the mock factory from BaseRepositoryTest
            _repo = new ModdedAppRepository(FactoryMock.Object);
        }

        [Fact]
        public async Task InsertAsync_ShouldSaveApp_AndReturnIt()
        {
            // Arrange
            var app = new ModdedApp
            {
                Name = "Skyrim",
                LastUpdatedDate = new DateOnly(2026, 2, 5)
            };

            // Act
            await _repo.InsertAsync(app, Connection);

            // Assert
            var result = await Connection.QuerySingleOrDefaultAsync<ModdedApp>(
                "SELECT * FROM ModdedApp WHERE Name = @Name", new { Name = "Skyrim" });

            Assert.NotNull(result);
            Assert.Equal("Skyrim", result.Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingApp()
        {
            // Arrange: Insert an initial record
            await Connection.ExecuteAsync(@"
        INSERT INTO ModdedApp (Id, Name, InstalledVersion) 
        VALUES (1, 'Old Name', '1.0')");

            var appToUpdate = new ModdedApp
            {
                Id = 1,
                Name = "New Name", 
                Description = "Updated Description",
                InstalledVersion = "2.0",
                LastUpdatedDate = new DateOnly(2026, 2, 5)
            };

            // Act
            await _repo.UpdateAsync(appToUpdate, Connection);

            // Assert: Verify the changes in the database
            var result = await Connection.QuerySingleOrDefaultAsync<ModdedApp>(
                "SELECT * FROM ModdedApp WHERE Id = 1");

            Assert.NotNull(result);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal("2.0", result.InstalledVersion);
            Assert.Equal(appToUpdate.LastUpdatedDate, result.LastUpdatedDate);
        }

        [Fact]
        public async Task QueryAllAsync_ShouldReturnAllApps()
        {
            // Arrange
            await Connection.ExecuteAsync("INSERT INTO ModdedApp (Name) VALUES ('App 1'), ('App 2')");

            // Act
            var apps = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.Equal(2, apps.Count());
        }

        [Fact]
        public async Task FindByNameAsync_ShouldReturnCorrectApp()
        {
            // Arrange
            await Connection.ExecuteAsync("INSERT INTO ModdedApp (Name) VALUES ('TargetApp')");

            // Act
            var app = await _repo.FindByNameAsync("TargetApp", Connection);

            // Assert
            Assert.NotNull(app);
            Assert.Equal("TargetApp", app.Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveAppFromDatabase()
        {
            // Arrange
            await Connection.ExecuteAsync("INSERT INTO ModdedApp (Id, Name) VALUES (99, 'DeleteMe')");

            // Act
            var result = await _repo.DeleteAsync(99, Connection);
            var exists = await Connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM ModdedApp WHERE Id = 99");

            // Assert
            Assert.True(result);
            Assert.False(exists);
        }
    }
}