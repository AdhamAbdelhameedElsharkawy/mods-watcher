using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class RetiredModsViewModel : BaseViewModel
    {
        private readonly ModdedApp _app;
        private readonly INavigationService _nav;
        private readonly IStorageService _storageService;

        public string AppName => _app.Name;

        // Using the historical entity instead of the live Mod entity
        public ObservableCollection<UnusedModHistory> RetiredMods { get; set; }

        // Logic for the Blank State/Opacity toggles in XAML
        public bool HasRetiredMods => RetiredMods.Count > 0;
        public bool HasNoRetiredMods => !HasRetiredMods;

        public ICommand RestoreCommand { get; } // Matches binding in XAML
        public ICommand BackCommand { get; }

        public RetiredModsViewModel(ModdedApp app, INavigationService nav, IStorageService storageService)
        {
            _app = app;
            _nav = nav;
            _storageService = storageService;

            RetiredMods = new ObservableCollection<UnusedModHistory>();

            // Wire up collection change notification to update blank state properties
            RetiredMods.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasRetiredMods));
                OnPropertyChanged(nameof(HasNoRetiredMods));
            };

            LoadData();

            // Command to Re-birth the mod from history
            RestoreCommand = new RelayCommand(o =>
            {
                if (o is UnusedModHistory selectedHistory)
                {
                    // Logic: Service creates a new ModShell/InstalledMod using history DNA
                    _storageService.RestoreModFromHistoryAsync(selectedHistory);

                    // Remove from graveyard view
                    RetiredMods.Remove(selectedHistory);
                }
            });

            // Navigation back to the active library
            BackCommand = new RelayCommand(o =>
            {
                _nav.NavigateTo<LibraryViewModel, ModdedApp>(_app);
            });
        }

        private async Task LoadData()
        {
            RetiredMods.Clear();
            var archived = await _storageService.GetRetiredModsByAppIdAsync(_app.Id);
            foreach (var m in archived)
            {
                RetiredMods.Add(m);
            }
        }
    }
}