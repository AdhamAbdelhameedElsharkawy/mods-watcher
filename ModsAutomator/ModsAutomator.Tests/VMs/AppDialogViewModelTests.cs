using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using Moq;


namespace ModsAutomator.Tests.VMs
{
    public class AppDialogViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;

        public AppDialogViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
        }

        [Fact]
        public void Constructor_AddMode_ShouldInitializeEmptyApp()
        {
            // Act
            var vm = new AppDialogViewModel(_storageMock.Object, null);

            // Assert
            Assert.False(vm.IsEditMode);
            Assert.True(vm.CanEditName);
            Assert.Equal("0.0.0", vm.App.InstalledVersion);
        }

        [Fact]
        public void Constructor_EditMode_ShouldInitializeExistingApp()
        {
            // Arrange
            var existing = new ModdedApp { Name = "Skyrim", Id = 1 };

            // Act
            var vm = new AppDialogViewModel(_storageMock.Object, existing);

            // Assert
            Assert.True(vm.IsEditMode);
            Assert.False(vm.CanEditName);
            Assert.Equal("Skyrim", vm.Name);
        }

        [Fact]
        public void PropertyChanged_ShouldTriggerOnSetters()
        {
            // Arrange
            var vm = new AppDialogViewModel(_storageMock.Object, null);
            bool wasNotified = false;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(vm.Name)) wasNotified = true; };

            // Act
            vm.Name = "New Game";

            // Assert
            Assert.True(wasNotified);
            Assert.Equal("New Game", vm.App.Name);
        }

        [Fact]
        public async Task SaveCommand_ShouldUpdateLastUpdatedDate_AndCallAddApp()
        {
            // Arrange
            var vm = new AppDialogViewModel(_storageMock.Object, null);
            vm.Name = "Test Game";
            vm.InstalledVersion = "1.0";
            vm.LatestVersion = "1.0";

            // Act - CAST to RelayCommand to use the Task-returning ExecuteAsync
            if (vm.SaveCommand is RelayCommand relayCommand)
            {
                await relayCommand.ExecuteAsync(null);
            }

            // Assert
            _storageMock.Verify(s => s.AddAppAsync(It.IsAny<ModdedApp>()), Times.Once);
            Assert.NotNull(vm.App.LastUpdatedDate);
        }

        [Fact]
        public async Task SaveCommand_InEditMode_ShouldCallUpdateAppAsync()
        {
            // Arrange
            var existing = new ModdedApp { Id = 5, Name = "Old Name", InstalledVersion = "1.0", LatestVersion = "2.0" };
            var vm = new AppDialogViewModel(_storageMock.Object, existing);
            vm.Name = "Updated Name";

            // Act
            // Act - CAST to RelayCommand to use the Task-returning ExecuteAsync
            if (vm.SaveCommand is RelayCommand relayCommand)
            {
                await relayCommand.ExecuteAsync(null);
            }

            // Assert
            _storageMock.Verify(s => s.UpdateAppAsync(It.Is<ModdedApp>(a => a.Id == 5 && a.Name == "Updated Name")), Times.Once);
        }
    }
}