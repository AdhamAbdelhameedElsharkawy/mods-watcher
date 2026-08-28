using GongSolutions.Wpf.DragDrop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Desktop.Enums;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Services;
using ModsWatcher.Services.Config;
using ModsWatcher.Services.Interfaces;
using Moq;

namespace ModsWatcher.Tests.VMs
{
    public class LibraryViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly Mock<IWatcherService> _watcherMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<CommonUtils> _commonUtilsMock;
        private readonly Mock<ILogger<LibraryViewModel>> _loggerMock;
        private readonly LibraryViewModel _vm;
        private readonly ModdedApp _testApp;
        private readonly Mock<ModItemViewModel> _modItemViewModel;

        public LibraryViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _watcherMock = new Mock<IWatcherService>();
            _dialogServiceMock = new Mock<IDialogService>();
            var optionsMock = new Mock<IOptions<WatcherSettings>>();
            optionsMock.Setup(o => o.Value).Returns(new WatcherSettings { CheckingThresholdHours = 8 });
            _commonUtilsMock = new Mock<CommonUtils>(optionsMock.Object);
            _loggerMock = new Mock<ILogger<LibraryViewModel>>();
            _modItemViewModel = new Mock<ModItemViewModel>();

            // BaseViewModel.Loading is static and required by any path touching it (e.g. Drop)
            BaseViewModel.Initialize(new Mock<ILoadingService>().Object);

