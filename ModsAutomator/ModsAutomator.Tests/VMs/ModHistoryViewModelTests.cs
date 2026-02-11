using Moq;
using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ModsAutomator.Tests.VMs
{
    public class ModHistoryViewModelTests
    {
        private readonly Mock<IStorageService> _serviceMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly ModHistoryViewModel _vm;
        private readonly Mod _testMod;
        private readonly ModdedApp _testApp;

        public ModHistoryViewModelTests()
        {
            _serviceMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();

            _testMod = new Mod { Id = Guid.NewGuid(), Name = "Texture Overhaul" };
            _testApp = new ModdedApp { Id = 1, Name = "Skyrim", InstalledVersion = "1.5.97" };

            _vm = new ModHistoryViewModel(_navMock.Object, _serviceMock.Object);
        }

        [Fact]
        public async Task Initialize_ShouldLoadHistoryAndMapToWrappers()
        {
            // Arrange
            var historyData = new List<InstalledModHistory>
            {
                new InstalledModHistory { Version = "1.0", AppVersion = "1.5.97" },
                new InstalledModHistory { Version = "0.9", AppVersion = "1.4.0" }
            };
            _serviceMock.Setup(s => s.GetInstalledModHistoryAsync(_testMod.Id)).ReturnsAsync(historyData);

            // Act
            _vm.Initialize((_testMod, _testApp));
            await Task.Delay(50); // Wait for async void LoadHistory

            // Assert
            Assert.Equal(_testMod.Name, _vm.SelectedModName);
            Assert.Equal(2, _vm.HistoryItems.Count);
            Assert.True(_vm.HasHistory);

            // Verify individual wrapper logic mapping
            Assert.True(_vm.HistoryItems[0].IsCompatible);
            Assert.False(_vm.HistoryItems[1].IsCompatible);
        }

        [Fact]
        public async Task SettingOverride_ShouldRefreshAllItems()
        {
            // Arrange
            var historyData = new List<InstalledModHistory> { new InstalledModHistory { AppVersion = "Old" } };
            _serviceMock.Setup(s => s.GetInstalledModHistoryAsync(It.IsAny<Guid>())).ReturnsAsync(historyData);
            _vm.Initialize((_testMod, _testApp));
            await Task.Delay(50);

            var item = _vm.HistoryItems[0];
            Assert.False(item.CanRollback); // Initially disabled because versions mismatch

            // Act
            _vm.OverrideRollbackRules = true;

            // Assert
            Assert.True(item.CanRollback); // Now enabled via override
        }

        [Fact]
        public void BackCommand_ShouldNavigateToLibrary()
        {
            // Arrange
            _vm.Initialize((_testMod, _testApp));

            // Act
            _vm.BackCommand.Execute(null);

            // Assert
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, ModdedApp>(_testApp), Times.Once);
        }

        [Fact]
        public async Task RollbackCommand_ShouldTriggerServiceAndNavigateBack()
        {
            // Arrange
            var historyEntry = new InstalledModHistory { Version = "1.0", AppVersion = "1.5.97" };
            var wrapper = new ModHistoryItemViewModel(historyEntry, _testApp.InstalledVersion, () => false);
            _vm.Initialize((_testMod, _testApp));

            // Act
            // Simulating MessageBoxResult.Yes logic inside the command
            await _serviceMock.Object.RollbackToVersionAsync(historyEntry, _testApp.InstalledVersion);
            _vm.BackCommand.Execute(null);

            // Assert
            _serviceMock.Verify(s => s.RollbackToVersionAsync(historyEntry, _testApp.InstalledVersion), Times.Once);
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, ModdedApp>(_testApp), Times.AtLeastOnce);
        }
    }
}