using Moq;
using ModsAutomator.Services.Interfaces;
using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Desktop.Interfaces;
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

            // Mocking the Data Loading
            var fakeData = new List<AppSummaryDto>
            {
                new AppSummaryDto { App = new ModdedApp { Id = 1, Name = "Test App" }, ActiveCount = 5 }
            };
            _serviceMock.Setup(s => s.GetAllAppSummariesAsync()).ReturnsAsync(fakeData);

            _vm = new AppSelectionViewModel(_serviceMock.Object, _navMock.Object);
        }

        [Fact]
        public async Task LoadApps_ShouldPopulateModdedAppsCollection()
        {
            // Since LoadApps is 'async void', it runs on construction. 
            // In tests, we might need a tiny delay or to call it manually if it wasn't called in constructor.

            // Allow a small delay for the async void to complete
            await Task.Delay(50);

            Assert.Single(_vm.ModdedApps);
            Assert.Equal("Test App", _vm.ModdedApps[0].App.Name);
            Assert.Equal(5, _vm.ModdedApps[0].ActiveModsCount);
        }

        [Fact]
        public void SelectAppCommand_ShouldTriggerNavigation()
        {
            // Arrange
            var item = new ModdedAppItemViewModel(new ModdedApp { Name = "Skyrim" });

            // Act
            _vm.SelectAppCommand.Execute(item);

            // Assert
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, ModdedApp>(item.App), Times.Once);
        }
    }
}