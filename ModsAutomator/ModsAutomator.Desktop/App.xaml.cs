using Microsoft.Extensions.DependencyInjection;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.Services;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Desktop.Views;
using System.Windows;


namespace ModsAutomator.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 



    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var services = new ServiceCollection();

            // 1. Register Services (Singletons usually)
            services.AddSingleton<INavigationService, NavigationService>();

            // 2. Register ViewModels (Transients so they refresh when navigated to)
            services.AddSingleton<MainViewModel>(); // Main stays alive
            services.AddTransient<AppSelectionViewModel>();
            services.AddTransient<LibraryViewModel>();
            services.AddTransient<AvailableVersionsViewModel>();
            services.AddTransient<ModHistoryViewModel>();
            services.AddTransient<RetiredModsViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = new MainWindow();

            // 1. Get the MainViewModel so the Window has its context
            var mainVM = ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = mainVM;

            // 2. NEW: Use the NavigationService to set the initial "App Selection" screen
            var nav = ServiceProvider.GetRequiredService<INavigationService>();
            nav.NavigateTo<AppSelectionViewModel>();

            mainWindow.Show();
            base.OnStartup(e);
        }
    }

}
