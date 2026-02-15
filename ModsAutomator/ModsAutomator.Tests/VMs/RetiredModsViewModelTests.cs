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
        private readonly RetiredModsViewModel _vm;
        private readonly ModdedApp _testApp;

        public RetiredModsViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _vm = new RetiredModsViewModel(_navMock.Object, _storageMock.Object);
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

        [Fact]
        public async Task RestoreCommand_ShouldCallService_AndRefreshList()
        {
            // Arrange
            var itemToRestore = new UnusedModHistory { Name = "To Restore" };
            _vm.Initialize(_testApp);
            _vm.RetiredMods.Add(itemToRestore);

            // Act
            // Using the Hybrid RelayCommand's ExecuteAsync
            await ((RelayCommand)_vm.RestoreCommand).ExecuteAsync(itemToRestore);

            // Assert
            _storageMock.Verify(s => s.RestoreModFromHistoryAsync(itemToRestore), Times.Once);
            _storageMock.Verify(s => s.GetRetiredModsByAppIdAsync(_testApp.Id), Times.AtLeast(2));
        }

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