using System.Collections.ObjectModel;
using System.Windows.Input;
using ModsAutomator.Core.Entities;

namespace ModsAutomator.Desktop.ViewModels
{
    public class RetiredModsViewModel : BaseViewModel
    {
        private readonly ModdedApp _app;
        // private readonly IDataService _dataService;
        // private readonly INavigationService _nav;

        public string AppName => _app.Name;

        public ObservableCollection<Mod> RetiredMods { get; set; }

        public ICommand RestoreModCommand { get; }
        public ICommand BackCommand { get; }

        public RetiredModsViewModel(ModdedApp app /*, IDataService dataService, INavigationService nav */)
        {
            _app = app;
            RetiredMods = new ObservableCollection<Mod>();

            // 1. Load data
            // var archived = _dataService.GetRetiredMods(_app.ID);
            // foreach(var m in archived) RetiredMods.Add(m);

            // Command to move mod from Archive back to Library
            RestoreModCommand = new RelayCommand(o =>
            {
                if (o is Mod selectedMod)
                {
                    // selectedMod.IsRetired = false;
                    // _dataService.UpdateMod(selectedMod);
                    // RetiredMods.Remove(selectedMod);
                }
            });

            // Navigation back to the active library
            BackCommand = new RelayCommand(o =>
            {
                // _nav.NavigateToLibrary(_app);
            });
        }
    }
}