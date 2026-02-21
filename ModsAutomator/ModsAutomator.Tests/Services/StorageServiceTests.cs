using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Interfaces;
using ModsWatcher.Desktop.Services;
using ModsWatcher.Services;
using Moq;
using System.Data;

namespace ModsWatcher.Tests.Services
{
    public class StorageServiceTests
    {
        private readonly Mock<IConnectionFactory> _factoryMock;
        private readonly Mock<IModdedAppRepository> _appRepoMock;
        private readonly Mock<IModRepository> _shellModRepoMock;
        private readonly Mock<IInstalledModRepository> _modRepoMock;
        private readonly Mock<IUnusedModHistoryRepository> _unusedModRepoMock;
        private readonly Mock<IInstalledModHistoryRepository> _installedModHistoryRepoMock;
        private readonly Mock<IAvailableModRepository> _availableModRepoMock;
        private readonly Mock<IModCrawlerConfigRepository> _configRepoMock;
        private readonly Mock<IDbConnection> _connectionMock;
        private readonly Mock<CommonUtils> _commonUtilsMock;
        private readonly StorageService _service;

        public StorageServiceTests()
        {
            _factoryMock = new Mock<IConnectionFactory>();
            _appRepoMock = new Mock<IModdedAppRepository>();
            _shellModRepoMock = new Mock<IModRepository>();
            _modRepoMock = new Mock<IInstalledModRepository>();
            _unusedModRepoMock = new Mock<IUnusedModHistoryRepository>();
            _installedModHistoryRepoMock = new Mock<IInstalledModHistoryRepository>();
            _availableModRepoMock = new Mock<IAvailableModRepository>();
            _configRepoMock = new Mock<IModCrawlerConfigRepository>();
            _connectionMock = new Mock<IDbConnection>();
            _commonUtilsMock = new Mock<CommonUtils>();


            // 1. SETUP: When the service asks for a connection, give it our mock connection
            _factoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

            _service = new StorageService(
                _factoryMock.Object,
                _appRepoMock.Object,
                _shellModRepoMock.Object,
                _modRepoMock.Object,
                _unusedModRepoMock.Object,
                _installedModHistoryRepoMock.Object,
                _configRepoMock.Object,
                _availableModRepoMock.Object,
                _commonUtilsMock.Object
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

            // Setup: Return app list
            _appRepoMock.Setup(r => r.QueryAllAsync(It.IsAny<IDbConnection>(), null, default))
                        .ReturnsAsync(apps);

            // Setup: Return specific stats for App ID 1 (ActiveCount: 5, PotentialUpdatesCount: 3)
            _shellModRepoMock.Setup(r => r.GetWatcherSummaryStatsAsync(1, It.IsAny<IDbConnection>()))
                        .ReturnsAsync((5, 3));

            // Setup: Return specific stats for App ID 2 (ActiveCount: 0, PotentialUpdatesCount: 0)
            _shellModRepoMock.Setup(r => r.GetWatcherSummaryStatsAsync(2, It.IsAny<IDbConnection>()))
                        .ReturnsAsync((0, 0));

            // 2. Act
            var results = (await _service.GetAllAppSummariesAsync()).ToList();

            // 3. Assert
            Assert.Equal(2, results.Count);

            // Check App A (ID 1)
            var summaryA = results.First(s => s.App.Id == 1);
            Assert.Equal(5, summaryA.ActiveCount);
            Assert.Equal(3, summaryA.PotentialUpdatesCount);

            // Check App B (ID 2)
            var summaryB = results.First(s => s.App.Id == 2);
            Assert.Equal(0, summaryB.ActiveCount);
            Assert.Equal(0, summaryB.PotentialUpdatesCount);
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
            var results = (await _service.GetFullModsByAppId(appId)).ToList();

            // Assert
            Assert.Equal(2, results.Count);

            // Check first item (Fully installed)
            var (shell1, inst1, config1) = results[0];
            Assert.Equal("Mod A", shell1.Name);
            Assert.NotNull(inst1);
            Assert.Equal("1.0.1", inst1.InstalledVersion);

            // Check second item (Shell only, no installation)
            var (shell2, inst2, config2) = results[1];
            Assert.Equal("Mod B", shell2.Name);
            Assert.Null(inst2);

            _connectionMock.Verify(c => c.Open(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task AddModShellAsync_ShouldCallInsert_WithGeneratedGuid()
        {
            // Arrange
            var shell = new Mod { Name = "New Mod", RootSourceUrl = "https://source.com", Id = Guid.NewGuid(), AppId = 1 };

            // Act
            await _service.AddModShellAsync(shell);

            // Assert
            _shellModRepoMock.Verify(r => r.InsertAsync(
                It.Is<Mod>(m => m.Name == "New Mod" && m.Id != Guid.Empty),
                It.IsAny<IDbConnection>(),
                null,
                default),
                Times.Once);
        }

        [Fact]
        public async Task UpdateModShellAsync_ShouldCallUpdate()
        {
            // Arrange
            var shell = new Mod { Id = Guid.NewGuid(), Name = "Updated Mod" };

            // Act
            await _service.UpdateModShellAsync(shell);

            // Assert
            _shellModRepoMock.Verify(r => r.UpdateAsync(
                It.Is<Mod>(m => m.Name == "Updated Mod"),
                It.IsAny<IDbConnection>(),
                null,
                default),
                Times.Once);
        }

        #endregion

        #region Retired Mods Tests

        [Fact]
        public async Task GetRetiredModsByAppIdAsync_ShouldCallFindByModdedAppId()
        {
            // Arrange
            int appId = 99;
            var historyList = new List<UnusedModHistory> { new UnusedModHistory { Name = "Old Mod" } };
            _unusedModRepoMock.Setup(r => r.FindByModdedAppIdAsync(appId, It.IsAny<IDbConnection>()))
                              .ReturnsAsync(historyList);

            // Act
            var result = await _service.GetRetiredModsByAppIdAsync(appId);

            // Assert
            Assert.Single(result);
            _unusedModRepoMock.Verify(r => r.FindByModdedAppIdAsync(appId, It.IsAny<IDbConnection>()), Times.Once);
        }

        [Fact]
        public async Task RestoreModFromHistoryAsync_ShouldInsertModAndRemoveHistory()
        {
            // Arrange
            var transactionMock = new Mock<IDbTransaction>();
            _connectionMock.Setup(c => c.BeginTransaction()).Returns(transactionMock.Object);

            var history = new UnusedModHistory
            {
                Id = 1,
                ModId = Guid.NewGuid(),
                ModdedAppId = 10,
                Name = "Restorable Mod",
                RootSourceUrl = "https://mod.com/source"
            };

            // Act
            await _service.RestoreModFromHistoryAsync(history);

            // Assert: Check that a NEW Mod was created with the DNA from history
            _shellModRepoMock.Verify(r => r.InsertAsync(
                It.Is<Mod>(m => m.Id == history.ModId && m.Name == history.Name && m.RootSourceUrl == history.RootSourceUrl),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>(), // Transaction must be present
                default),
                Times.Once);

            // Assert: Check that the history record was deleted
            _unusedModRepoMock.Verify(r => r.DeleteAsync(
                history.Id,
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>(),
                default),
                Times.Once);
        }

        #endregion

        #region Mod History Tests

        [Fact]
        public async Task GetInstalledModHistoryAsync_ShouldCallRepoWithCorrectId()
        {
            // Arrange
            var targetModId = Guid.NewGuid();
            var expectedHistory = new List<InstalledModHistory>
    {
        new() { ModId = targetModId, Version = "1.0.0", InstalledAt = new DateOnly(2025, 1, 1) },
        new() { ModId = targetModId, Version = "1.1.0", InstalledAt = new DateOnly(2026, 2, 1) }
    };

            _installedModHistoryRepoMock
                .Setup(r => r.FindByModIdAsync(targetModId, _connectionMock.Object))
                .ReturnsAsync(expectedHistory);

            // Act
            var result = await _service.GetInstalledModHistoryAsync(targetModId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _installedModHistoryRepoMock.Verify(r => r.FindByModIdAsync(targetModId, _connectionMock.Object), Times.Once);
        }

        [Fact]
        public async Task GetInstalledModHistoryAsync_ShouldReturnEmpty_WhenNoHistoryExists()
        {
            // Arrange
            var targetModId = Guid.NewGuid();
            _installedModHistoryRepoMock
                .Setup(r => r.FindByModIdAsync(targetModId, _connectionMock.Object))
                .ReturnsAsync(new List<InstalledModHistory>());

            // Act
            var result = await _service.GetInstalledModHistoryAsync(targetModId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task RollbackToVersionAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            var target = new InstalledModHistory { Version = "1.0.0", DownloadUrl = "C:\\mods\\backup.zip" };

            // Ensure BeginTransaction does not return null
            var mockTransaction = new Mock<IDbTransaction>();

            _connectionMock
                .Setup(c => c.BeginTransaction())
                .Returns(mockTransaction.Object);

            // Act
            var task = _service.RollbackToVersionAsync(target, "1.1.0");
            await task;

            // Assert
            Assert.True(task.IsCompletedSuccessfully);
        }

        #endregion

        #region Hard Wipe Tests

        [Fact]
        public async Task HardWipeModAsync_ShouldCallReposInCorrectOrder()
        {
            // Arrange
            var mod = new Mod
            {
                Id = Guid.NewGuid(),
                AppId = 1,
                Name = "Test Mod",
                RootSourceUrl = "https://source.com/mod"
            };

            var app = new ModdedApp
            {
                Id = mod.AppId,
                Name = "Test App",
                InstalledVersion = "1.0.0"
            };

            var config = new ModCrawlerConfig
            {
                ModId = mod.Id,
            };

            var mockTransaction = new Mock<IDbTransaction>();

            // Ensure BeginTransaction does not return null
            _connectionMock
                .Setup(c => c.BeginTransaction())
                .Returns(mockTransaction.Object);

            // Act
            await _service.HardWipeModAsync(mod, app, config, "");

            // Assert
            // 1. Verify Snapshot Insertion
            _unusedModRepoMock.Verify(r => r.InsertAsync(
                It.Is<UnusedModHistory>(h => h.ModId == mod.Id && h.AppName == app.Name),
                _connectionMock.Object,
                It.IsAny<IDbTransaction>()), Times.Once);

            // 2. Verify Sub-table Deletions
            _modRepoMock.Verify(r => r.DeleteByModIdAsync(mod.Id, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
            _availableModRepoMock.Verify(r => r.DeleteByModIdAsync(mod.Id, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
            _installedModHistoryRepoMock.Verify(r => r.DeleteByModIdAsync(mod.Id, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);

            // 3. Verify Shell Deletion
            _shellModRepoMock.Verify(r => r.DeleteAsync(mod.Id, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task HardWipeAppAsync_ShouldCallBulkDeleteRepos()
        {
            // Arrange
            int appId = 10;

            var mockTransaction = new Mock<IDbTransaction>();

            // Ensure BeginTransaction does not return null
            _connectionMock
                .Setup(c => c.BeginTransaction())
                .Returns(mockTransaction.Object);

            // Act
            await _service.HardWipeAppAsync(appId);

            // Assert
            // Verify bulk purge across all related tables
            _unusedModRepoMock.Verify(r => r.DeleteByAppIdAsync(appId, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
            _modRepoMock.Verify(r => r.DeleteByAppIdAsync(appId, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
            _availableModRepoMock.Verify(r => r.DeleteByAppIdAsync(appId, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
            _installedModHistoryRepoMock.Verify(r => r.DeleteByAppIdAsync(appId, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
            _modRepoMock.Verify(r => r.DeleteByAppIdAsync(appId, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
            _appRepoMock.Verify(r => r.DeleteAsync(appId, _connectionMock.Object, It.IsAny<IDbTransaction>()), Times.Once);
        }

        #endregion

        #region Hard Wipe & Sync Tests

        [Fact]
        public async Task HardWipeModAsync_ShouldExecuteFullTransaction_AndRollbackOnFailure()
        {
            // Arrange
            var modId = Guid.NewGuid();
            var mod = new Mod { Id = modId, AppId = 1, Name = "Dead Mod" };
            var app = new ModdedApp { Id = 1, Name = "Test Game" };
            var config = new ModCrawlerConfig
            {
                ModId = mod.Id,
            };

            // 1. Mock the Transaction and the Connection's use of it
            var transactionMock = new Mock<IDbTransaction>();
            _connectionMock.Setup(c => c.BeginTransaction()).Returns(transactionMock.Object);

            // 2. Setup the second delete to fail
            _modRepoMock.Setup(r => r.DeleteByModIdAsync(modId, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default))
                .ThrowsAsync(new Exception("Database Crash"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.HardWipeModAsync(mod, app, config, ""));
            Assert.Equal("Database Crash", ex.Message);

            // 3. Verify Rollback was called because of the crash
            transactionMock.Verify(t => t.Rollback(), Times.Once);
        }

        
        
        #endregion

        

    }
}