using Moq;
using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ModsAutomator.Tests.VMs
{
    public class ModShellDialogViewModelTests
    {
        private readonly Mock<IStorageService> _serviceMock;
        private const int TestAppId = 10;

        public ModShellDialogViewModelTests()
        {
            _serviceMock = new Mock<IStorageService>();
        }

        [Fact]
        public void Constructor_WithNullMod_ShouldSetAddMode()
        {
            // Act
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId, null);

            // Assert
            Assert.False(vm.IsEditMode);
            Assert.True(vm.CanEditName);
            Assert.Equal("Register New Mod", vm.DialogTitle);
            Assert.Equal(TestAppId, vm.Shell.AppId);
            Assert.NotEqual(Guid.Empty, vm.Shell.Id);
        }

        [Fact]
        public void Constructor_WithExistingMod_ShouldSetEditMode()
        {
            // Arrange
            var existingMod = new Mod { Id = Guid.NewGuid(), Name = "Existing Mod", AppId = TestAppId };

            // Act
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId, existingMod);

            // Assert
            Assert.True(vm.IsEditMode);
            Assert.False(vm.CanEditName);
            Assert.Equal("Edit Mod Identity", vm.DialogTitle);
            Assert.Equal("Existing Mod", vm.Name);
        }

        [Fact]
        public void Properties_ShouldUpdateUnderlyingEntity()
        {
            // Arrange
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId);

            // Act
            vm.Name = "New Texture Mod";
            vm.RootSourceUrl = "https://nexusmods.com/test";
            vm.Description = "A cool description";

            // Assert
            Assert.Equal("New Texture Mod", vm.Shell.Name);
            Assert.Equal("https://nexusmods.com/test", vm.Shell.RootSourceUrl);
            Assert.Equal("A cool description", vm.Shell.Description);
        }

        [Fact]
        public async Task SaveCommand_InAddMode_ShouldCallAddModShellAsync()
        {
            // Arrange
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId);
            vm.Name = "Brand New Mod";

            // Act
            try { vm.SaveCommand.Execute(null); } catch { /* Ignore WPF Window loop */ }

            // Assert
            _serviceMock.Verify(s => s.AddModShellAsync(It.Is<Mod>(m => m.Name == "Brand New Mod" && m.AppId == TestAppId)), Times.Once);
        }

        [Fact]
        public async Task SaveCommand_InEditMode_ShouldCallUpdateModShellAsync()
        {
            // Arrange
            var mod = new Mod { Id = Guid.NewGuid(), Name = "Old Mod", AppId = TestAppId };
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId, mod);
            vm.Name = "Updated Mod Name";

            // Act
            try { vm.SaveCommand.Execute(null); } catch { /* Ignore WPF Window loop */ }

            // Assert
            _serviceMock.Verify(s => s.UpdateModShellAsync(It.Is<Mod>(m => m.Name == "Updated Mod Name")), Times.Once);
        }
    }
}