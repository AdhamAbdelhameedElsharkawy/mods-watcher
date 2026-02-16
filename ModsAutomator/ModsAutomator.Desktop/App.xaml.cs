using Microsoft.Extensions.DependencyInjection;
using ModsAutomator.Data.DI;
using ModsAutomator.Data.Helpers;
using ModsAutomator.Data.Interfaces;
using ModsAutomator.Desktop.Interfaces;
using ModsAutomator.Desktop.Services;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Desktop.Views;
using ModsAutomator.Services;
using ModsAutomator.Services.DI;
using ModsAutomator.Services.Interfaces;
using System.Runtime.InteropServices.JavaScript;
using System.Windows;


namespace ModsAutomator.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    //TODO:Logging layer
    //TODO:Admin tools (for managing mods, users, etc.)
    //TODO:Inatallation /Uninstallation logic (with progress reporting)

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
            services.AddTransient<IDialogService, WpfDialogService>();

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<AppSelectionViewModel>();
            services.AddTransient<LibraryViewModel>();
            services.AddTransient<RetiredModsViewModel>();
            services.AddTransient<ModHistoryViewModel>();
            services.AddTransient<AvailableVersionsViewModel>();
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

            try
            {
                // Installs Chromium if it's missing. 
                // This is a synchronous call that will block the UI until finished.
                var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });

                if (exitCode != 0)
                {
                    MessageBox.Show("Playwright failed to install Chromium.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Playwright: {ex.Message}");
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

        protected override async void OnExit(ExitEventArgs e)
        {
            if (ServiceProvider is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
            else if (ServiceProvider is IDisposable syncDisposable)
            {
                syncDisposable.Dispose();
            }

            base.OnExit(e);
        }
    }

}
