using Microsoft.Extensions.Logging;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.ViewModels;

namespace ModsWatcher.Desktop.Services
{
    public class LoadingService : BaseViewModel, ILoadingService

        
    {
        public LoadingService(ILogger logger) : base(logger) { }

        private bool _isBusy;
        private string _busyMessage;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public string BusyMessage { get => _busyMessage; set { _busyMessage = value; OnPropertyChanged(); } }
    }
}
