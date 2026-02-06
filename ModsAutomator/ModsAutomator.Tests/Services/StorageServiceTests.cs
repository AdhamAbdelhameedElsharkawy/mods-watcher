using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using ModsAutomator.Services;
using Moq;
using System.Data;

namespace ModsAutomator.Tests.Services
{
    public class StorageServiceTests
    {
        private readonly Mock<IConnectionFactory> _factoryMock;
        private readonly Mock<IModdedAppRepository> _appRepoMock;
        private readonly Mock<IModRepository> _shellModRepoMock;
        private readonly Mock<IInstalledModRepository> _modRepoMock;
        private readonly Mock<IDbConnection> _connectionMock;
        private readonly StorageService _service;

        public StorageServiceTests()
        {
            _factoryMock = new Mock<IConnectionFactory>();
            _appRepoMock = new Mock<IModdedAppRepository>();
            _shellModRepoMock = new Mock<IModRepository>();
            _modRepoMock = new Mock<IInstalledModRepository>();
            _connectionMock = new Mock<IDbConnection>();

            // 1. SETUP: When the service asks for a connection, give it our mock connection
            _factoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

            _service = new StorageService(
                _factoryMock.Object,
                _appRepoMock.Object,
                _shellModRepoMock.Object,
                _modRepoMock.Object
            );
        }

        #region ModdedApp Tests

        [Fact]
        public async Task AddAppAsync_ShouldCallInsert_WithActiveConnection()
        {
            // Arrange
            var newApp = new ModdedApp { Name = "Test App" };

            // Act
            await _service.AddAppAsync(newApp);

            // Assert
            // We use It.IsAny<IDbConnection>() because the service creates the connection internally
            _appRepoMock.Verify(r => r.InsertAsync(
                It.Is<ModdedApp>(a => a.Name == "Test App"),
                It.IsAny<IDbConnection>(),
                null,
                default),
                Times.Once);

            // Bonus Assert: Verify the service actually tried to open the connection
            _connectionMock.Verify(c => c.Open(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateAppAsync_ShouldCallUpdate_WithActiveConnection()
        {
            // Arrange
            var existingApp = new ModdedApp { Id = 1, Name = "Updated Name" };

            // Act
            await _service.UpdateAppAsync(existingApp);

            // Assert
            _appRepoMock.Verify(r => r.UpdateAsync(
                It.Is<ModdedApp>(a => a.Name == "Updated Name"),
                It.IsAny<IDbConnection>(),
                null,
                default),
                Times.Once);

            _connectionMock.Verify(c => c.Open(), Times.AtLeastOnce);
        }



        [Fact]
        public async Task GetAllAppsAsync_ShouldReturnListFromRepo()
        {
            // Arrange
            var apps = new List<ModdedApp> { new ModdedApp { Id = 1 }, new ModdedApp { Id = 2 } };
            _appRepoMock.Setup(r => r.QueryAllAsync(It.IsAny<IDbConnection>(), null, default))
                        .ReturnsAsync(apps);

            // Act
            var result = await _service.GetAllAppsAsync();

            // Assert
            Assert.Equal(2, result.Count());
            _connectionMock.Verify(c => c.Open(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetAllAppSummariesAsync_ShouldReturnCombinedData()
        {
            // 1. Arrange: Create fake apps
            var apps = new List<ModdedApp>
    {
        new ModdedApp { Id = 1, Name = "Game A", InstalledVersion = "1.0" },
        new ModdedApp { Id = 2, Name = "Game B", InstalledVersion = "2.0" }
    };

            // Setup: When Repo.QueryAllAsync is called, return our list
            _appRepoMock.Setup(r => r.QueryAllAsync(It.IsAny<IDbConnection>(), null, default))
                        .ReturnsAsync(apps);

            // Setup: Return specific stats for App ID 1
            _modRepoMock.Setup(r => r.GetAppSummaryStatsAsync(1, "1.0", It.IsAny<IDbConnection>()))
                        .ReturnsAsync((5, 1024, 1)); // 5 active, 1GB, 1 incompatible

            // Setup: Return specific stats for App ID 2
            _modRepoMock.Setup(r => r.GetAppSummaryStatsAsync(2, "2.0", It.IsAny<IDbConnection>()))
                        .ReturnsAsync((0, 0, 0));

            // 2. Act
            var results = (await _service.GetAllAppSummariesAsync()).ToList();

            // 3. Assert
            Assert.Equal(2, results.Count);

            // Check if App A (ID 1) got the right stats mapped
            var summaryA = results.First(s => s.App.Id == 1);
            Assert.Equal(5, summaryA.ActiveCount);
            Assert.Equal(1024, summaryA.TotalSize);
            Assert.Equal(1, summaryA.IncompatibleCount);
        }
    

    #endregion
    #region Mod Repository Tests

    [Fact]
        public async Task GetModsByAppId_ShouldReturnCombinedShellAndInstallation()
        {
            // Arrange
            int appId = 1;
            var shells = new List<Mod>
    {
        new Mod { Id = Guid.NewGuid(), Name = "Mod A", AppId = appId },
        new Mod { Id = Guid.NewGuid(), Name = "Mod B", AppId = appId }
    };

            var installedRecord = new InstalledMod
            {
                Id = shells[0].Id,
                InstalledVersion = "1.0.1",
                IsUsed = true
            };

            // 1. Return the shells for this app
            _shellModRepoMock.Setup(r => r.GetByAppIdAsync(appId, It.IsAny<IDbConnection>()))
                             .ReturnsAsync(shells);

            // 2. Return an installation record for the first shell only (simulate second mod not installed)
            _modRepoMock.Setup(r => r.FindByModIdAsync(shells[0].Id, It.IsAny<IDbConnection>()))
                        .ReturnsAsync(installedRecord);

            _modRepoMock.Setup(r => r.FindByModIdAsync(shells[1].Id, It.IsAny<IDbConnection>()))
                        .ReturnsAsync((InstalledMod?)null);

            // Act
            var results = (await _service.GetModsByAppId(appId)).ToList();

            // Assert
            Assert.Equal(2, results.Count);

            // Check first item (Fully installed)
            var (shell1, inst1) = results[0];
            Assert.Equal("Mod A", shell1.Name);
            Assert.NotNull(inst1);
            Assert.Equal("1.0.1", inst1.InstalledVersion);

            // Check second item (Shell only, no installation)
            var (shell2, inst2) = results[1];
            Assert.Equal("Mod B", shell2.Name);
            Assert.Null(inst2);

            _connectionMock.Verify(c => c.Open(), Times.AtLeastOnce);
        }

        #endregion
    }

}