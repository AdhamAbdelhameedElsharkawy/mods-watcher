using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;

namespace ModsAutomator.Tests.VMs
{
    public class ModGroupViewModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var vm = new ModGroupViewModel();

            // Assert
            Assert.NotNull(vm.AvailableVersions);
            Assert.Empty(vm.AvailableVersions);
        }

        [Fact]
        public void PropertyAssignment_ShouldStoreDataCorrectly()
        {
            // Arrange
            var vm = new ModGroupViewModel();
            var modId = Guid.NewGuid();

            // Act
            vm.ModId = modId;
            vm.ModName = "Script Extender";
            vm.RootSourceUrl = "https://nexusmods.com/...";

            // Assert
            Assert.Equal(modId, vm.ModId);
            Assert.Equal("Script Extender", vm.ModName);
            Assert.Equal("https://nexusmods.com/...", vm.RootSourceUrl);
        }

        [Fact]
        public void AvailableVersions_ShouldAllowAddingItems()
        {
            // Arrange
            var vm = new ModGroupViewModel();
            var version = new AvailableMod { AvailableVersion = "2.0.1", DownloadUrl = "..." };

            // Act
            vm.AvailableVersions.Add(version);

            // Assert
            Assert.Single(vm.AvailableVersions);
            Assert.Equal("2.0.1", vm.AvailableVersions[0].AvailableVersion);
        }
    }
}