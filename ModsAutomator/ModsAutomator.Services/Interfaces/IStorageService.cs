using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;

namespace ModsAutomator.Services.Interfaces
{
    public interface IStorageService
    {
        // For the App Selection Screen
        Task<IEnumerable<ModdedApp>> GetAllAppsAsync();

        Task<IEnumerable<AppSummaryDto>> GetAllAppSummariesAsync();

        Task AddAppAsync(ModdedApp app);

        Task UpdateAppAsync(ModdedApp app);

        // We'll add methods for the Library Screen (GetModsByAppId) 
        // in the next step to keep this focused.
    }
}