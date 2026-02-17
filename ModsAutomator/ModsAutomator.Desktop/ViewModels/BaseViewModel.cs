using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModsAutomator.Desktop.ViewModels
{
  

    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        
        
        public event PropertyChangedEventHandler? PropertyChanged;

        // This method handles the 'notification' logic
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // A helper method to set properties and notify in one line
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private bool _isBusy;
        private string _busyMessage = "Loading...";

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }
    }
}
