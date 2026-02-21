using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.Services;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Services.Interfaces;
using Moq;

namespace ModsWatcher.Tests.VMs
{
    public class AppSelectionViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly Mock<IWatcherService> _watcherMock;
        private readonly Mock<IDialogService> _dialogMock;
        private readonly Mock<CommonUtils> _commonUtilsMock;

        public AppSelectionViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _watcherMock = new Mock<IWatcherService>();
            _dialogMock = new Mock<IDialogService>();
            _commonUtilsMock = new Mock<CommonUtils>();

            // Default setup for LoadApps in constructor
            _storageMock.Setup(s => s.GetAllAppSummariesAsync())
                .ReturnsAsync(new List<AppSummaryDto>());
        }

        [Fact]
        public async Task LoadApps_ShouldPopulateCollection_WithMappedViewModels()
        {
            // Arrange
            var summaries = new List<AppSummaryDto>
            {
                new AppSummaryDto { App = new ModdedApp { Name = "Game 1" }, ActiveCount = 5 },
                new AppSummaryDto { App = new ModdedApp { Name = "Game 2" }, ActiveCount = 2 }
            };
            _storageMock.Setup(s => s.GetAllAppSummariesAsync()).ReturnsAsync(summaries);

            // Act
            var vm = new AppSelectionViewModel(_storageMock.Object, _navMock.Object, _watcherMock.Object, _dialogMock.Object, _commonUtilsMock.Object);
            // Since LoadApps is called in ctor, we need to wait for it or trigger it
            await Task.Delay(50); // Small delay for the ctor-initiated task

            // Assert
            Assert.Equal(2, vm.ModdedApps.Count);
            Assert.Equal("Game 1", vm.ModdedApps[0].App.Name);
            Assert.Equal(5, vm.ModdedApps[0].ActiveModsCount);
        }

        [Fact]
        public void SelectAppCommand_ShouldInvokeNavigationService()
        {
            // Arrange
            var vm = new AppSelectionViewModel(_storageMock.Object, _navMock.Object, _watcherMock.Object, _dialogMock.Object, _commonUtilsMock.Object);
            var appItem = new ModdedAppItemViewModel(new ModdedApp { Id = 10, Name = "Test" });

            // Act
            vm.SelectAppCommand.Execute(appItem);

            // Assert
            _navMock.Verify(n => n.NavigateTo<LibraryViewModel, ModdedApp>(
                It.Is<ModdedApp>(a => a.Id == 10)),
                Times.Once);
        }

        [Fact]
        public async Task SyncAppModsCommand_ShouldToggleSyncingState_WhileExecuting()
        {
            // Arrange
            var vm = new AppSelectionViewModel(_storageMock.Object, _navMock.Object, _watcherMock.Object, _dialogMock.Object, _commonUtilsMock.Object);
            var appItem = new ModdedAppItemViewModel(new ModdedApp { Id = 1 });
            var bundle = new List<(Mod, ModCrawlerConfig)> { (new Mod(), new ModCrawlerConfig()) };

            // Use a TaskCompletionSource to control when the watcher "finishes"
            var tcs = new TaskCompletionSource<bool>();

            _storageMock.Setup(s => s.GetWatchableBundleByAppIdAsync(1))
                .ReturnsAsync(bundle);

            // Make the watcher wait until we manually release it
            _watcherMock.Setup(w => w.RunStatusCheckAsync(It.IsAny<IEnumerable<(Mod, ModCrawlerConfig)>>()))
                .Returns(tcs.Task);

            // Act
            var syncTask = ((RelayCommand)vm.SyncAppModsCommand).ExecuteAsync(appItem);

            // Assert: Now it HAS to be true because RunStatusCheckAsync is stuck at tcs.Task
            Assert.True(appItem.IsSyncing, "IsSyncing should be true while the service is running.");

            // Release the service call
            tcs.SetResult(true);
            await syncTask;

            // Assert: Now it should be false
            Assert.False(appItem.IsSyncing, "IsSyncing should be false after completion.");
        }
    }
}