using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using Moq;

namespace ModsAutomator.Tests.VMs
{
    public class RetiredModsViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly Mock<IDialogService> _dialogMock;
        private readonly RetiredModsViewModel _vm;
        private readonly ModdedApp _testApp;

        public RetiredModsViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _dialogMock = new Mock<IDialogService>();
            _vm = new RetiredModsViewModel(_navMock.Object, _storageMock.Object, _dialogMock.Object);
            _testApp = new ModdedApp { Id = 1, Name = "Test Game" };
        }

        [Fact]
        public async Task Initialize_ShouldPopulateRetiredMods()
        {
            // Arrange
            var retiredList = new List<UnusedModHistory>
            {
                new UnusedModHistory { Name = "Old Mod 1" },
                new UnusedModHistory { Name = "Old Mod 2" }
            };
            _storageMock.Setup(s => s.GetRetiredModsByAppIdAsync(_testApp.Id))
                        .ReturnsAsync(retiredList);

            // Act
            _vm.Initialize(_testApp);
            await Task.Delay(50); // Wait for async void LoadRetiredMods

            // Assert
            Assert.Equal(2, _vm.RetiredMods.Count);
            Assert.False(_vm.HasNoRetiredMods);
            Assert.Equal("Old Mod 1", _vm.RetiredMods[0].Name);
        }

        //[Fact]
        //public async Task RestoreCommand_ShouldCallService_AndRefreshList()
        //{
        //    // Arrange
        //    var targetVersion = "1.2.3";

        //    // 1. Setup the App context
        //    var parentApp = new ModdedApp { Id = 1, InstalledVersion = targetVersion };

        //    // 2. Setup the History item with matching version
        //    var historyItem = new UnusedModHistory
        //    {
        //        ModId = Guid.NewGuid(),
        //        AppVersion = targetVersion,
        //        Name = "Test Mod"
        //    };

        //    // 3. Setup Mocks
        //    _dialogMock.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()))
        //               .Returns(true);

        //    // Mock the restoration service call
        //    _storageMock.Setup(s => s.RestoreModFromHistoryAsync(It.IsAny<UnusedModHistory>()))
        //                .Returns(Task.CompletedTask);

        //    // 4. Initialize VM (Ensure parentApp is passed/accessible)
        //    var vm = new RetiredModsViewModel(_navMock.Object, _storageMock.Object, _dialogMock.Object);

        //    // Act
        //    vm.RestoreCommand.Execute(historyItem);

        //    // We MUST wait for the async void operation to actually hit the service
        //    // A small delay is the standard way to test 'async void' RelayCommands
        //    await Task.Delay(100);

        //    // Assert
        //    _storageMock.Verify(s => s.RestoreModFromHistoryAsync(historyItem), Times.Once);
        //}
        [Fact]
        public void BackCommand_ShouldNavigateToLibrary()
        {
            // Act
            _vm.Initialize(_testApp);
            _vm.BackCommand.Execute(null);

            // Assert
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, ModdedApp>(_testApp), Times.Once);
        }
    }
}