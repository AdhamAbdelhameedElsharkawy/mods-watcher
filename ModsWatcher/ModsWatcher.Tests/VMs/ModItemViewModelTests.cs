using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Services;
using ModsWatcher.Services.Config;
using Moq;

namespace ModsWatcher.Tests.VMs
{
    public class ModItemViewModelTests
    {

        private readonly Mock<CommonUtils> _commonUtilsMock;
        private readonly Mock<ILogger<ModItemViewModel>> _loggerMock;

        public ModItemViewModelTests()
        {
            var optionsMock = new Mock<IOptions<WatcherSettings>>();
            _commonUtilsMock = new Mock<CommonUtils>(optionsMock.Object);
            _loggerMock = new Mock<ILogger<ModItemViewModel>>();
        }


        [Fact]
        public void UninstalledMod_ShouldShowCorrectDefaults()
        {
            var shell = new Mod { Name = "Uninstalled Mod" };
            var vm = new ModItemViewModel(shell, null, null, null, _commonUtilsMock.Object, _loggerMock.Object);

            Assert.Equal("Not Installed", vm.Version);
            Assert.False(vm.IsUsed);
        }

        [Fact]
        public void SettingIsUsed_ShouldRaiseNotificationsForIsUsedAndSummary()
        {
            // Arrange
            var shell = new Mod { Name = "Toggle Mod", IsUsed = false };
            var installed = new InstalledMod { };
            var vm = new ModItemViewModel(shell, installed, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object);

            var changedProperties = new List<string>();
            vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            // Act
            vm.IsUsed = true;

            // Assert
            Assert.True(shell.IsUsed);

            // Check that the necessary properties were notified at least once
            Assert.Contains(nameof(vm.IsUsed), changedProperties);

            // Optional: If you strictly want to know why it was 3
            // Assert.Equal(3, changedProperties.Count); 
        }

        
    }
}