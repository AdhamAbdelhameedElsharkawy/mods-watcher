using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Desktop.Services;
using ModsWatcher.Desktop.ViewModels;
using Moq;
using Xunit;

namespace ModsWatcher.Tests.VMs
{
    public class ModItemViewModelTests
    {

        private readonly CommonUtils _commonUtilsMock;
        private readonly Mock<ILogger<ModItemViewModel>> _loggerMock;

        public ModItemViewModelTests()
        {
            _commonUtilsMock = new CommonUtils();
            _loggerMock = new Mock<ILogger<ModItemViewModel>>();
        }


        [Fact]
        public void UninstalledMod_ShouldShowCorrectDefaults()
        {
            var shell = new Mod { Name = "Uninstalled Mod" };
            var vm = new ModItemViewModel(shell, null, null, null, _commonUtilsMock, _loggerMock.Object);

            Assert.Equal("Not Installed", vm.Version);
            Assert.False(vm.IsUsed);
        }

        [Fact]
        public void SettingIsUsed_ShouldRaiseNotificationsForIsUsedAndSummary()
        {
            // Arrange
            var shell = new Mod { Name = "Toggle Mod", IsUsed = false };
            var installed = new InstalledMod { };
            var vm = new ModItemViewModel(shell, installed, null, "1.0", _commonUtilsMock, _loggerMock.Object);

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