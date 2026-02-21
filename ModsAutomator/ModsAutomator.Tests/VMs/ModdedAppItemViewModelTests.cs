using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.ViewModels;
using Xunit;

namespace ModsWatcher.Tests.VMs
{
    public class ModdedAppItemViewModelTests
    {
        [Fact]
        public void Constructor_ShouldMapEntityProperties()
        {
            // Arrange
            var app = new ModdedApp { Name = "Cyberpunk", InstalledVersion = "2.1" };

            // Act
            var vm = new ModdedAppItemViewModel(app);

            // Assert
            Assert.Equal("Cyberpunk", vm.Name);
            Assert.Equal("2.1", vm.InstalledVersion);
            Assert.Equal(app, vm.App);
        }

        [Theory]
        [InlineData(nameof(ModdedAppItemViewModel.ActiveModsCount), 5)]
        [InlineData(nameof(ModdedAppItemViewModel.PotentialUpdatesCount), 3)]
        [InlineData(nameof(ModdedAppItemViewModel.IsSyncing), true)]
        public void Properties_ShouldNotifyOnChanged(string propertyName, object newValue)
        {
            // Arrange
            var vm = new ModdedAppItemViewModel(new ModdedApp());
            bool wasNotified = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == propertyName) wasNotified = true;
            };

            // Act
            var prop = vm.GetType().GetProperty(propertyName);
            prop.SetValue(vm, newValue);

            // Assert
            Assert.True(wasNotified, $"Property {propertyName} did not trigger PropertyChanged.");
        }
    }
}