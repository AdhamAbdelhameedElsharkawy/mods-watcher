using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.ViewModels;
using Moq;
using Xunit;

namespace ModsWatcher.Tests.VMs
{
    public class ModdedAppItemViewModelTests
    {
        private readonly Mock<ILogger<ModdedAppItemViewModel>> _loggerMock;

        public ModdedAppItemViewModelTests()
        {
            _loggerMock = new Mock<ILogger<ModdedAppItemViewModel>>();
        }

        [Fact]
        public void Constructor_ShouldMapEntityProperties()
        {
            // Arrange
            var app = new ModdedApp { Name = "Cyberpunk", InstalledVersion = "2.1" };

            // Act
            var vm = new ModdedAppItemViewModel(app, _loggerMock.Object);

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
            var vm = new ModdedAppItemViewModel(new ModdedApp(), _loggerMock.Object);
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