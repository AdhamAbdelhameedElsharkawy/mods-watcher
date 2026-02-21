using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Services.Interfaces;
using Moq;

namespace ModsWatcher.Tests.VMs
{
    public class ModShellDialogViewModelTests
    {
        private readonly Mock<IStorageService> _serviceMock;
        private readonly Mock<IDialogService> _dialogMock;
        private readonly Mock<ILogger<ModShellDialogViewModel>> _loggerMock;
        private const int TestAppId = 10;

        public ModShellDialogViewModelTests()
        {
            _serviceMock = new Mock<IStorageService>();
            _dialogMock = new Mock<IDialogService>();
            _loggerMock = new Mock<ILogger<ModShellDialogViewModel>>();
        }

        [Fact]
        public void Constructor_WithNullMod_ShouldSetAddMode()
        {
            // Act
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId, _dialogMock.Object, _loggerMock.Object, null);

            // Assert
            Assert.False(vm.IsEditMode);
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
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId, _dialogMock.Object, _loggerMock.Object, existingMod);

            // Assert
            Assert.True(vm.IsEditMode);
            Assert.Equal("Edit Mod Identity", vm.DialogTitle);
            Assert.Equal("Existing Mod", vm.Name);
        }

        [Fact]
        public void Properties_ShouldUpdateUnderlyingEntity()
        {
            // Arrange
            var vm = new ModShellDialogViewModel(_serviceMock.Object, TestAppId, _dialogMock.Object, _loggerMock.Object);

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
        public async Task SaveCommand_InAddMode_ShouldCallSaveModWithConfigAsync()
        {
            // Arrange
            var vm = new ModShellDialogViewModel(_serviceMock.Object, 10, _dialogMock.Object, _loggerMock.Object); // AppId = 10
            vm.Name = "Brand New Mod";
            vm.RootSourceUrl = "https://nexusmods.com/test";

            // Act
            await ((RelayCommand)vm.SaveCommand).ExecuteAsync(null);

            // Assert
            // Match the new service method signature
            _serviceMock.Verify(s => s.SaveModWithConfigAsync(
                It.Is<Mod>(m => m.Name == "Brand New Mod" && m.AppId == 10),
                It.IsAny<ModCrawlerConfig>()
            ), Times.Once);
        }

        [Fact]
        public async Task SaveCommand_InEditMode_ShouldCallUpdateModWithConfigAsync()
        {
            // Arrange
            var existingMod = new Mod { Id = Guid.NewGuid(), Name = "Old Name", AppId = 1 };
            var existingConfig = new ModCrawlerConfig { ModId = existingMod.Id };

            var vm = new ModShellDialogViewModel(_serviceMock.Object, 1, _dialogMock.Object, _loggerMock.Object, existingMod, existingConfig);
            vm.Name = "Updated Mod Name";
            vm.RootSourceUrl = "https://nexusmods.com/updated";

            // Act
            await ((RelayCommand)vm.SaveCommand).ExecuteAsync(null);

            // Assert: Verify the new unified update method
            _serviceMock.Verify(s => s.UpdateModWithConfigAsync(
                It.Is<Mod>(m => m.Name == "Updated Mod Name"),
                It.IsAny<ModCrawlerConfig>()
            ), Times.Once);
        }
    }
}