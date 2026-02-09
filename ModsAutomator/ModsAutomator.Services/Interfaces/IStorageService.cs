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
        Task<IEnumerable<(Mod Shell, InstalledMod Installed)>> GetModsByAppId(int appId);
        Task AddModShellAsync(Mod shell);
        Task UpdateModShellAsync(Mod shell);

        //Retired Mods/UnusedModHistory

        Task<IEnumerable<UnusedModHistory>> GetRetiredModsByAppIdAsync(int appId);

        Task RestoreModFromHistoryAsync(UnusedModHistory history);
    }
}