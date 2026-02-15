using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using Moq;

namespace ModsAutomator.Tests.VMs
{
    public class LibraryViewModelTests
    {
        private readonly Mock<IStorageService> _storageMock;
        private readonly Mock<INavigationService> _navMock;
        private readonly Mock<IWatcherService> _watcherMock;
        private readonly LibraryViewModel _vm;
        private readonly ModdedApp _testApp;

        public LibraryViewModelTests()
        {
            _storageMock = new Mock<IStorageService>();
            _navMock = new Mock<INavigationService>();
            _watcherMock = new Mock<IWatcherService>();

            _vm = new LibraryViewModel(_navMock.Object, _storageMock.Object, _watcherMock.Object);
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

            // Act
            _vm.Initialize(_testApp);
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
            var modItem = new ModItemViewModel(new Mod(), new InstalledMod { IsUsed = true }, null, "1.0");
            List<string> changedProps = new();
            _vm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            // Act
            _vm.SelectedMod = modItem;

            // Assert
            Assert.Contains(nameof(_vm.CanToggleActivation), changedProps);
            Assert.Contains(nameof(_vm.CanCrawlSelectedMod), changedProps);
            Assert.True(_vm.CanCrawlSelectedMod);
        }

        [Fact]
        public async Task MoveModOrder_ShouldSwapPriority_AndPersistToStorage()
        {
            // Arrange
            var mod1 = new ModItemViewModel(new Mod { PriorityOrder = 0 }, null, null, "1.0");
            var mod2 = new ModItemViewModel(new Mod { PriorityOrder = 1 }, null, null, "1.0");
            _vm.Mods.Add(mod1);
            _vm.Mods.Add(mod2);

            // Act
            // Using the Hybrid RelayCommand's ExecuteAsync
            await ((RelayCommand)_vm.MoveDownCommand).ExecuteAsync(mod1);

            // Assert
            Assert.Equal(1, mod1.Shell.PriorityOrder);
            Assert.Equal(0, mod2.Shell.PriorityOrder);
            _storageMock.Verify(s => s.UpdateModShellAsync(It.IsAny<Mod>()), Times.Exactly(2));
            Assert.Equal(mod1, _vm.Mods[1]); // Verified UI collection swap
        }

        [Fact]
        public void NavToHistory_ShouldPassCorrectTuple()
        {
            // Arrange
            var shell = new Mod { Id = Guid.NewGuid() };
            _vm.SelectedApp = _testApp;
            _vm.SelectedMod = new ModItemViewModel(shell, null, null, "1.0");

            // Act
            _vm.ShowHistoryCommand.Execute(null);

            // Assert
            _navMock.Verify(n => n.NavigateTo<ModHistoryViewModel, (Mod, ModdedApp)>(
                It.Is<(Mod, ModdedApp)>(t => t.Item1 == shell && t.Item2 == _testApp)),
                Times.Once);
        }
    }
}