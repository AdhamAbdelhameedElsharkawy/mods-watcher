using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using Xunit;

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
        }

        [Fact]
        public void IsCompatible_ShouldBeFalse_WhenVersionsMismatch()
        {
            // Arrange
            var history = new InstalledModHistory { AppVersion = "1.0.0" };
            var vm = new ModHistoryItemViewModel(history, "1.5.0", () => false);

            // Assert
            Assert.False(vm.IsCompatible);
        }

        [Theory]
        [InlineData(true, false, true)]  // Matches, No Override -> True
        [InlineData(false, true, true)]  // Mismatch, Override ON -> True
        [InlineData(false, false, false)] // Mismatch, Override OFF -> False
        public void CanRollback_LogicCheck(bool matches, bool overrideActive, bool expected)
        {
            // Arrange
            var historyVersion = matches ? "1.0" : "2.0";
            var history = new InstalledModHistory { AppVersion = historyVersion };

            var vm = new ModHistoryItemViewModel(history, "1.0", () => overrideActive);

            // Assert
            Assert.Equal(expected, vm.CanRollback);
        }

        [Fact]
        public void RefreshCompatibility_ShouldRaisePropertyChanged()
        {
            // Arrange
            var history = new InstalledModHistory();
            var vm = new ModHistoryItemViewModel(history, "1.0", () => false);
            bool notified = false;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(vm.CanRollback)) notified = true; };

            // Act
            vm.RefreshCompatibility();

            // Assert
            Assert.True(notified);
        }
    }
}