            _vm = new LibraryViewModel(_navMock.Object, _storageMock.Object, _watcherMock.Object, _dialogServiceMock.Object, _commonUtilsMock.Object, _loggerMock.Object);
            _testApp = new ModdedApp { Id = 1, Name = "Test App", InstalledVersion = "1.0" };
        }

        [Fact]
        public async Task Initialize_ShouldLoadSortedMods()
        {
            // Arrange
            var data = new List<(Mod Shell, InstalledMod Installed, ModCrawlerConfig Config)>
            {
                (new Mod { Id = Guid.NewGuid(), Name = "Mod B", PriorityOrder = 2 }, null, null),
                (new Mod { Id = Guid.NewGuid(), Name = "Mod A", PriorityOrder = 1 }, null, null)
            };

            _storageMock.Setup(s => s.GetFullModsByAppId(_testApp.Id)).ReturnsAsync(data);
            _storageMock.Setup(s => s.GetModIdsWithAlternativesByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetPackageMainModIdsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetDependencyRolesByAppIdAsync(_testApp.Id)).ReturnsAsync((new HashSet<Guid>(), new HashSet<Guid>()));
            _storageMock.Setup(s => s.GetModIdsWithAvailableVersionsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());

            // Act
            _vm.Initialize((_testApp, null));
            await Task.Delay(10); // Wait for async LoadLibrary

            // Assert
            Assert.Equal(2, _vm.Mods.Count);
            Assert.Equal("Mod A", _vm.Mods[0].Shell.Name); // Verified sorting
            Assert.Equal("Mod B", _vm.Mods[1].Shell.Name);
        }

        [Fact]
        public void SelectedMod_SettingValue_ShouldNotifyDependentProperties()
        {
            // Arrange
            var modItem = new ModItemViewModel(new Mod(), new InstalledMod { IsUsed = true }, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object);
            List<string> changedProps = new();
            _vm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            // Act
            _vm.SelectedMod = modItem;

            // Assert
            Assert.Contains(nameof(_vm.CanToggleActivation), changedProps);
        }

        [Fact]
        public async Task MoveModOrder_ShouldSwapPriority_AndPersistToStorage()
        {
            // Arrange: reordering only works on the "All" filter (see CanReorder)
            _vm.SelectedApp = _testApp;
            _vm.SelectedStateFilter = _vm.StateFilterOptions.First(o => o.Value == ModStateFilter.All);

            var mod1Shell = new Mod { Id = Guid.NewGuid(), Name = "Mod1", PriorityOrder = 0 };
            var mod2Shell = new Mod { Id = Guid.NewGuid(), Name = "Mod2", PriorityOrder = 1 };

            var data = new List<(Mod Shell, InstalledMod Installed, ModCrawlerConfig Config)>
            {
                (mod1Shell, null, null),
                (mod2Shell, null, null)
            };

            _storageMock.Setup(s => s.GetFullModsByAppId(_testApp.Id)).ReturnsAsync(data);
            _storageMock.Setup(s => s.GetModIdsWithAlternativesByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetPackageMainModIdsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetDependencyRolesByAppIdAsync(_testApp.Id)).ReturnsAsync((new HashSet<Guid>(), new HashSet<Guid>()));
            _storageMock.Setup(s => s.GetModIdsWithAvailableVersionsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());

            _vm.Initialize((_testApp, null));
            await Task.Delay(10);

            var mod1 = _vm.Mods.First(m => m.Shell.Id == mod1Shell.Id);

            // Act
            // Using the Hybrid RelayCommand's ExecuteAsync
            await ((RelayCommand)_vm.MoveDownCommand).ExecuteAsync(mod1);

            // Assert
            Assert.Equal(1, mod1Shell.PriorityOrder);
            Assert.Equal(0, mod2Shell.PriorityOrder);
            _storageMock.Verify(s => s.UpdateModShellAsync(It.IsAny<Mod>()), Times.Exactly(2));
            Assert.Equal("Mod2", _vm.Mods[0].Shell.Name); // Re-sorted by new PriorityOrder after reload
            Assert.Equal("Mod1", _vm.Mods[1].Shell.Name);
        }

        [Fact]
        public async Task MoveModOrder_ShouldDoNothing_WhenFilterIsNotAll()
        {
            // Arrange: default filter is Active, not All
            var mod1 = new ModItemViewModel(new Mod { PriorityOrder = 0 }, null, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object);
            var mod2 = new ModItemViewModel(new Mod { PriorityOrder = 1 }, null, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object);
            _vm.Mods.Add(mod1);
            _vm.Mods.Add(mod2);

            // Act
            await ((RelayCommand)_vm.MoveDownCommand).ExecuteAsync(mod1);

            // Assert: nothing moved, nothing persisted
            Assert.Equal(0, mod1.Shell.PriorityOrder);
            Assert.Equal(1, mod2.Shell.PriorityOrder);
            _storageMock.Verify(s => s.UpdateModShellAsync(It.IsAny<Mod>()), Times.Never);
        }

        [Fact]
        public async Task Drop_ShouldRedistributeExistingPriorityOrderSet_NotRenumberFromZero()
        {
            // Arrange: reordering only works on the "All" filter (see CanReorder). Values
            // {0,1,3,4} (a gap at 2) verify Drop redistributes the existing set rather than
            // blindly renumbering from 0 — defensive even though a real gap can't arise
            // under "All" today, since every mod is included there.
            _vm.SelectedStateFilter = _vm.StateFilterOptions.First(o => o.Value == ModStateFilter.All);

            var mods = new List<ModItemViewModel>
            {
                new(new Mod { Name = "A", PriorityOrder = 0 }, null, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object),
                new(new Mod { Name = "B", PriorityOrder = 1 }, null, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object),
                new(new Mod { Name = "C", PriorityOrder = 3 }, null, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object),
                new(new Mod { Name = "D", PriorityOrder = 4 }, null, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object),
            };
            foreach (var mod in mods)
            {
                _vm.Mods.Add(mod);
                _vm.FilteredMods.Add(mod);
            }

            var sourceItem = mods[0]; // "A", currently at index 0

            var dropInfoMock = new Mock<IDropInfo>();
            dropInfoMock.Setup(d => d.Data).Returns(sourceItem);
            dropInfoMock.Setup(d => d.InsertIndex).Returns(3); // drop it at the end

            // Act
            _vm.Drop(dropInfoMock.Object);
            await Task.Delay(10); // Drop is async void

            // Assert: the same 4 values {0,1,3,4} are still in use, just redistributed
            var usedOrders = mods.Select(m => m.Shell.PriorityOrder).OrderBy(x => x).ToList();
            Assert.Equal(new List<int> { 0, 1, 3, 4 }, usedOrders);
        }

        [Fact]
        public void SelectedStateFilter_ShouldDefaultToActive()
        {
            // Assert
            Assert.Equal(ModStateFilter.Active, _vm.SelectedStateFilter.Value);
            Assert.False(_vm.CanReorder);
        }

        [Fact]
        public async Task ApplyStateFilter_ShouldNarrowFilteredMods_ByDependencyRole()
        {
            // Arrange
            var parentMod = new Mod { Id = Guid.NewGuid(), Name = "Parent Mod", IsUsed = true };
            var childMod = new Mod { Id = Guid.NewGuid(), Name = "Child Mod", IsUsed = true };

            var data = new List<(Mod Shell, InstalledMod Installed, ModCrawlerConfig Config)>
            {
                (parentMod, null, null),
                (childMod, null, null)
            };

            _storageMock.Setup(s => s.GetFullModsByAppId(_testApp.Id)).ReturnsAsync(data);
            _storageMock.Setup(s => s.GetModIdsWithAlternativesByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetPackageMainModIdsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetDependencyRolesByAppIdAsync(_testApp.Id))
                .ReturnsAsync((new HashSet<Guid> { parentMod.Id }, new HashSet<Guid> { childMod.Id }));
            _storageMock.Setup(s => s.GetModIdsWithAvailableVersionsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());

            _vm.Initialize((_testApp, null));
            await Task.Delay(10);

            // Act: switch to the Dependency — Parent filter
            _vm.SelectedStateFilter = _vm.StateFilterOptions.First(o => o.Value == ModStateFilter.DependencyParent);

            // Assert
            Assert.Single(_vm.FilteredMods);
            Assert.Equal("Parent Mod", _vm.FilteredMods[0].Shell.Name);

            // Act: switch to the Dependency — Child filter
            _vm.SelectedStateFilter = _vm.StateFilterOptions.First(o => o.Value == ModStateFilter.DependencyChild);

            // Assert
            Assert.Single(_vm.FilteredMods);
            Assert.Equal("Child Mod", _vm.FilteredMods[0].Shell.Name);
        }

        [Fact]
        public async Task ApplyStateFilter_ShouldNarrowFilteredMods_ByWatchableCrawlableAndUpdateAvailable()
        {
            // Arrange
            var watchableMod = new Mod { Id = Guid.NewGuid(), Name = "Watchable Mod", IsUsed = true, IsWatchable = true };
            var crawlableMod = new Mod { Id = Guid.NewGuid(), Name = "Crawlable Mod", IsUsed = true, IsCrawlable = true };
            var updateMod = new Mod { Id = Guid.NewGuid(), Name = "Update Mod", IsUsed = true, WatcherStatus = WatcherStatusType.UpdateFound };
            var plainMod = new Mod { Id = Guid.NewGuid(), Name = "Plain Mod", IsUsed = true };

            var data = new List<(Mod Shell, InstalledMod Installed, ModCrawlerConfig Config)>
            {
                (watchableMod, null, null),
                (crawlableMod, null, null),
                (updateMod, null, null),
                (plainMod, null, null)
            };

            _storageMock.Setup(s => s.GetFullModsByAppId(_testApp.Id)).ReturnsAsync(data);
            _storageMock.Setup(s => s.GetModIdsWithAlternativesByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetPackageMainModIdsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());
            _storageMock.Setup(s => s.GetDependencyRolesByAppIdAsync(_testApp.Id)).ReturnsAsync((new HashSet<Guid>(), new HashSet<Guid>()));
            _storageMock.Setup(s => s.GetModIdsWithAvailableVersionsByAppIdAsync(_testApp.Id)).ReturnsAsync(new HashSet<Guid>());

            _vm.Initialize((_testApp, null));
            await Task.Delay(10);

            // Act / Assert: Watchable
            _vm.SelectedStateFilter = _vm.StateFilterOptions.First(o => o.Value == ModStateFilter.Watchable);
            Assert.Single(_vm.FilteredMods);
            Assert.Equal("Watchable Mod", _vm.FilteredMods[0].Shell.Name);

            // Act / Assert: Crawlable
            _vm.SelectedStateFilter = _vm.StateFilterOptions.First(o => o.Value == ModStateFilter.Crawlable);
            Assert.Single(_vm.FilteredMods);
            Assert.Equal("Crawlable Mod", _vm.FilteredMods[0].Shell.Name);

            // Act / Assert: Update Available
            _vm.SelectedStateFilter = _vm.StateFilterOptions.First(o => o.Value == ModStateFilter.UpdateAvailable);
            Assert.Single(_vm.FilteredMods);
            Assert.Equal("Update Mod", _vm.FilteredMods[0].Shell.Name);
        }

        [Fact]
        public async Task RunFullSync_ShouldClearStaleUpdateFlag_ForNonCrawlableMod_WhenHashUnchanged()
        {
            // Arrange: already flagged from a prior check, and this check's hash comparison
            // finds nothing new (RunStatusCheckAsync leaves the mod untouched — simulated by
            // not mutating it in the mock callback).
            _vm.SelectedApp = _testApp;

            var shell = new Mod
            {
                Id = Guid.NewGuid(),
                Name = "Stale Mod",
                IsCrawlable = false,
                WatcherStatus = WatcherStatusType.UpdateFound,
                LastWatcherHash = "existing-hash",
                LastWatched = DateTime.Now.AddDays(-1)
            };
            var modItem = new ModItemViewModel(shell, null, new ModCrawlerConfig(), "1.0", _commonUtilsMock.Object, _loggerMock.Object);

            _watcherMock.Setup(w => w.RunStatusCheckAsync(It.IsAny<IEnumerable<(Mod, ModCrawlerConfig)>>()))
                .Returns(Task.CompletedTask);

            // Act
            await _vm.RunFullSync(modItem);

            // Assert
            Assert.Equal(WatcherStatusType.Idle, shell.WatcherStatus);
            _storageMock.Verify(s => s.UpdateModShellAsync(It.Is<Mod>(m => m.Id == shell.Id)), Times.AtLeastOnce);
            _dialogServiceMock.Verify(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RunFullSync_ShouldKeepUpdateFlag_ForNonCrawlableMod_WhenHashGenuinelyChanges()
        {
            // Arrange: same starting state as the stale case, but this check's hash
            // comparison finds a real difference — simulated by the mock mutating the hash,
            // the same way RunStatusCheckAsync's real hash-mismatch branch would.
            _vm.SelectedApp = _testApp;

            var shell = new Mod
            {
                Id = Guid.NewGuid(),
                Name = "Freshly Changed Mod",
                IsCrawlable = false,
                WatcherStatus = WatcherStatusType.UpdateFound,
                LastWatcherHash = "existing-hash",
                LastWatched = DateTime.Now.AddDays(-1)
            };
            var modItem = new ModItemViewModel(shell, null, new ModCrawlerConfig(), "1.0", _commonUtilsMock.Object, _loggerMock.Object);

            _watcherMock.Setup(w => w.RunStatusCheckAsync(It.IsAny<IEnumerable<(Mod, ModCrawlerConfig)>>()))
                .Callback(() =>
                {
                    shell.LastWatcherHash = "new-hash";
                    shell.WatcherStatus = WatcherStatusType.UpdateFound;
                })
                .Returns(Task.CompletedTask);

            // Act
            await _vm.RunFullSync(modItem);

            // Assert: a genuinely fresh detection is left flagged, not reset
            Assert.Equal(WatcherStatusType.UpdateFound, shell.WatcherStatus);
        }

        [Fact]
        public void NavToHistory_ShouldPassCorrectTuple()
        {
            // Arrange
            var shell = new Mod { Id = Guid.NewGuid() };
            _vm.SelectedApp = _testApp;
            _vm.SelectedMod = new ModItemViewModel(shell, null, null, "1.0", _commonUtilsMock.Object, _loggerMock.Object);

            // Act
            _vm.ShowHistoryCommand.Execute(null);

            // Assert
            _navMock.Verify(n => n.NavigateTo<ModHistoryViewModel, (ModItemViewModel, ModdedApp)>(
                It.Is<(ModItemViewModel, ModdedApp)>(t => t.Item1.Shell == shell && t.Item2 == _testApp)),
                Times.Once);
        }
    }
}