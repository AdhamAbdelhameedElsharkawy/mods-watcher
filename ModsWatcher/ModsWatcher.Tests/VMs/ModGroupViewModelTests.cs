using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.ViewModels;

namespace ModsWatcher.Tests.VMs
{
    public class ModGroupViewModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var vm = new ModVersionGroupViewModel();

            // Assert
            Assert.NotNull(vm.Versions);
            Assert.Empty(vm.Versions);
        }

        [Fact]
        public void PropertyAssignment_ShouldStoreDataCorrectly()
        {
            // Arrange
            var vm = new ModVersionGroupViewModel();
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

        //[Fact]
        //public void AvailableVersions_ShouldAllowAddingItems()
        //{
        //    // Arrange
        //    var vm = new ModVersionGroupViewModel();
        //    var version = new AvailableMod { AvailableVersion = "2.0.1", DownloadUrl = "..." };

        //    // Act
        //    vm.Versions.Add(version);

        //    // Assert
        //    Assert.Single(vm.Versions);
        //    Assert.Equal("2.0.1", vm.Versions[0].AvailableVersion);
        //}
    }
}