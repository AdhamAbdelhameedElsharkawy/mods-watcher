using Microsoft.Extensions.Logging;
using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Enums;
using System.Windows;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class DependencyImpactDialogViewModel : BaseViewModel
    {
        private readonly bool _isDeactivation;

        // --- Display ---
        public string Title { get; }
        public string Subtitle { get; }
        public string RemoveDependentLabel { get; }
        public DependencyTreeNodeDto[] TreeSource { get; }

        // --- Visibility ---
        public Visibility DeactivateButtonVisibility { get; }
        public Visibility BreakLinkButtonVisibility { get; }

        // --- Result ---
        public DependencyImpactAction SelectedAction { get; private set; } = DependencyImpactAction.Cancel;

        // --- Commands ---
        public ICommand RemoveDependentCommand { get; }
        public ICommand DeactivateDependentCommand { get; }
        public ICommand BreakDependencyCommand { get; }
        public ICommand CancelCommand { get; }

        public DependencyImpactDialogViewModel(string modName, DependencyTreeNodeDto tree, bool isDeactivation, ILogger logger) : base(logger)
        {
            _isDeactivation = isDeactivation;

            Title = isDeactivation
                ? $"'{modName}' has dependent mods"
                : $"Removing '{modName}' will affect dependent mods";

            Subtitle = isDeactivation
                ? "Other mods depend on this one. Deactivating it may break them. How would you like to proceed?"
                : "Other mods depend on this one. Removing it may break them. How would you like to proceed?";

            RemoveDependentLabel = isDeactivation
                ? "Retire all dependent mods"
                : "Remove all dependent mods";

            DeactivateButtonVisibility = isDeactivation ? Visibility.Visible : Visibility.Collapsed;
            BreakLinkButtonVisibility = isDeactivation ? Visibility.Collapsed : Visibility.Visible;

            // Wrap root in array so TreeView ItemsSource can bind to it
            TreeSource = new[] { tree };

            RemoveDependentCommand = new RelayCommand(_ => CloseWith(DependencyImpactAction.RemoveDependent));
            DeactivateDependentCommand = new RelayCommand(_ => CloseWith(DependencyImpactAction.DeactivateDependent));
            BreakDependencyCommand = new RelayCommand(_ => CloseWith(DependencyImpactAction.BreakDependency));
            CancelCommand = new RelayCommand(_ => CloseWith(DependencyImpactAction.Cancel));
        }

        private void CloseWith(DependencyImpactAction action)
        {
            SelectedAction = action;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    return;
                }
            }
        }
    }
}