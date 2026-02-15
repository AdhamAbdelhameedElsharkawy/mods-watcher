using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;

namespace ModsAutomator.Tests.VMs
{
    public class ModHistoryItemViewModelTests
    {
        [Fact]
        public void IsCompatible_ShouldBeTrue_WhenVersionsMatch()
        {
            // Arrange
            var history = new InstalledModHistory { AppVersion = "1.5.0" };
            var vm = new ModHistoryItemViewModel(history, "1.5.0", () => false);

            // Assert
            Assert.True(vm.IsCompatible);
            Assert.True(vm.CanRollback);
        }

        [Fact]
        public void IsCompatible_ShouldBeFalse_WhenVersionsMismatch()
        {
            // Arrange
            var history = new InstalledModHistory { AppVersion = "1.4.0" };
            var vm = new ModHistoryItemViewModel(history, "1.5.0", () => false);

            // Assert
            Assert.False(vm.IsCompatible);
            Assert.False(vm.CanRollback);
        }

        [Fact]
        public void CanRollback_ShouldBeTrue_WhenMismatchButOverrideIsActive()
        {
            // Arrange
            var history = new InstalledModHistory { AppVersion = "1.4.0" };
            // Simulate the parent "Allow Incompatible" checkbox being checked
            var vm = new ModHistoryItemViewModel(history, "1.5.0", () => true);

            // Assert
            Assert.False(vm.IsCompatible);
            Assert.True(vm.CanRollback);
        }

        [Fact]
        public void RefreshCompatibility_ShouldTriggerPropertyChanged()
        {
            // Arrange
            var vm = new ModHistoryItemViewModel(new InstalledModHistory(), "1.0", () => false);
            bool wasNotified = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.CanRollback)) wasNotified = true;
            };

            // Act
            vm.RefreshCompatibility();

            // Assert
            Assert.True(wasNotified);
        }
    }
}