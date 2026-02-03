using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class AppSelectionViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        // private readonly IDataService _dataService;

        public ObservableCollection<ModdedApp> ModdedApps { get; set; }

        public ICommand SelectAppCommand { get; }

        public AppSelectionViewModel(INavigationService navigationService /*, IDataService dataService */)
        {
            _navigationService = navigationService;
            // _dataService = dataService;

            ModdedApps = new ObservableCollection<ModdedApp>();

            // Mocking the load logic
            LoadApps();

            SelectAppCommand = new RelayCommand(o =>
            {
                if (o is ModdedApp selectedApp)
                {
                    // Using the new Generic method: 
                    // TViewModel = LibraryViewModel
                    // TData = ModdedApp
                    _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(selectedApp);
                }
            });
        }

        private void LoadApps()
        {
            // var apps = _dataService.GetAllApps();
            // foreach(var app in apps) ModdedApps.Add(app);
        }
    }
}