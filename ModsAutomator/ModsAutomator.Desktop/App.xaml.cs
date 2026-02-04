using Microsoft.Extensions.DependencyInjection;
using ModsAutomator.Data;
using ModsAutomator.Data.DI;
using ModsAutomator.Data.Interfaces;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.Services;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Desktop.Views;
using ModsAutomator.Services;
using ModsAutomator.Services.DI;
using ModsAutomator.Services.Interfaces;
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
            string connectionString = "Data Source=mods.db";

            // 1. Data Project DI (Registers Repos & Factory)
            services.AddDataServices(connectionString);

            // 2. Services Project DI (Registers StorageService, etc.)
            // Assuming you have a method like AddBusinessServices() in that project
            services.AddServicesLayer();

            // 3. Desktop Project (Local registrations)
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<AppSelectionViewModel>();
            // ... other viewmodels

            ServiceProvider = services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // 1. Initialize Database using the registered Factory
            var connectionFactory = ServiceProvider.GetRequiredService<IConnectionFactory>();
            using (var connection = connectionFactory.CreateConnection())
            {
                await SqliteDbInitializer.InitializeAsync(connection);
            }

            // 2. Setup UI
            var mainWindow = new MainWindow();
            var mainVM = ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = mainVM;

            var nav = ServiceProvider.GetRequiredService<INavigationService>();
            nav.NavigateTo<AppSelectionViewModel>();

            mainWindow.Show();
            base.OnStartup(e);
        }
    }

}
