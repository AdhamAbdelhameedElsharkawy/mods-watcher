using ModsAutomator.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ModsAutomator.Data.DI
{
    public static class DataDependencyInjection
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
        {
            // Connection factory
            services.AddSingleton<IConnectionFactory>(new SqliteConnectionFactory(connectionString));

            // Repositories
            //services.AddScoped<IInstalledModRepository, InstalledModRepository>();
            //services.AddScoped<IAvailableModRepository, AvailableModRepository>();
            //services.AddScoped<IInstalledModHistoryRepository, InstalledModHistoryRepository>();
            // No IModRepository, remember Mod is abstract

            return services;
        }
    }
}
