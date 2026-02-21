using Microsoft.Extensions.Logging;
using ModsWatcher.Desktop.Services;

namespace ModsWatcher.Desktop.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _currentViewModel;

        /// <summary>
        /// The property the MainWindow's ContentControl binds to.
        /// </summary>
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public MainViewModel(ILogger logger) : base(logger)
        {
            // Note: We don't initialize the starting view here anymore 
            // if we want to follow the DI pattern strictly. 
            // The App.xaml.cs will tell the NavigationService to set the initial view.
        }
    }
}