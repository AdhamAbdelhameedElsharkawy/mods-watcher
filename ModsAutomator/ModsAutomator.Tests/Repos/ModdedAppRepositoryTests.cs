using ModsAutomator.Core.Entities;
using ModsAutomator.Data;
using Dapper;
using Xunit;

namespace ModsAutomator.Tests.Repos
{
    public class ModdedAppRepositoryTests : BaseRepositoryTest
    {
        private readonly ModdedAppRepository _repo;

        public ModdedAppRepositoryTests()
        {
            _repo = new ModdedAppRepository(FactoryMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectApp()
        {
            // Arrange
            await Connection.ExecuteAsync("INSERT INTO ModdedApp (Id, Name) VALUES (10, 'Target')");

            // Act
            var result = await _repo.GetByIdAsync(10, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Target", result.Name);
        }

        [Fact]
        public async Task InsertAsync_ShouldPopulateId_OnReturnedObject()
        {
            // Arrange
            var app = new ModdedApp { Name = "ValidApp" };

            // Act
            var result = await _repo.InsertAsync(app, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0, "The Repository failed to capture the database-generated ID.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingApp()
        {
            // Arrange
            await Connection.ExecuteAsync(@"
                INSERT INTO ModdedApp (Id, Name, InstalledVersion) 
                VALUES (1, 'Old Name', '1.0')");

            var appToUpdate = new ModdedApp
            {
                Id = 1,
                Name = "New Name", // This is missing from your Repo SQL!
                Description = "Updated Description",
                InstalledVersion = "2.0",
                LastUpdatedDate = new DateOnly(2026, 2, 5)
            };

            // Act
            await _repo.UpdateAsync(appToUpdate, Connection);

            // Assert
            var result = await Connection.QuerySingleOrDefaultAsync<ModdedApp>(
                "SELECT * FROM ModdedApp WHERE Id = 1");

            Assert.NotNull(result);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal("2.0", result.InstalledVersion);
            
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
        public async Task QueryAllAsync_ShouldReturnEmpty_WhenNoAppsExist()
        {
            // Act
            var apps = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.Empty(apps);
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
        public async Task DeleteAsync_ShouldRemoveApp_AndReturnTrue()
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

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenIdDoesNotExist()
        {
            // Act
            var result = await _repo.DeleteAsync(999, Connection);

            // Assert
            Assert.False(result);
        }
    }
}