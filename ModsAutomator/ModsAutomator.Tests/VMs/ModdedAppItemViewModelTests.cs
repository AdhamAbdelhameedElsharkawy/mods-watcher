using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using System.Windows;
using Xunit;

namespace ModsAutomator.Tests.VMs
{
    public class ModdedAppItemViewModelTests
    {
        [Fact]
        public void Properties_ShouldReflectUnderlyingEntity()
        {
            // Arrange
            var app = new ModdedApp { Name = "Elden Ring", InstalledVersion = "1.0.4" };

            // Act
            var vm = new ModdedAppItemViewModel(app);

            // Assert
            Assert.Equal("Elden Ring", vm.Name);
            Assert.Equal("1.0.4", vm.InstalledVersion);
        }

        [Theory]
        [InlineData(0, Visibility.Collapsed)]
        [InlineData(1, Visibility.Visible)]
        [InlineData(99, Visibility.Visible)]
        public void IncompatibleCountVisibility_ShouldToggleBasedOnCount(int count, Visibility expectedVisibility)
        {
            // Arrange
            var vm = new ModdedAppItemViewModel(new ModdedApp());

            // Act
            vm.IncompatibleCount = count;

            // Assert
            Assert.Equal(expectedVisibility, vm.IncompatibleCountVisibility);
        }

        [Fact]
        public void SettingIncompatibleCount_ShouldRaiseNotifyPropertyChanged()
        {
            // Arrange
            var vm = new ModdedAppItemViewModel(new ModdedApp());
            bool wasVisibilityNotified = false;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.IncompatibleCountVisibility))
                    wasVisibilityNotified = true;
            };

            // Act
            vm.IncompatibleCount = 5;

            // Assert
            Assert.True(wasVisibilityNotified, "IncompatibleCountVisibility should notify UI when count changes.");
        }

        [Fact]
        public void Stats_ShouldUpdateCorrect()
        {
            // Arrange
            var vm = new ModdedAppItemViewModel(new ModdedApp());

            // Act
            vm.ActiveModsCount = 10;
            vm.TotalUsedSizeMB = 512.5m;

            // Assert
            Assert.Equal(10, vm.ActiveModsCount);
            Assert.Equal(512.5m, vm.TotalUsedSizeMB);
        }
    }
}