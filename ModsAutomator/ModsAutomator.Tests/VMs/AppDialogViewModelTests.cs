using Moq;
using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using Xunit;

namespace ModsAutomator.Tests.VMs
{
    public class AppDialogViewModelTests
    {
        private readonly Mock<IStorageService> _serviceMock;

        public AppDialogViewModelTests()
        {
            _serviceMock = new Mock<IStorageService>();
        }

        [Fact]
        public void Constructor_WithNullApp_ShouldSetAddMode()
        {
            // Act
            var vm = new AppDialogViewModel(_serviceMock.Object, null);

            // Assert
            Assert.False(vm.IsEditMode);
            Assert.True(vm.CanEditName);
            Assert.NotNull(vm.App);
            Assert.Equal("0.0.0", vm.InstalledVersion);
        }

        [Fact]
        public void Constructor_WithExistingApp_ShouldSetEditMode()
        {
            // Arrange
            var existing = new ModdedApp { Name = "Skyrim", Id = 1 };

            // Act
            var vm = new AppDialogViewModel(_serviceMock.Object, existing);

            // Assert
            Assert.True(vm.IsEditMode);
            Assert.False(vm.CanEditName);
            Assert.Equal("Skyrim", vm.Name);
        }

        [Fact]
        public void PropertyChanged_ShouldFire_WhenNameIsSet()
        {
            // Arrange
            var vm = new AppDialogViewModel(_serviceMock.Object);
            string? changedProperty = null;
            vm.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            // Act
            vm.Name = "New Name";

            // Assert
            Assert.Equal(nameof(vm.Name), changedProperty);
            Assert.Equal("New Name", vm.App.Name);
        }

        [Fact]
        public async Task SaveCommand_InAddMode_ShouldCallAddAppAsync()
        {
            // Arrange
            var vm = new AppDialogViewModel(_serviceMock.Object);
            vm.Name = "New Game";

            // Act
            // Note: This may throw in a unit test runner because of Application.Current.Windows
            // unless the test project is set to UseWPF or Application.Current is mocked.
            try { vm.SaveCommand.Execute(null); } catch { /* Ignore WPF Window loop error */ }

            // Assert
            _serviceMock.Verify(s => s.AddAppAsync(It.Is<ModdedApp>(a => a.Name == "New Game")), Times.Once);
        }

        [Fact]
        public async Task SaveCommand_InEditMode_ShouldCallUpdateAppAsync()
        {
            // Arrange
            var existing = new ModdedApp { Id = 1, Name = "Original" };
            var vm = new AppDialogViewModel(_serviceMock.Object, existing);
            vm.Name = "Updated";

            // Act
            try { vm.SaveCommand.Execute(null); } catch { /* Ignore WPF Window loop error */ }

            // Assert
            _serviceMock.Verify(s => s.UpdateAppAsync(It.Is<ModdedApp>(a => a.Name == "Updated")), Times.Once);
        }
    }
}