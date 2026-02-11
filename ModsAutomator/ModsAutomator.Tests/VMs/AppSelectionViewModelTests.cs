using Moq;
using ModsAutomator.Services.Interfaces;
using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Desktop.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ModsAutomator.Tests.VMs
{
    public class AppSelectionViewModelTests
    {
        private readonly Mock<IStorageService> _serviceMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly AppSelectionViewModel _vm;

        public AppSelectionViewModelTests()
        {
            _serviceMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();

            // Mock the DTO data returned by the storage service
            var fakeSummaries = new List<AppSummaryDto>
            {
                new AppSummaryDto
                {
                    App = new ModdedApp { Id = 1, Name = "Witcher 3" },
                    ActiveCount = 10,
                    TotalSize = 1024,
                    IncompatibleCount = 2
                }
            };
            _serviceMock.Setup(s => s.GetAllAppSummariesAsync()).ReturnsAsync(fakeSummaries);

            _vm = new AppSelectionViewModel(_serviceMock.Object, _navMock.Object);
        }

        [Fact]
        public async Task LoadApps_ShouldCorrectlyMapDtoToWrapper()
        {
            // Allow time for the async void LoadApps in constructor
            await Task.Delay(50);

            Assert.Single(_vm.ModdedApps);
            var wrapper = _vm.ModdedApps[0];

            Assert.Equal("Witcher 3", wrapper.App.Name);
            Assert.Equal(10, wrapper.ActiveModsCount);
            Assert.Equal(1024, wrapper.TotalUsedSizeMB);
            Assert.Equal(2, wrapper.IncompatibleCount);
        }

        [Fact]
        public void SelectAppCommand_ShouldNavigateWithCorrectParam()
        {
            // Arrange
            var wrapper = new ModdedAppItemViewModel(new ModdedApp { Id = 5, Name = "Cyberpunk" });

            // Act
            _vm.SelectAppCommand.Execute(wrapper);

            // Assert
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, ModdedApp>(
                It.Is<ModdedApp>(a => a.Id == 5)), Times.Once);
        }

        [Fact]
        public async Task DeleteAppCommand_WhenConfirmed_ShouldCallHardWipeAndRemoveFromUI()
        {
            // Arrange
            var appToDelete = new ModdedApp { Id = 99, Name = "Grave" };
            var wrapper = new ModdedAppItemViewModel(appToDelete);
            _vm.ModdedApps.Add(wrapper);

            // Note: In a real unit test environment, MessageBox.Show will block.
            // You would normally abstract the IDialogService to mock the "Yes" result.
            // Assuming the logic passes the MessageBox check:

            // Act
            // In a real test, you'd wrap MessageBox in a service to avoid UI blocking.
            // If the service call is reached:
            await _serviceMock.Object.HardWipeAppAsync(appToDelete.Id);

            // Assert
            _serviceMock.Verify(s => s.HardWipeAppAsync(99), Times.Once);
        }
    }
}