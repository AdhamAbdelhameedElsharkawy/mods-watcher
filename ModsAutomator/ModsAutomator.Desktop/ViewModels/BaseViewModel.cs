using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace ModsWatcher.Desktop.ViewModels
{
  

    public abstract class BaseViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Dictionary<string, List<string>> _errors = new();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public bool HasErrors => _errors.Any();

        public IEnumerable GetErrors(string? propertyName) =>
            _errors.GetValueOrDefault(propertyName ?? string.Empty, new List<string>());

        protected void ValidateProperty(object? value, [CallerMemberName] string propertyName = "")
        {
            var context = new ValidationContext(this) { MemberName = propertyName };
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateProperty(value, context, results))
            {
                _errors[propertyName] = results.Select(r => r.ErrorMessage!).ToList();
            }
            else
            {
                _errors.Remove(propertyName);
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }

        protected void AddCustomError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName)) _errors[propertyName] = new List<string>();
            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        protected void RemoveCustomError(string propertyName, string error)
        {
            if (_errors.ContainsKey(propertyName) && _errors[propertyName].Contains(error))
            {
                _errors[propertyName].Remove(error);
                if (_errors[propertyName].Count == 0) _errors.Remove(propertyName);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        protected void ValidateAll()
        {
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            _errors.Clear();

            // Validates all properties marked with [Attributes]
            if (!Validator.TryValidateObject(this, context, results, true))
            {
                foreach (var error in results)
                {
                    foreach (var memberName in error.MemberNames)
                    {
                        if (!_errors.ContainsKey(memberName)) _errors[memberName] = new List<string>();
                        _errors[memberName].Add(error.ErrorMessage!);
                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(memberName));
                    }
                }
            }
            OnPropertyChanged(nameof(HasErrors));
        }

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
