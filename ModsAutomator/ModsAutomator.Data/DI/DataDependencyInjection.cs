using ModsWatcher.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Helpers;

namespace ModsWatcher.Data.DI
{
    public static class DataDependencyInjection
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
        {
            // Connection factory
            services.AddSingleton<IConnectionFactory>(new SqliteConnectionFactory(connectionString));

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
