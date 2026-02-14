using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data;
using Moq;
using System.Data;

namespace ModsAutomator.Tests.Repos
{
    public class InstalledModRepositoryTests : BaseRepositoryTest
    {
        private readonly InstalledModRepository _repo;
        private readonly Mock<IModRepository> _modRepoMock;

        public InstalledModRepositoryTests()
        {
            _modRepoMock = new Mock<IModRepository>();
            _repo = new InstalledModRepository(FactoryMock.Object, _modRepoMock.Object);
        }

        private async Task<(int AppId, Guid ModId)> SeedDatabaseAsync()
        {
            const string appSql = "INSERT INTO ModdedApp (Name) VALUES ('Skyrim'); SELECT last_insert_rowid();";
            int appId = await Connection.QuerySingleAsync<int>(appSql);

            Guid modId = Guid.NewGuid();
            const string modSql = "INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES (@Id, @AppId, 'SkyUI', 1, 0)";
            await Connection.ExecuteAsync(modSql, new { Id = modId, AppId = appId });

            return (appId, modId);
        }

        [Fact]
        public async Task InsertAsync_ShouldCreateInstalledMod_AndMapGuidCorrectly()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            var installedMod = new InstalledMod
            {
                Id = ids.ModId,
                InstalledVersion = "5.2",
                InstalledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PackageType = PackageType.Zip
            };

            _modRepoMock.Setup(m => m.GetByIdAsync(ids.ModId, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default))
                        .ReturnsAsync(new Mod { Id = ids.ModId });

            // Act
            await _repo.InsertAsync(installedMod, Connection);

            // Assert
            var result = await _repo.FindByModIdAsync(ids.ModId, Connection);
            Assert.NotNull(result);
            Assert.Equal(ids.ModId, result.Id);
            Assert.Equal("5.2", result.InstalledVersion);
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), result.InstalledDate);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectModByInternalId()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync("INSERT INTO InstalledMod (ModId, InstalledVersion) VALUES (@ModId, '1.1')", new { ModId = ids.ModId });
            int internalId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var result = await _repo.GetByIdAsync(internalId, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ids.ModId, result.Id);
            Assert.Equal("1.1", result.InstalledVersion);
        }

        [Fact]
        public async Task QueryAllAsync_ShouldReturnEveryInstalledMod()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync("INSERT INTO InstalledMod (ModId) VALUES (@ModId)", new { ModId = ids.ModId });

            // Act
            var results = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.Single(results);
            Assert.Equal("SkyUI", results.First().Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPersistChanges()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync("INSERT INTO InstalledMod (ModId, InstalledVersion, PriorityOrder) VALUES (@ModId, '1.0', 1)", new { ModId = ids.ModId });

            var update = new InstalledMod
            {
                Id = ids.ModId,
                InstalledVersion = "2.0",
                PriorityOrder = 99,
                PackageType = PackageType.Rar
            };

            // Act
            await _repo.UpdateAsync(update, Connection);

            // Assert
            var result = await _repo.FindByModIdAsync(ids.ModId, Connection);
            Assert.Equal("2.0", result.InstalledVersion);
            Assert.Equal(99, result.PriorityOrder);
            Assert.Equal(PackageType.Rar, result.PackageType);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveRecord()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync("INSERT INTO InstalledMod (ModId) VALUES (@ModId)", new { ModId = ids.ModId });
            int internalId = await Connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");

            // Act
            var deleted = await _repo.DeleteAsync(internalId, Connection);
            var count = await Connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM InstalledMod WHERE Id = @Id", new { Id = internalId });

            // Assert
            Assert.True(deleted);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task FindByModIdAsync_ShouldReturnCorrectModByGuid()
        {
            // Arrange
            var ids = await SeedDatabaseAsync();
            await Connection.ExecuteAsync(
                "INSERT INTO InstalledMod (ModId, InstalledVersion) VALUES (@ModId, '5.0')",
                new { ModId = ids.ModId });

            // Act
            var result = await _repo.FindByModIdAsync(ids.ModId, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ids.ModId, result.Id); // This checks the Guid mapping specifically
            Assert.Equal("5.0", result.InstalledVersion);
        }

        [Fact]
        public async Task DeleteByAppIdAsync_ShouldWipeDataUsingSubquery()
        {
            // Arrange
            var ids = await SeedDatabaseAsync(); // Seeds AppId and ModId
            int appId = ids.AppId;
            Guid modId = ids.ModId;

            await Connection.ExecuteAsync(
                "INSERT INTO InstalledMod (ModId, InstalledVersion) VALUES (@ModId, 'v1')",
                new { ModId = modId });

            // Act
            var result = await _repo.DeleteByAppIdAsync(appId, Connection);

            // Assert
            Assert.True(result);
            var count = await Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM InstalledMod WHERE ModId = @ModId", new { ModId = modId });
            Assert.Equal(0, count);
        }
    }
}