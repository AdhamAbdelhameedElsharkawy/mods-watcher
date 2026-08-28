using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using System.Data;

namespace ModsWatcher.Services.Interfaces
{
    public interface IStorageService
    {
        // For the App Selection Screen
        Task<IEnumerable<ModdedApp>> GetAllAppsAsync();

        Task<IEnumerable<AppSummaryDto>> GetAllAppSummariesAsync();

        Task<bool> AddAppAsync(ModdedApp app);

        Task UpdateAppAsync(ModdedApp app);

        Task<IEnumerable<(Mod Shell, InstalledMod? Installed, ModCrawlerConfig? Config)>> GetFullModsByAppId(int appId);
        Task AddModShellAsync(Mod shell);
        Task UpdateModShellAsync(Mod shell);


        // Unified methods to handle both entities together
        Task<bool> SaveModWithConfigAsync(Mod mod, ModCrawlerConfig config);
        Task UpdateModWithConfigAsync(Mod mod, ModCrawlerConfig config);

        Task<(Mod? Shell, ModCrawlerConfig? Config)> GetModPackageAsync(Guid modId);

        //Retired Mods/UnusedModHistory

        Task<IEnumerable<UnusedModHistory>> GetRetiredModsByAppIdAsync(int appId);

        Task RestoreModFromHistoryAsync(UnusedModHistory history);

        // Mod Installation History

        Task<IEnumerable<InstalledModHistory>> GetInstalledModHistoryAsync(Guid modId);

        Task RollbackToVersionAsync(InstalledModHistory target, string appVersion);

        Task DeleteInstalledModHistoryAsync(int historyId);

        // Hard wipe

        Task HardWipeAppAsync(int appId);

        Task HardWipeModAsync(Mod mod, ModdedApp parentApp, ModCrawlerConfig modCrawlerConfig, string wipeReason);

        //Available Versions Screen
        Task<IEnumerable<(Mod Shell, IEnumerable<AvailableMod> Versions)>> GetAvailableVersionsByAppIdAsync(int appId, Guid? modId = null);
        Task SaveCrawledVersionsAsync(Guid modId, IEnumerable<AvailableMod> versions);
        Task PromoteAvailableToInstalledAsync(
    AvailableMod selected,
    string appVersion,
    IDbConnection? connection = null,
    IDbTransaction? transaction = null);

        Task DeleteAvailableModAsync(int internalId);
        Task DeleteAvailableModsBatchAsync(IEnumerable<int> internalIds);

        //Crawl Configurations

        Task<ModCrawlerConfig?> GetModCrawlerConfigByModIdAsync(Guid modId);

        //Watcher logic

        Task<IEnumerable<(Mod Shell, ModCrawlerConfig Config)>> GetWatchableBundleByAppIdAsync(int appId);

        Task<InstalledMod?> ProcessCrawlResultsAsync(string appVersion, Guid shellId, AvailableMod? primary, List<AvailableMod> scrapedMods);

        // Mod Dependencies

        Task AddDependencyAsync(Guid dependentModId, Guid parentModId);
        Task RemoveDependencyAsync(Guid dependentModId, Guid parentModId);
        Task<IEnumerable<ModDependency>> GetDependentsAsync(Guid parentModId);
        Task<IEnumerable<ModDependency>> GetParentsAsync(Guid dependentModId);
        Task<DependencyTreeNodeDto?> GetDependencyImpactTreeAsync(Guid parentModId);
        Task<bool> WouldCreateCircularDependencyAsync(Guid dependentModId, Guid parentModId);
        Task<IEnumerable<DependencyTreeNodeDto>> GetDependencyForestByAppIdAsync(int appId);
        Task<(HashSet<Guid> Parents, HashSet<Guid> Children)> GetDependencyRolesByAppIdAsync(int appId);
        Task<HashSet<Guid>> GetModIdsWithAvailableVersionsByAppIdAsync(int appId);

        // Mod Alternatives

        Task AddAlternativeAsync(Guid modId, Guid alternativeModId);
        Task RemoveAlternativeAsync(Guid modId, Guid alternativeModId);
        Task<IEnumerable<ModAlternativeDisplayDto>> GetAlternativeGroupAsync(Guid modId);
        Task<HashSet<Guid>> GetModIdsWithAlternativesByAppIdAsync(int appId);

        // Mod Packages

        Task<IEnumerable<ModPackageMember>> GetPackageMembersAsync(Guid mainModId);
        Task<ModPackageMember> AddPackageMemberAsync(Guid mainModId, string name, string? notes, string? url);
        Task RemovePackageMemberAsync(int memberInternalId);
        Task ReorderPackageMembersAsync(IEnumerable<ModPackageMember> orderedMembers);
        Task<HashSet<Guid>> GetPackageMainModIdsByAppIdAsync(int appId);



        //Mod installation and uninstallation

        Task<InstalledMod?> GetInstalledModsByModIdAsync(Guid? modId);

        Task SaveInstalledModAsync(InstalledMod installedMod);

        Task UpdateInstalledModAsync(InstalledMod installedMod);
    }
}