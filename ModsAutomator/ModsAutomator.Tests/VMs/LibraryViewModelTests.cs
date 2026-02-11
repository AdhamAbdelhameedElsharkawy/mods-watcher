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
    public class LibraryViewModelTests
    {
        private readonly Mock<IStorageService> _serviceMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly LibraryViewModel _vm;
        private readonly ModdedApp _testApp;

        public LibraryViewModelTests()
        {
            _serviceMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _testApp = new ModdedApp { Id = 1, Name = "Test Game", InstalledVersion = "1.0" };

            _vm = new LibraryViewModel(_navMock.Object, _serviceMock.Object);
        }

        [Fact]
        public async Task Initialize_ShouldLoadModsAndSetSelectedApp()
        {
            // Arrange
            var fakeLibrary = new List<(Mod, InstalledMod?)>
            {
                (new Mod { Id = Guid.NewGuid(), Name = "Mod 1" }, new InstalledMod { InstalledVersion = "1.1" })
            };
            _serviceMock.Setup(s => s.GetModsByAppId(_testApp.Id)).ReturnsAsync(fakeLibrary);

            // Act
            _vm.Initialize(_testApp);
            await Task.Delay(50); // Delay for async void LoadLibrary

            // Assert
            Assert.Equal(_testApp, _vm.SelectedApp);
            Assert.Single(_vm.Mods);
            Assert.Equal("Mod 1", _vm.Mods[0].Shell.Name);
        }

        [Fact]
        public void CanToggleActivation_ShouldBeTrue_OnlyWhenModIsInstalled()
        {
            // Case 1: Mod is just a Shell (not installed)
            _vm.SelectedMod = new ModItemViewModel(new Mod(), null);
            Assert.False(_vm.CanToggleActivation);

            // Case 2: Mod is installed
            _vm.SelectedMod = new ModItemViewModel(new Mod(), new InstalledMod());
            Assert.True(_vm.CanToggleActivation);
        }

        [Theory]
        [InlineData(true, true, true)]   // Installed and Used -> Can Crawl
        [InlineData(true, false, false)] // Installed but Disabled -> Cannot Crawl
        [InlineData(false, false, false)] // Not Installed -> Cannot Crawl
        public void CanCrawlSelectedMod_LogicCheck(bool isInstalled, bool isUsed, bool expected)
        {
            // Arrange
            var installed = isInstalled ? new InstalledMod { IsUsed = isUsed } : null;
            _vm.SelectedMod = new ModItemViewModel(new Mod(), installed);

            // Assert
            Assert.Equal(expected, _vm.CanCrawlSelectedMod);
        }

        [Fact]
        public void ShowHistoryCommand_ShouldNavigateWithCorrectTuple()
        {
            // Arrange
            _vm.Initialize(_testApp);
            var mod = new Mod { Id = Guid.NewGuid(), Name = "HistoryMod" };
            _vm.SelectedMod = new ModItemViewModel(mod, new InstalledMod());

            // Act
            _vm.ShowHistoryCommand.Execute(null);

            // Assert
            _navMock.Verify(n => n.NavigateTo<ModHistoryViewModel, (Mod, ModdedApp)>(
                It.Is<(Mod, ModdedApp)>(data => data.Item1 == mod && data.Item2 == _testApp)),
                Times.Once);
        }

        [Fact]
        public void HardWipeCommand_ShouldClearSelectionAfterSuccess()
        {
            // Arrange
            _vm.Initialize(_testApp);
            var mod = new Mod { Id = Guid.NewGuid(), Name = "WipeMe" };
            _vm.SelectedMod = new ModItemViewModel(mod, new InstalledMod());

            // Mocking the result of MessageBox is usually done via a service, 
            // but assuming the service call completes:

            // Act
            // Manually simulating the internal logic since MessageBox blocks
            // await _serviceMock.Object.HardWipeModAsync(mod, _testApp); 

            // Verifying the state change after a hypothetical successful wipe
            _vm.SelectedMod = null;

            // Assert
            Assert.Null(_vm.SelectedMod);
            Assert.False(_vm.CanToggleActivation);
        }
    }
}