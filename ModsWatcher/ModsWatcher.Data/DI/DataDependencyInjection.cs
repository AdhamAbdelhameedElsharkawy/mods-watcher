using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Config;
using ModsWatcher.Data.Helpers;
using ModsWatcher.Data.Interfaces;

namespace ModsWatcher.Data.DI
{
    public static class DataDependencyInjection
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
        {

            // Register specific nodes from the config
            services.Configure<DatabaseSettings>(configuration.GetSection("DatabaseSettings"));

            string connString = configuration["DatabaseSettings:ConnectionString"] ?? "Data Source=mods.db";

            // Connection factory
            services.AddSingleton<IConnectionFactory>(new SqliteConnectionFactory(connString));

            // Repositories
            services.AddScoped<IModdedAppRepository, ModdedAppRepository>();
            services.AddScoped<IModRepository, ModRepository>();
            services.AddScoped<IInstalledModRepository, InstalledModRepository>();
            services.AddScoped<IAvailableModRepository, AvailableModRepository>();
            services.AddScoped<IInstalledModHistoryRepository, InstalledModHistoryRepository>();
            services.AddScoped<IUnusedModHistoryRepository, UnusedModHistoryRepository>();
            services.AddScoped<IModCrawlerConfigRepository, ModCrawlerConfigRepository>();
            

            return services;
        }
    }
}
