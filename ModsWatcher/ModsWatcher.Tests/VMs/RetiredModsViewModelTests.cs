using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Services.Interfaces;
using Moq;
using Xunit;

namespace ModsWatcher.Tests.VMs
{
    public class RetiredModsViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly Mock<IDialogService> _dialogMock;
        private readonly Mock<ILogger<RetiredModsViewModel>> _loggerMock;
        private readonly RetiredModsViewModel _vm;
        private readonly ModdedApp _testApp;

        public RetiredModsViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _dialogMock = new Mock<IDialogService>();
            _loggerMock = new Mock<ILogger<RetiredModsViewModel>>();

            _vm = new RetiredModsViewModel(_navMock.Object, _storageMock.Object, _dialogMock.Object, _loggerMock.Object);
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
            // Updated to use the tuple initialization but passing null for the VM to avoid proxy hell
            _vm.Initialize((_testApp, null));
            await Task.Delay(100);

            // Assert
            Assert.Equal(2, _vm.RetiredMods.Count);
            Assert.False(_vm.HasNoRetiredMods);
            Assert.Equal("Old Mod 1", _vm.RetiredMods[0].Name);
        }

        [Fact]
        public void BackCommand_ShouldNavigateToLibrary()
        {
            // Act
            _vm.Initialize((_testApp, null));
            _vm.BackCommand.Execute(null);

            // Assert
            // Verifies it navigates back with the App and a null ModItemViewModel
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, (ModdedApp, ModItemViewModel)>(
                It.Is<(ModdedApp App, ModItemViewModel Mod)>(data =>
                    data.App == _testApp && data.Mod == null)
                ), Times.Once);
        }

        [Fact]
        public async Task RestoreCommand_ShouldCallService_AndRefreshList()
        {
            // Arrange
            var historyItem = new UnusedModHistory { Name = "Test Mod", ModId = Guid.NewGuid() };
            _vm.Initialize((_testApp, null));

            _dialogMock.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(true);

            _storageMock.Setup(s => s.RestoreModFromHistoryAsync(It.IsAny<UnusedModHistory>()))
                        .Returns(Task.CompletedTask);

            // Act
            _vm.RestoreCommand.Execute(historyItem);
            await Task.Delay(100);

            // Assert
            _storageMock.Verify(s => s.RestoreModFromHistoryAsync(historyItem), Times.Once);
            _storageMock.Verify(s => s.GetRetiredModsByAppIdAsync(_testApp.Id), Times.AtLeast(2));
        }
    }
}