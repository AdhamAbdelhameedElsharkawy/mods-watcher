using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using Xunit;

namespace ModsAutomator.Tests.VMs
{
    public class ModItemViewModelTests
    {
        [Fact]
        public void UninstalledMod_ShouldShowCorrectDefaults()
        {
            // Arrange
            var shell = new Mod { Name = "Uninstalled Mod" };

            // Act
            var vm = new ModItemViewModel(shell, null);

            // Assert
            Assert.Equal("Not Installed", vm.Version);
            Assert.Equal("Pending Setup", vm.Summary);
            Assert.False(vm.IsUsed);
        }

        [Fact]
        public void InstalledMod_ShouldShowActiveSummary()
        {
            // Arrange
            var shell = new Mod { Name = "Active Mod" };
            var installed = new InstalledMod
            {
                InstalledVersion = "2.1",
                InstalledSizeMB = 150,
                IsUsed = true
            };

            // Act
            var vm = new ModItemViewModel(shell, installed);

            // Assert
            Assert.Equal("2.1", vm.Version);
            Assert.Equal("150 MB | Active", vm.Summary);
        }

        [Fact]
        public void SettingIsUsed_ShouldRaiseNotificationsForIsUsedAndSummary()
        {
            // Arrange
            var shell = new Mod { Name = "Toggle Mod" };
            var installed = new InstalledMod { IsUsed = false, InstalledSizeMB = 10 };
            var vm = new ModItemViewModel(shell, installed);

            int notificationCount = 0;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.IsUsed) || e.PropertyName == nameof(vm.Summary))
                    notificationCount++;
            };

            // Act
            vm.IsUsed = true;

            // Assert
            Assert.True(installed.IsUsed);
            Assert.Equal(2, notificationCount); // Both properties should notify
            Assert.Contains("Active", vm.Summary);
        }

        [Fact]
        public void Summary_ShouldReflectDisabledStatus()
        {
            // Arrange
            var installed = new InstalledMod { IsUsed = false, InstalledSizeMB = 50 };
            var vm = new ModItemViewModel(new Mod(), installed);

            // Assert
            Assert.Equal("50 MB | Disabled", vm.Summary);
        }
    }
}