using Microsoft.Extensions.DependencyInjection;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data;
using ModsWatcher.Data.Interfaces;
using ModsWatcher.Desktop.Services;
using ModsWatcher.Services.Interfaces;

namespace ModsWatcher.Services.DI
{
    public static class ServicesDependencyInjection
    {
        public static IServiceCollection AddServicesLayer(this IServiceCollection services)
        {
            // Register all your services here
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IWatcherService, PlaywrightWatcherService>();
            services.AddSingleton<CommonUtils>();

            return services;
        }
    }
}
