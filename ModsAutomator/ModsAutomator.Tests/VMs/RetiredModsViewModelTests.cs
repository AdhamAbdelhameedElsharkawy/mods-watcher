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
    public class RetiredModsViewModelTests
    {
        private readonly Mock<IStorageService> _serviceMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly RetiredModsViewModel _vm;
        private readonly ModdedApp _testApp;

        public RetiredModsViewModelTests()
        {
            _serviceMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _testApp = new ModdedApp { Id = 1, Name = "Skyrim" };

            _vm = new RetiredModsViewModel(_navMock.Object, _serviceMock.Object);
        }

        [Fact]
        public async Task Initialize_ShouldPopulateRetiredMods()
        {
            // Arrange
            var fakeRetired = new List<UnusedModHistory>
            {
                new UnusedModHistory { ModId = Guid.NewGuid(), AppVersion = "1.1.0" },
                new UnusedModHistory { ModId = Guid.NewGuid(), AppVersion = "1.1.1" },
            };
            _serviceMock.Setup(s => s.GetRetiredModsByAppIdAsync(_testApp.Id))
                        .ReturnsAsync(fakeRetired);

            // Act
            _vm.Initialize(_testApp);
            await Task.Delay(50); // Wait for async LoadRetiredMods

            // Assert
            Assert.Equal(2, _vm.RetiredMods.Count);
            Assert.False(_vm.HasNoRetiredMods);
            Assert.Equal("1.1.0", _vm.RetiredMods[0].AppVersion);
        }

        [Fact]
        public async Task HasNoRetiredMods_ShouldBeTrue_WhenCollectionIsEmpty()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetRetiredModsByAppIdAsync(It.IsAny<int>()))
                        .ReturnsAsync(new List<UnusedModHistory>());

            // Act
            _vm.Initialize(_testApp);
            await Task.Delay(50);

            // Assert
            Assert.Empty(_vm.RetiredMods);
            Assert.True(_vm.HasNoRetiredMods);
        }

        [Fact]
        public async Task RestoreCommand_ShouldCallServiceAndRefreshList()
        {
            // Arrange
            var historyItem = new UnusedModHistory { Id = 5, AppVersion = "1.1.0" };
            _serviceMock.Setup(s => s.GetRetiredModsByAppIdAsync(_testApp.Id))
                        .ReturnsAsync(new List<UnusedModHistory> { historyItem });

            _vm.Initialize(_testApp);
            await Task.Delay(50);

            // Mock the next load to return empty (simulating successful restoration/removal from list)
            _serviceMock.Setup(s => s.GetRetiredModsByAppIdAsync(_testApp.Id))
                        .ReturnsAsync(new List<UnusedModHistory>());

            // Act
            await Task.Run(() => _vm.RestoreCommand.Execute(historyItem));
            await Task.Delay(50); // Wait for refresh

            // Assert
            _serviceMock.Verify(s => s.RestoreModFromHistoryAsync(historyItem), Times.Once);
            Assert.True(_vm.HasNoRetiredMods);
        }

        [Fact]
        public void BackCommand_ShouldNavigateToLibrary()
        {
            // Arrange
            _vm.Initialize(_testApp);

            // Act
            _vm.BackCommand.Execute(null);

            // Assert
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, ModdedApp>(_testApp), Times.Once);
        }
    }
}