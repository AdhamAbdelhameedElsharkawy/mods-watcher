using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Desktop.ViewModels;
using Moq;
using Xunit;

namespace ModsWatcher.Tests.VMs
{
    public class ModInstallationDialogViewModelTests
    {
        
        private readonly Mock<ILogger<ModInstallationDialogViewModel>> _loggerMock;

        public ModInstallationDialogViewModelTests()
        {
            _loggerMock = new Mock<ILogger<ModInstallationDialogViewModel>>();
        }


        [Fact]
        public void Constructor_ShouldInitializeWithEntity()
        {
            // Arrange
            var entity = new InstalledMod { InstalledVersion = "2.0.0", PackageType = PackageType.Zip };

            // Act
            var vm = new ModInstallationDialogViewModel(entity, _loggerMock.Object);

            // Assert
            Assert.Equal(entity, vm.Entity);
            Assert.Equal("2.0.0", vm.Entity.InstalledVersion);
        }

        [Fact]
        public void PackageTypes_ShouldContainAllEnumValues()
        {
            // Arrange
            var entity = new InstalledMod();
            var vm = new ModInstallationDialogViewModel(entity, _loggerMock.Object);

            // Act
            var types = vm.PackageTypes;

            // Assert
            Assert.Equal(Enum.GetValues(typeof(PackageType)).Length, types.Length);
        }

        [Fact]
        public void Commands_ShouldNotCrash_WhenApplicationCurrentIsNull()
        {
            // Arrange
            // Note: Ensure your VM has the Application.Current?.Windows null-check!
            var vm = new ModInstallationDialogViewModel(new InstalledMod(), _loggerMock.Object);

            // Act & Assert (Verification that logic runs without UI context)
            vm.SaveCommand.Execute(null);
            vm.CancelCommand.Execute(null);
        }
    }
}