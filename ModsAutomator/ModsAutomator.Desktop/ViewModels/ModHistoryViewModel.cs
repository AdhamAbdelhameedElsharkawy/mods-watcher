using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModHistoryViewModel : BaseViewModel, IInitializable<(Guid ModId, ModdedApp App)>
    {
        private readonly INavigationService _navigationService;
        private Guid _modId;
        private ModdedApp _parentApp;

        public ObservableCollection<InstalledModHistory> HistoryItems { get; set; }

        public ModHistoryViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            HistoryItems = new ObservableCollection<InstalledModHistory>();
        }

        // Initialize now captures both pieces of state
        public void Initialize((Guid ModId, ModdedApp App) data)
        {
            _modId = data.ModId;
            _parentApp = data.App;
            LoadHistory();
        }

        private void LoadHistory()
        {
            // Use _modId to fetch records
        }

        public ICommand BackCommand => new RelayCommand(o =>
        {
            // Navigate back to Library using the parent app reference
            _navigationService.NavigateTo<LibraryViewModel, ModdedApp>(_parentApp);
        });
    }
}