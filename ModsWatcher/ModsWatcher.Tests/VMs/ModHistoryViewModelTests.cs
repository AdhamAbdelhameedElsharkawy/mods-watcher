using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Services;
using ModsWatcher.Services.Config;
using ModsWatcher.Services.Interfaces;
using Moq;

namespace ModsWatcher.Tests.VMs
{
    public class ModHistoryViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<CommonUtils> _commonUtilsMock;
        private readonly Mock<ILogger<ModHistoryViewModel>> _loggerMock;
        private readonly ModHistoryViewModel _vm;
        private readonly Mod _testMod;
        private readonly ModdedApp _testApp;
        private readonly ModItemViewModel _itemViewModel;

        public ModHistoryViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _dialogServiceMock = new Mock<IDialogService>();

            var optionsMock = new Mock<IOptions<WatcherSettings>>();
            optionsMock.Setup(o => o.Value).Returns(new WatcherSettings());
            _commonUtilsMock = new Mock<CommonUtils>(optionsMock.Object);

            _loggerMock = new Mock<ILogger<ModHistoryViewModel>>();

            _vm = new ModHistoryViewModel(
                _navMock.Object,
                _storageMock.Object,
                _dialogServiceMock.Object,
                _commonUtilsMock.Object,
                _loggerMock.Object);

            _testMod = new Mod { Id = Guid.NewGuid(), Name = "UI Overhaul" };
            _testApp = new ModdedApp { Id = 1, InstalledVersion = "2.0" };

            // Fixed: Using the provided constructor signature
            _itemViewModel = new ModItemViewModel(
                _testMod,
                new InstalledMod { Id = _testMod.Id, InstalledVersion = "1.0" },
                new ModCrawlerConfig(),
                _testApp.InstalledVersion,
                _commonUtilsMock.Object,
                new Mock<ILogger<ModItemViewModel>>().Object);
        }

        [Fact]
        public async Task Initialize_ShouldLoadHistoryItems_AndMapNames()
        {
            // Arrange
            var history = new List<InstalledModHistory>
            {
                new InstalledModHistory { ModId = _testMod.Id, Version = "1.0", AppVersion = "2.0" },
                new InstalledModHistory { ModId = _testMod.Id, Version = "0.9", AppVersion = "1.9" }
            };
            _storageMock.Setup(s => s.GetInstalledModHistoryAsync(_testMod.Id)).ReturnsAsync(history);

            // Act
            _vm.Initialize((_itemViewModel, _testApp));
            await Task.Delay(100);

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
            var history = new List<InstalledModHistory>
            {
                new InstalledModHistory { ModId = _testMod.Id, AppVersion = "1.0" }
            };
            _storageMock.Setup(s => s.GetInstalledModHistoryAsync(It.IsAny<Guid>())).ReturnsAsync(history);

            _vm.Initialize((_itemViewModel, _testApp));
            await Task.Delay(100);
            var item = _vm.HistoryItems[0];

            // Act
            _vm.OverrideRollbackRules = true;

            // Assert
            Assert.True(item.CanRollback);
        }

        [Fact]
        public void BackCommand_ShouldNavigateToLibrary()
        {
            // Arrange
            _vm.Initialize((_itemViewModel, _testApp));

            // Act
            _vm.BackCommand.Execute(null);

            // Assert
            // Assert
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, (ModdedApp, ModItemViewModel)>(
                It.Is<(ModdedApp, ModItemViewModel)>(t => t.Item1 == _testApp && t.Item2 == _itemViewModel)),
                Times.Once);
        }
    }
}