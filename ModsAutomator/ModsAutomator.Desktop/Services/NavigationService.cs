using Microsoft.Extensions.DependencyInjection;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.ViewModels;

namespace ModsAutomator.Desktop.Services
{
    public class NavigationService : INavigationService
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(MainViewModel mainViewModel, IServiceProvider serviceProvider)
        {
            _mainViewModel = mainViewModel;
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            _mainViewModel.CurrentViewModel = viewModel;
        }

        public void NavigateTo<TViewModel, TData>(TData data) where TViewModel : BaseViewModel
        {
            // 1. Resolve the ViewModel from DI
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            // 2. Pass the data if the VM supports IInitializable
            if (viewModel is IInitializable<TData> initializable)
            {
                initializable.Initialize(data);
            }

            // 3. Update the UI
            _mainViewModel.CurrentViewModel = viewModel;
        }
    }
}