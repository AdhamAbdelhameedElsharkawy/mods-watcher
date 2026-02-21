using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModsWatcher.Services.Config;
using ModsWatcher.Services.Interfaces;

namespace ModsWatcher.Services.DI
{
    public static class ServicesDependencyInjection
    {
        public static IServiceCollection AddServicesLayer(this IServiceCollection services, IConfiguration configuration)
        {

            // Register specific nodes from the config
            services.Configure<WatcherSettings>(configuration.GetSection("WatcherSettings"));

            // Register all your services here
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IWatcherService, PlaywrightWatcherService>();
            services.AddSingleton<CommonUtils>();

            return services;
        }
    }
}
