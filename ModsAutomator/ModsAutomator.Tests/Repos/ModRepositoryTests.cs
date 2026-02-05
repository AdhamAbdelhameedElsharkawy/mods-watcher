using Dapper;
using ModsAutomator.Core.Entities;
using ModsAutomator.Data;
using ModsAutomator.Tests.Repos;

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
        public async Task InsertAsync_ShouldSaveMod_AndLinkToApp()
        {
            // Arrange
            int appId = await SeedParentAppAsync();
            var mod = new Mod
            {
                Id = Guid.NewGuid(), 
                AppId = appId,
                Name = "High-Res Textures",
                IsUsed = true,
                IsDeprecated = false
            };

            // Act
            await _repo.InsertAsync(mod, Connection);

            // Assert
            var result = await Connection.QuerySingleOrDefaultAsync<Mod>(
                "SELECT * FROM Mod WHERE Id = @Id", new { Id = mod.Id });

            Assert.NotNull(result);
            Assert.Equal(mod.Name, result.Name);
            Assert.Equal(appId, result.AppId);
        }

        [Fact]
        public async Task GetByAppIdAsync_ShouldReturnOnlyModsForThatApp()
        {
            // Arrange
            int appA = await SeedParentAppAsync();
            int appB = await SeedParentAppAsync();

            await Connection.ExecuteAsync("INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES (@Id, @AppId, @Name, 1, 0)",
                new[] {
                    new { Id = Guid.NewGuid(), AppId = appA, Name = "Mod A" },
                    new { Id = Guid.NewGuid(), AppId = appB, Name = "Mod B" }
                });

            // Act
            var mods = await _repo.GetByAppIdAsync(appA, Connection);

            // Assert
            Assert.Single(mods);
            Assert.Equal("Mod A", mods.First().Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyAllowedFields()
        {
            // Arrange
            int appId = await SeedParentAppAsync();
            Guid modId = Guid.NewGuid();
            await Connection.ExecuteAsync(
                "INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated, Description) VALUES (@Id, @AppId, 'Original', 1, 0, 'Old')",
                new { Id = modId, AppId = appId });

            var updatedMod = new Mod
            {
                Id = modId,
                IsUsed = false,
                IsDeprecated = true,
                Description = "New Description",
                RootSourceUrl = "http://nexusmods.com"
            };

            // Act
            await _repo.UpdateAsync(updatedMod, Connection);

            // Assert
            var result = await Connection.QuerySingleAsync<Mod>("SELECT * FROM Mod WHERE Id = @Id", new { Id = modId });
            Assert.Equal(false, result.IsUsed);
            Assert.Equal(true, result.IsDeprecated);
            Assert.Equal("New Description", result.Description);
            Assert.Equal("http://nexusmods.com", result.RootSourceUrl);
            // Name should NOT have changed if you didn't include it in SQL
            Assert.Equal("Original", result.Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenModIsRemoved()
        {
            // Arrange
            int appId = await SeedParentAppAsync();
            var modId = Guid.NewGuid();
            await Connection.ExecuteAsync(
                "INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES (@Id, @AppId, 'Bye', 1, 0)",
                new { Id = modId, AppId = appId });

            // Act
            var result = await _repo.DeleteAsync(modId, Connection);

            // Assert
            Assert.True(result);
            var exists = await Connection.ExecuteScalarAsync<bool>("SELECT COUNT(1) FROM Mod WHERE Id = @Id", new { Id = modId });
            Assert.False(exists);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectMod()
        {
            // Arrange
            int appId = await SeedParentAppAsync();
            Guid modId = Guid.NewGuid();
            await Connection.ExecuteAsync(
                "INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES (@Id, @AppId, 'FindMe', 1, 0)",
                new { Id = modId, AppId = appId });

            // Act
            var result = await _repo.GetByIdAsync(modId, Connection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FindMe", result.Name);
            Assert.Equal(modId, result.Id);
        }

        [Fact]
        public async Task QueryAllAsync_ShouldReturnEveryModInDatabase()
        {
            // Arrange
            int appId = await SeedParentAppAsync();
            await Connection.ExecuteAsync(@"
        INSERT INTO Mod (Id, AppId, Name, IsUsed, IsDeprecated) VALUES 
        (@Id1, @AppId, 'Mod 1', 1, 0),
        (@Id2, @AppId, 'Mod 2', 1, 0)",
                new { Id1 = Guid.NewGuid(), Id2 = Guid.NewGuid(), AppId = appId });

            // Act
            var results = await _repo.QueryAllAsync(Connection);

            // Assert
            Assert.Equal(2, results.Count());
        }
    }
}