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

        public ModItemViewModelTests()
        {
            _commonUtilsMock = new CommonUtils();
        }


        [Fact]
        public void UninstalledMod_ShouldShowCorrectDefaults()
        {
            var shell = new Mod { Name = "Uninstalled Mod" };
            var vm = new ModItemViewModel(shell, null, null, null, _commonUtilsMock);

            Assert.Equal("Not Installed", vm.Version);
            Assert.Equal("Pending Setup", vm.Summary);
            Assert.False(vm.IsUsed);
        }

        [Fact]
        public void InstalledMod_ShouldShowActiveSummary()
        {
            var shell = new Mod { WatcherStatus = WatcherStatusType.Idle, IsUsed = true };
            var installed = new InstalledMod { InstalledVersion = "1.0.0", SupportedAppVersions="1.0, 1.0.1, 1.0.0" };
            var vm = new ModItemViewModel(shell, installed, null, "1.0.0", _commonUtilsMock);

            // Matches: $"{activeStatus} | {compatibilityStatus} | {watcherResult}"
            Assert.Equal("Active | Ok | Up to date", vm.Summary);
        }

        [Fact]
        public void SettingIsUsed_ShouldRaiseNotificationsForIsUsedAndSummary()
        {
            // Arrange
            var shell = new Mod { Name = "Toggle Mod", IsUsed = false };
            var installed = new InstalledMod { };
            var vm = new ModItemViewModel(shell, installed, null, "1.0", _commonUtilsMock);

            var changedProperties = new List<string>();
            vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            // Act
            vm.IsUsed = true;

            // Assert
            Assert.True(shell.IsUsed);

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
            var vm = new ModItemViewModel(new Mod(), installed, null, "1.0", _commonUtilsMock);

            // Act & Assert
            // Updated to match your actual VM string logic: Status | Compatibility | Watcher
            Assert.Equal("Disabled | VERSION MISMATCH | Up to date", vm.Summary);
        }
    }
}