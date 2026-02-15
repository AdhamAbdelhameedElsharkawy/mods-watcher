using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using Moq;

namespace ModsAutomator.Tests.VMs
{
    public class ModHistoryViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly ModHistoryViewModel _vm;
        private readonly Mod _testMod;
        private readonly ModdedApp _testApp;

        public ModHistoryViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _vm = new ModHistoryViewModel(_navMock.Object, _storageMock.Object, _dialogServiceMock.Object);

            _testMod = new Mod { Id = Guid.NewGuid(), Name = "UI Overhaul" };
            _testApp = new ModdedApp { Id = 1, InstalledVersion = "2.0" };
        }

        [Fact]
        public async Task Initialize_ShouldLoadHistoryItems_AndMapNames()
        {
            // Arrange
            var history = new List<InstalledModHistory>
            {
                new InstalledModHistory { Version = "1.0", AppVersion = "2.0" },
                new InstalledModHistory { Version = "0.9", AppVersion = "1.9" }
            };
            _storageMock.Setup(s => s.GetInstalledModHistoryAsync(_testMod.Id)).ReturnsAsync(history);

            // Act
            _vm.Initialize((_testMod, _testApp));
            await Task.Delay(50); // Wait for async void LoadHistory

            // Assert
            Assert.Equal("UI Overhaul", _vm.SelectedModName);
            Assert.Equal(2, _vm.HistoryItems.Count);
            Assert.True(_vm.HasHistory);
            Assert.True(_vm.HistoryItems[0].IsCompatible);  // 2.0 == 2.0
            Assert.False(_vm.HistoryItems[1].IsCompatible); // 1.9 != 2.0
        }

        [Fact]
        public async Task OverrideRollbackRules_WhenChanged_ShouldRefreshAllItems()
        {
            // Arrange
            var history = new List<InstalledModHistory> { new InstalledModHistory { AppVersion = "1.0" } };
            _storageMock.Setup(s => s.GetInstalledModHistoryAsync(It.IsAny<Guid>())).ReturnsAsync(history);

            _vm.Initialize((_testMod, _testApp));
            await Task.Delay(10);
            var item = _vm.HistoryItems[0];

            // Act
            _vm.OverrideRollbackRules = true;

            // Assert
            // Even if incompatible (1.0 vs 2.0), the override should now allow rollback
            Assert.True(item.CanRollback);
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
    }
}