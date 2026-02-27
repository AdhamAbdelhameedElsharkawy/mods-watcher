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

   
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        private string _playwrightPath = string.Empty;

        public App()
        {
            // 1. Configuration
            var configuration = new ConfigurationBuilder()
.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
.Build();


            // 2. Extract Logging values (with defaults as backups)
            var logSettings = configuration.GetSection("LoggingSettings");
            string minLevel = logSettings["MinimumLevel"] ?? "Information";
            string logFileName = logSettings["LogFileName"] ?? "log-.txt";
            long fileSize = long.Parse(logSettings["FileSizeLimitBytes"] ?? "5242880");
            int retainedFiles = int.Parse(logSettings["RetainedFileCountLimit"] ?? "10");
            bool rollOnSize = bool.Parse(logSettings["RollOnFileSizeLimit"] ?? "true");
            string template = logSettings["OutputTemplate"] ?? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            string logFilePath = Path.Combine(logDirectory, logFileName);

            // 3. Configure Serilog using those values
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Enum.Parse<Serilog.Events.LogEventLevel>(minLevel))
                .WriteTo.File(logFilePath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: fileSize,
                    rollOnFileSizeLimit: rollOnSize,
                    retainedFileCountLimit: retainedFiles,
                    outputTemplate: template)
                .CreateLogger();


            // 4. Setup Playwright path for
            var watcherSettings = configuration.GetSection("WatcherSettings");
            _playwrightPath = watcherSettings["PlayWrightDebugPath"] ?? @"C:\PlaywrightOffline";



            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);

            //Add Logging to DI
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
            services.AddSingleton<ILoadingService, LoadingService>();

            ServiceProvider = services.BuildServiceProvider();

            var globalLoading = ServiceProvider.GetRequiredService<ILoadingService>();
            BaseViewModel.Initialize(globalLoading);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {

            // Change shutdown mode so closing setupWin doesn't kill the app
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            base.OnStartup(e);

            var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("ModsWatcher starting up...");

            var config = ServiceProvider.GetRequiredService<IConfiguration>();

            // 2. Decide which file to load
            bool isDark = bool.Parse(config["Theme:IsDarkTheme"] ?? "true");
            string themeFile = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

            // 3. Add to Global Resources
            try
            {
                var dict = new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) };
                this.Resources.MergedDictionaries.Add(dict);
            }
            catch { /* Fallback logic */ }

            // Hook Global Exceptions
            this.DispatcherUnhandledException += (s, args) =>
            {
                logger.LogCritical(args.Exception, "Unhandled UI Exception");
                MessageBox.Show("A critical error occurred. See logs for details.");
            };

            // Database Init
            var connectionFactory = ServiceProvider.GetRequiredService<IConnectionFactory>();
            using (var connection = connectionFactory.CreateConnection())
            {
                await SqliteDbInitializer.InitializeAsync(connection);
            }


#if DEBUG
           
#else
   string baseDir = AppDomain.CurrentDomain.BaseDirectory;
_playwrightPath = Path.Combine(baseDir, ".playwright");

// The .NET driver usually buries node.exe here:
string nodeDir = Path.Combine(_playwrightPath, "node", "win32_x64");
string nodeExe = Path.Combine(nodeDir, "node.exe");

// Tell Playwright where to find its own engine
Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", nodeExe);

// Tell Playwright where to download/find the browsers (Chromium)
Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", _playwrightPath);
#endif


            
                logger.LogInformation($"Checking Playwright browsers with Path: {_playwrightPath}...");
                // Check if the actual chromium folder exists in .playwright
                bool isInstalled = Directory.Exists(_playwrightPath) &&
                                   Directory.GetDirectories(_playwrightPath, "chromium-*", SearchOption.AllDirectories).Any();

                if (!isInstalled)
                {
                    var setupWin = new ModsWatcher.Desktop.Views.BrowserSetupWindow();
                    var sw = Stopwatch.StartNew();

                    
                    logger.LogInformation("Chromium not found. Installing to sidecar...");

                    try
                    {
                        setupWin.Show();
                        await Task.Run(() =>
                        {
                            // Now Main() will find node.exe because we set PLAYWRIGHT_NODEJS_PATH
                            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                            sw.Stop();
                            logger.LogInformation("Playwright check completed in {Elapsed}ms", sw.ElapsedMilliseconds);
                            if (exitCode != 0) throw new Exception($"Install failed: {exitCode}");
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Playwright initialization failed.");
                        MessageBox.Show($"Error initializing Playwright: {ex.Message}");
                    }
                    finally
                    {
                        setupWin.Close();
                    }
                }
            
            

            // UI Setup
            var mainWindow = new MainWindow();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();

            var nav = ServiceProvider.GetRequiredService<INavigationService>();
            nav.NavigateTo<AppSelectionViewModel>();

            this.MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Closed += (s, args) =>
            {
                logger.LogInformation("Main window closed. Shutting down...");
                this.Shutdown(); // This officially kills the app
            };

        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exiting.");
            Log.CloseAndFlush(); // Important to flush logs to disk
            base.OnExit(e);
        }
    }
}