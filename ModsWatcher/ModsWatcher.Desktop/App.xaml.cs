using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; 
using ModsWatcher.Data.DI;
using ModsWatcher.Data.Helpers;
using ModsWatcher.Data.Interfaces;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Desktop.Services;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Desktop.Views;
using ModsWatcher.Services.DI;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ModsWatcher.Desktop
{

    //TODO:Admin tools (for managing mods, users, etc.)
    //TODO:Installation pacakaging.
    //TODO:check App, Mods Cards, for more visual notification of updates, new versions, etc.
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            // 1. Configure Serilog immediately
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            string logFilePath = Path.Combine(logDirectory, "log-.txt");

            //var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModsAutomator", "logs.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 5 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 10,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

            var services = new ServiceCollection();

            // 2. Add Logging to DI
            services.AddLogging(builder => builder.AddSerilog(dispose: true));

            services.AddTransient<Microsoft.Extensions.Logging.ILogger>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("App"));

            //string connectionString = "Data Source=mods.db";

            services.AddDataServices(configuration);
            services.AddServicesLayer(configuration);

            services.AddTransient<IDialogService, WpfDialogService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<AppSelectionViewModel>();
            services.AddTransient<LibraryViewModel>();
            services.AddTransient<RetiredModsViewModel>();
            services.AddTransient<ModHistoryViewModel>();
            services.AddTransient<AvailableVersionsViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {

            base.OnStartup(e);

            var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("ModsWatcher starting up...");

            // Hook Global Exceptions
            this.DispatcherUnhandledException += (s, args) => {
                logger.LogCritical(args.Exception, "Unhandled UI Exception");
                MessageBox.Show("A critical error occurred. See logs for details.");
            };

            // Database Init
            var connectionFactory = ServiceProvider.GetRequiredService<IConnectionFactory>();
            using (var connection = connectionFactory.CreateConnection())
            {
                await SqliteDbInitializer.InitializeAsync(connection);
            }

            try
            {
                var sw = Stopwatch.StartNew();
                logger.LogInformation("Checking Playwright browsers...");

                await Task.Run(() => {
                    Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                });
                sw.Stop();
                logger.LogInformation("Playwright check completed in {Elapsed}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Playwright initialization failed.");
                MessageBox.Show($"Error initializing Playwright: {ex.Message}");
            }

            // UI Setup
            var mainWindow = new MainWindow();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();

            var nav = ServiceProvider.GetRequiredService<INavigationService>();
            nav.NavigateTo<AppSelectionViewModel>();

            mainWindow.Show();
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exiting.");
            Log.CloseAndFlush(); // Important to flush logs to disk
            base.OnExit(e);
        }
    }
}