using Microsoft.Extensions.DependencyInjection;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data;
using ModsAutomator.Data.Interfaces;
using ModsAutomator.Services.Interfaces;

namespace ModsAutomator.Services.DI
{
    public static class ServicesDependencyInjection
    {
        public static IServiceCollection AddServicesLayer(this IServiceCollection services)
        {
            // Register all your services here
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IWatcherService, PlaywrightWatcherService>();

            return services;
        }
    }
}
