using System;
using System.Windows.Input;
namespace ModsAutomator.Desktop.ViewModels
{


    public class RelayCommand : ICommand
    {
        private readonly Func<object, Task> _executeAsync;
        private readonly Predicate<object> _canExecute;
        private bool _isExecuting;

        // Standard Sync Constructor
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            : this(param => { execute(param); return Task.CompletedTask; }, canExecute) { }

        // Async Constructor
        public RelayCommand(Func<object, Task> executeAsync, Predicate<object> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) =>
            !_isExecuting && (_canExecute == null || _canExecute(parameter));

        // UI calls this (WPF)
        public async void Execute(object parameter) => await ExecuteAsync(parameter);

        // Tests call this (Unit Testing)
        public async Task ExecuteAsync(object parameter)
        {
            if (!CanExecute(parameter)) return;
            try
            {
                _isExecuting = true;
                CommandManager.InvalidateRequerySuggested();
                await _executeAsync(parameter);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
