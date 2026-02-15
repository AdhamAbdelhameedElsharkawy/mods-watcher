using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;

namespace ModsAutomator.Services.Interfaces
{
    public interface IStorageService
    {
        // For the App Selection Screen
        Task<IEnumerable<ModdedApp>> GetAllAppsAsync();

        Task<IEnumerable<AppSummaryDto>> GetAllAppSummariesAsync();

        Task AddAppAsync(ModdedApp app);

        Task UpdateAppAsync(ModdedApp app);

        Task<IEnumerable<(Mod Shell, InstalledMod? Installed, ModCrawlerConfig? Config)>> GetFullModsByAppId(int appId);
        Task AddModShellAsync(Mod shell);
        Task UpdateModShellAsync(Mod shell);

        // Unified methods to handle both entities together
        Task SaveModWithConfigAsync(Mod mod, ModCrawlerConfig config);
        Task UpdateModWithConfigAsync(Mod mod, ModCrawlerConfig config);

        Task<(Mod? Shell, ModCrawlerConfig? Config)> GetModPackageAsync(Guid modId);

        //Retired Mods/UnusedModHistory

        Task<IEnumerable<UnusedModHistory>> GetRetiredModsByAppIdAsync(int appId);

        Task RestoreModFromHistoryAsync(UnusedModHistory history);

        // Mod Installation History

        Task<IEnumerable<InstalledModHistory>> GetInstalledModHistoryAsync(Guid modId);

        Task RollbackToVersionAsync(InstalledModHistory target, string appVersion);

        // Hard wipe

        Task HardWipeAppAsync(int appId);

        Task HardWipeModAsync(Mod mod, ModdedApp parentApp);

        //Available Versions Screen
        Task<IEnumerable<(Mod Shell, IEnumerable<AvailableMod> Versions)>> GetAvailableVersionsByAppIdAsync(int appId, Guid? modId = null);
        Task SaveCrawledVersionsAsync(Guid modId, IEnumerable<AvailableMod> versions);
        Task PromoteAvailableToInstalledAsync(AvailableMod selected, string appVersion);

        Task<IEnumerable<(AvailableMod Entity, SyncChangeType Type)>> CompareAndIdentifyChangesAsync(Guid modId, int appId, List<AvailableMod> webVersions);

        // Inside IStorageService.cs
        Task CommitSyncChangeAsync(Guid modId, AvailableMod entity, SyncChangeType type);


        //Crawl Configurations

        Task<ModCrawlerConfig?> GetModCrawlerConfigByModIdAsync(Guid modId);

        //Watcher logic

        Task<IEnumerable<(Mod Shell, ModCrawlerConfig Config)>> GetWatchableBundleByAppIdAsync(int appId);

        //Mod installation and uninstallation

        Task SaveInstalledModAsync(InstalledMod installedMod);

        Task UpdateInstalledModAsync(InstalledMod installedMod);
    }
}