using ModsAutomator.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Helpers;

namespace ModsAutomator.Data.DI
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
            

            return services;
        }
    }
}
