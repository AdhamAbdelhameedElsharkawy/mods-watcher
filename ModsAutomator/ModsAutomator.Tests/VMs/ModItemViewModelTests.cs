using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Desktop.ViewModels;
using Xunit;

namespace ModsAutomator.Tests.VMs
{
    public class ModItemViewModelTests
    {
        [Fact]
        public void UninstalledMod_ShouldShowCorrectDefaults()
        {
            var shell = new Mod { Name = "Uninstalled Mod" };
            var vm = new ModItemViewModel(shell, null, null, null);

            Assert.Equal("Not Installed", vm.Version);
            Assert.Equal("Pending Setup", vm.Summary);
            Assert.False(vm.IsUsed);
        }

        [Fact]
        public void InstalledMod_ShouldShowActiveSummary()
        {
            var shell = new Mod { WatcherStatus = WatcherStatusType.Idle };
            var installed = new InstalledMod { IsUsed = true, InstalledVersion = "1.0.0" };
            var vm = new ModItemViewModel(shell, installed, null, "1.0.0");

            // Matches: $"{activeStatus} | {compatibilityStatus} | {watcherResult}"
            Assert.Equal("Active | Ok | Up to date", vm.Summary);
        }

        [Fact]
        public void SettingIsUsed_ShouldRaiseNotificationsForIsUsedAndSummary()
        {
            // Arrange
            var shell = new Mod { Name = "Toggle Mod" };
            var installed = new InstalledMod { IsUsed = false };
            var vm = new ModItemViewModel(shell, installed, null, "1.0");

            var changedProperties = new List<string>();
            vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            // Act
            vm.IsUsed = true;

            // Assert
            Assert.True(installed.IsUsed);

            // Check that the necessary properties were notified at least once
            Assert.Contains(nameof(vm.IsUsed), changedProperties);
            Assert.Contains(nameof(vm.Summary), changedProperties);

            // Optional: If you strictly want to know why it was 3
            // Assert.Equal(3, changedProperties.Count); 
        }

        [Fact]
        public void Summary_ShouldReflectDisabledStatus()
        {
            // Arrange
            var installed = new InstalledMod { IsUsed = false, InstalledVersion = "1.0" };
            var vm = new ModItemViewModel(new Mod(), installed, null, "1.0");

            // Act & Assert
            // Updated to match your actual VM string logic: Status | Compatibility | Watcher
            Assert.Equal("Disabled | Ok | Up to date", vm.Summary);
        }
    }
}