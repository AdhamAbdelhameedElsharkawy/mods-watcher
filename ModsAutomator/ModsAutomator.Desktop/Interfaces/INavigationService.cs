using ModsWatcher.Desktop.ViewModels;

namespace ModsWatcher.Desktop.Interfaces
{
    public interface INavigationService
    {
        // For simple screens with no data (e.g., Settings)
        void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;

        // For screens that need an Entity (e.g., Library needs a ModdedApp)
        void NavigateTo<TViewModel, TData>(TData data) where TViewModel : BaseViewModel;
    }
}
