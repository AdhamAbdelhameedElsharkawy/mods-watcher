using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Interfaces;
using ModsWatcher.Desktop.Services;
using ModsWatcher.Services;
using ModsWatcher.Services.Config;
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
        private readonly Mock<ILogger<StorageService>> _loggerMock;
        private readonly Mock<IModDependencyRepository> _modDepMock;

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
            var optionsMock = new Mock<IOptions<WatcherSettings>>();
            _commonUtilsMock = new Mock<CommonUtils>(optionsMock.Object);
            _loggerMock = new Mock<ILogger<StorageService>>();
            _modDepMock = new Mock<IModDependencyRepository>();



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
                _modDepMock.Object,
                _commonUtilsMock.Object,
                _loggerMock.Object
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

        #region Mod Dependency Tests

        [Fact]
        public async Task WouldCreateCircularDependencyAsync_ShouldReturnTrue_WhenProposedParentAlreadyDependsOnDependentThroughChain()
        {
            // Arrange: modA -> modB -> modC (modA depends on modB, modB depends on modC).
            // Proposing that modC depends on modA would close the loop: A -> B -> C -> A.
            var modA = Guid.NewGuid();
            var modB = Guid.NewGuid();
            var modC = Guid.NewGuid();

            _modDepMock.Setup(r => r.GetAllAncestorsAsync(modA, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>
                {
                    new() { DependentModId = modA, ParentModId = modB },
                    new() { DependentModId = modB, ParentModId = modC }
                });

            // Act
            var result = await _service.WouldCreateCircularDependencyAsync(modC, modA);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WouldCreateCircularDependencyAsync_ShouldReturnFalse_WhenNoCycleExists()
        {
            // Arrange
            var dependentModId = Guid.NewGuid();
            var parentModId = Guid.NewGuid();

            _modDepMock.Setup(r => r.GetAllAncestorsAsync(parentModId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>());

            // Act
            var result = await _service.WouldCreateCircularDependencyAsync(dependentModId, parentModId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task WouldCreateCircularDependencyAsync_ShouldReturnFalse_ForADiamondShapedDependency()
        {
            // Arrange: modD depends on modX, and modX depends on modP.
            // Proposing that modD ALSO depends directly on modP is a diamond
            // shape, not a cycle, so it must be allowed.
            var modD = Guid.NewGuid();
            var modP = Guid.NewGuid();

            _modDepMock.Setup(r => r.GetAllAncestorsAsync(modP, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>());

            // Act
            var result = await _service.WouldCreateCircularDependencyAsync(modD, modP);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddDependencyAsync_ShouldThrow_WhenModDependsOnItself()
        {
            // Arrange
            var modId = Guid.NewGuid();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.AddDependencyAsync(modId, modId));

            Assert.Contains("cannot depend on itself", ex.Message, StringComparison.OrdinalIgnoreCase);
            _modDepMock.Verify(r => r.AddAsync(It.IsAny<ModDependency>(), It.IsAny<IDbConnection>(), null, default), Times.Never);
        }

        [Fact]
        public async Task AddDependencyAsync_ShouldThrow_WhenItWouldCreateACircularReference()
        {
            // Arrange: modA -> modB -> modC (modA depends on modB, modB depends on modC).
            // Adding "modC depends on modA" would close the loop: A -> B -> C -> A.
            var modA = Guid.NewGuid();
            var modB = Guid.NewGuid();
            var modC = Guid.NewGuid();

            _modDepMock.Setup(r => r.GetAllAncestorsAsync(modA, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>
                {
                    new() { DependentModId = modA, ParentModId = modB },
                    new() { DependentModId = modB, ParentModId = modC }
                });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.AddDependencyAsync(modC, modA));

            Assert.Contains("circular", ex.Message, StringComparison.OrdinalIgnoreCase);
            _modDepMock.Verify(r => r.AddAsync(It.IsAny<ModDependency>(), It.IsAny<IDbConnection>(), null, default), Times.Never);
        }

        [Fact]
        public async Task AddDependencyAsync_ShouldInsertRelation_WhenValid()
        {
            // Arrange
            var dependentModId = Guid.NewGuid();
            var parentModId = Guid.NewGuid();

            _modDepMock.Setup(r => r.GetAllAncestorsAsync(parentModId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>());

            // Act
            await _service.AddDependencyAsync(dependentModId, parentModId);

            // Assert
            _modDepMock.Verify(r => r.AddAsync(
                It.Is<ModDependency>(d => d.DependentModId == dependentModId && d.ParentModId == parentModId),
                It.IsAny<IDbConnection>(),
                null,
                default),
                Times.Once);
        }

        [Fact]
        public async Task RemoveDependencyAsync_ShouldCallDelete_WithCorrectIds()
        {
            // Arrange
            var dependentModId = Guid.NewGuid();
            var parentModId = Guid.NewGuid();

            // Act
            await _service.RemoveDependencyAsync(dependentModId, parentModId);

            // Assert
            _modDepMock.Verify(r => r.DeleteAsync(dependentModId, parentModId, It.IsAny<IDbConnection>(), null, default), Times.Once);
        }

        [Fact]
        public async Task GetDependencyImpactTreeAsync_ShouldReturnNull_WhenModHasNoDependents()
        {
            // Arrange
            var parentModId = Guid.NewGuid();
            _modDepMock.Setup(r => r.GetDependentsAsync(parentModId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>());

            // Act
            var result = await _service.GetDependencyImpactTreeAsync(parentModId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDependencyImpactTreeAsync_ShouldBuildMultiLevelTree_FromDescendants()
        {
            // Arrange: Root -> Child -> Grandchild
            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var grandchildId = Guid.NewGuid();

            _modDepMock.Setup(r => r.GetDependentsAsync(rootId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency> { new() { DependentModId = childId, ParentModId = rootId } });

            _modDepMock.Setup(r => r.GetAllDescendantsAsync(rootId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>
                {
                    new() { DependentModId = childId, ParentModId = rootId },
                    new() { DependentModId = grandchildId, ParentModId = childId }
                });

            _shellModRepoMock.Setup(r => r.GetByIdAsync(rootId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new Mod { Id = rootId, Name = "Root Mod" });
            _shellModRepoMock.Setup(r => r.GetByIdAsync(childId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new Mod { Id = childId, Name = "Child Mod" });
            _shellModRepoMock.Setup(r => r.GetByIdAsync(grandchildId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new Mod { Id = grandchildId, Name = "Grandchild Mod" });

            // Act
            var tree = await _service.GetDependencyImpactTreeAsync(rootId);

            // Assert
            Assert.NotNull(tree);
            Assert.Equal("Root Mod", tree!.ModName);
            Assert.Single(tree.Children);

            var child = tree.Children[0];
            Assert.Equal("Child Mod", child.ModName);
            Assert.Single(child.Children);

            var grandchild = child.Children[0];
            Assert.Equal("Grandchild Mod", grandchild.ModName);
            Assert.Empty(grandchild.Children);
        }

        [Fact]
        public async Task GetDependencyForestByAppIdAsync_ShouldGroupModsUnderRootsWithNoParents()
        {
            // Arrange: App has 3 mods. Root -> Leaf, and an unrelated standalone mod.
            int appId = 5;
            var rootId = Guid.NewGuid();
            var leafId = Guid.NewGuid();
            var standaloneId = Guid.NewGuid();

            var mods = new List<Mod>
            {
                new() { Id = rootId, Name = "Root Mod", AppId = appId },
                new() { Id = leafId, Name = "Leaf Mod", AppId = appId },
                new() { Id = standaloneId, Name = "Standalone Mod", AppId = appId }
            };

            _shellModRepoMock.Setup(r => r.GetByAppIdAsync(appId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(mods);

            _modDepMock.Setup(r => r.GetAllByAppIdAsync(appId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>
                {
                    new() { DependentModId = leafId, ParentModId = rootId }
                });

            // Act
            var forest = (await _service.GetDependencyForestByAppIdAsync(appId)).ToList();

            // Assert: two roots (Root Mod with a child, Standalone Mod with none)
            Assert.Equal(2, forest.Count);

            var rootNode = forest.Single(n => n.ModName == "Root Mod");
            Assert.Single(rootNode.Children);
            Assert.Equal("Leaf Mod", rootNode.Children[0].ModName);

            var standaloneNode = forest.Single(n => n.ModName == "Standalone Mod");
            Assert.Empty(standaloneNode.Children);
        }

        [Fact]
        public async Task GetDependencyForestByAppIdAsync_ShouldReturnEmpty_WhenAppHasNoMods()
        {
            // Arrange
            int appId = 7;
            _shellModRepoMock.Setup(r => r.GetByAppIdAsync(appId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<Mod>());
            _modDepMock.Setup(r => r.GetAllByAppIdAsync(appId, It.IsAny<IDbConnection>(), null, default))
                .ReturnsAsync(new List<ModDependency>());

            // Act
            var forest = await _service.GetDependencyForestByAppIdAsync(appId);

            // Assert
            Assert.Empty(forest);
        }

        #endregion

    }
}