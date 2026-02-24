using Microsoft.Extensions.Logging;
using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Interfaces;
using ModsWatcher.Data.Interfaces;
using ModsWatcher.Services.Interfaces;
using System.Data;

namespace ModsWatcher.Services
{
    public class StorageService : IStorageService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IModdedAppRepository _appRepo;
        private readonly IModRepository _modRepo;
        private readonly IInstalledModRepository _installedModRepo;
        private readonly IUnusedModHistoryRepository _unUsedModRepo;
        private readonly IInstalledModHistoryRepository _installedModHistoryRepo;
        private readonly IModCrawlerConfigRepository _modCrawlerConfigRepo;
        private readonly IAvailableModRepository _availableModRepo;
        private readonly CommonUtils _commonUtils;
        private readonly ILogger<StorageService> _logger;

        // We inject the Repository and the ConnectionFactory
        public StorageService(
            IConnectionFactory connectionFactory,
            IModdedAppRepository appRepo,
            IModRepository modRepo,
            IInstalledModRepository installedModRepo,
            IUnusedModHistoryRepository unUsedModRepo,
            IInstalledModHistoryRepository installedModHistoryRepo,
            IModCrawlerConfigRepository modCrawlerConfigRepo,
            IAvailableModRepository availableModRepo,
            CommonUtils commonUtils,
            ILogger<StorageService> logger)
        {
            _connectionFactory = connectionFactory;
            _appRepo = appRepo;
            _modRepo = modRepo;
            _installedModRepo = installedModRepo;
            _unUsedModRepo = unUsedModRepo;
            _installedModHistoryRepo = installedModHistoryRepo;
            _modCrawlerConfigRepo = modCrawlerConfigRepo;
            _availableModRepo = availableModRepo;
            _commonUtils = commonUtils;
            _logger = logger;
        }


        #region ModdedApp Methods

        public async Task AddAppAsync(ModdedApp app)
        {
            using var connection = _connectionFactory.CreateConnection();
            // Ensure the connection is open for the repository
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            await _appRepo.InsertAsync(app, connection);
        }

        public async Task UpdateAppAsync(ModdedApp app)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            await _appRepo.UpdateAsync(app, connection);
        }

        public async Task<IEnumerable<ModdedApp>> GetAllAppsAsync()
        {
            // The StorageService manages the connection lifecycle
            using var connection = _connectionFactory.CreateConnection();

            // Dapper/Repos usually need the connection opened
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            return await _appRepo.QueryAllAsync(connection);
        }

        public async Task<IEnumerable<AppSummaryDto>> GetAllAppSummariesAsync()
        {
            using var connection = _connectionFactory.CreateConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            // 1. Get all apps
            var apps = await _appRepo.QueryAllAsync(connection);
            var summaries = new List<AppSummaryDto>();

            // 2. Fetch counts based on Mod entity properties
            foreach (var app in apps)
            {
                // Get the specific counts for this app's mod collection
                var stats = await _modRepo.GetWatcherSummaryStatsAsync(app.Id, connection);

                summaries.Add(new AppSummaryDto
                {
                    App = app,
                    ActiveCount = stats.ActiveCount,
                    PotentialUpdatesCount = stats.PotentialUpdatesCount
                });
            }

            return summaries;
        }

        #endregion

        #region Mod Shell Methods

        public async Task AddModShellAsync(Mod shell)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();


            await _modRepo.InsertAsync(shell, connection);
        }

        public async Task UpdateModShellAsync(Mod shell)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            await _modRepo.UpdateAsync(shell, connection);


        }

        public async Task UpdateModsOrderAsync(IEnumerable<Mod> shells)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                int priority = 0;
                foreach (var shell in shells)
                {
                    shell.PriorityOrder = priority++;
                    // Pass the transaction into your repository method
                    await _modRepo.UpdateAsync(shell, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<(Mod Shell, InstalledMod? Installed, ModCrawlerConfig? Config)>> GetFullModsByAppId(int appId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            var shells = await _modRepo.GetByAppIdAsync(appId, connection);
            var results = new List<(Mod, InstalledMod?, ModCrawlerConfig?)>();

            foreach (Mod shell in shells)
            {
                var installed = await _installedModRepo.FindByModIdAsync(shell.Id, connection);
                var config = await _modCrawlerConfigRepo.GetByModIdAsync(shell.Id, connection);
                results.Add((shell, installed, config));
            }

            return results;
        }

        #region Mod Shell & Config Unified Methods

        public async Task SaveModWithConfigAsync(Mod mod, ModCrawlerConfig config)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            // Pass connection to the repository; the repository handles the transaction internally
            await _modRepo.SaveModWithConfigAsync(mod, config, connection);
        }

        public async Task UpdateModWithConfigAsync(Mod mod, ModCrawlerConfig config)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            await _modRepo.UpdateModWithConfigAsync(mod, config, connection);
        }

        public async Task<(Mod? Shell, ModCrawlerConfig? Config)> GetModPackageAsync(Guid modId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            // We fetch them separately as read-only operations
            var shell = await _modRepo.GetByIdAsync(modId, connection);

            if (shell == null)
                return (null, null);

            var config = await _modCrawlerConfigRepo.GetByModIdAsync(modId, connection);

            return (shell, config);
        }

        #endregion


        #endregion

        #region Installed Mods Query Methods

        public async Task<InstalledMod?> GetInstalledModsByModIdAsync(Guid? modId)
        {

            if (modId == null)
                return null;
            
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            // This method fetches all active installed mods for a given app
           Guid nonNullableGuid = modId.Value;
            return await _installedModRepo.FindByModIdAsync(nonNullableGuid, connection);
        }

        #endregion

        #region Mod config Methods

        public async Task<ModCrawlerConfig?> GetModCrawlerConfigByModIdAsync(Guid modId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            return await _modCrawlerConfigRepo.GetByModIdAsync(modId, connection);
        }

        #endregion

        #region Retired Mods Methods

        public async Task<IEnumerable<UnusedModHistory>> GetRetiredModsByAppIdAsync(int appId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            // Fetches historical snapshots from the UnusedModHistory table
            return await _unUsedModRepo.FindByModdedAppIdAsync(appId, connection);


        }

        public async Task RestoreModFromHistoryAsync(UnusedModHistory history)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                
                _logger.LogInformation("Restoring mod from history. ModId: {ModId}, Name: {Name}, AppId: {AppId}", history.ModId, history.Name, history.ModdedAppId);
                // 1. Re-create the Mod Shell using the 3 essential props: 
                // ModId (Guid), RootSourceUrl (for crawler), and Name
                var restoredShell = new Mod
                {
                    Id = history.ModId,
                    AppId = history.ModdedAppId,
                    Name = history.Name,
                    RootSourceUrl = history.RootSourceUrl,
                    Description = history.Description ?? "Restored from Retired History",
                    Author = history.Author,
                    PriorityOrder = int.MaxValue, // Default to lowest priority; user can adjust after restore

                };

                var restoredConfig = new ModCrawlerConfig
                {
                    ModId = history.ModId,
                    WatcherXPath = history.WatcherXPath,
                    ModNameRegex = history.ModNameRegex,
                    VersionXPath = history.VersionXPath,
                    ReleaseDateXPath = history.ReleaseDateXPath,
                    SizeXPath = history.SizeXPath,
                    DownloadUrlXPath = history.DownloadUrlXPath,
                    SupportedAppVersionsXPath = history.SupportedAppVersionsXPath,
                    PackageFilesNumberXPath = history.PackageFilesNumberXPath
                };

                // 2. Insert the restored shell into the active Mods table
                await _modRepo.InsertAsync(restoredShell, connection, transaction);

                await _modCrawlerConfigRepo.InsertAsync(restoredConfig, connection, transaction);

                // 3. Remove the snapshot from history now that it is back in the library
                await _unUsedModRepo.DeleteAsync(history.Id, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                _logger.LogError("Error occurred while restoring mod from history. ModId: {ModId}, Name: {Name}, AppId: {AppId}", history.ModId, history.Name, history.ModdedAppId);
                throw;
            }
        }

        #endregion

        #region Installed Mod History Methods
        public async Task<IEnumerable<InstalledModHistory>> GetInstalledModHistoryAsync(Guid modId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            // Fetch historical version snapshots for this specific mod shell
            // This uses the IInstalledModHistoryRepository you'll have in your Data layer
            return await _installedModHistoryRepo.FindByModIdAsync(modId, connection);
        }

        public async Task RollbackToVersionAsync(InstalledModHistory target, string appVersion)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                _logger.LogInformation("Rolling back to version from history. ModId: {ModId}, Version: {Version}, AppVersion: {AppVersion}", target.ModId, target.Version, appVersion);
                // 1. Get current active record to archive it
                InstalledMod? currentActive = await _installedModRepo.FindByModIdAsync(target.ModId, connection, transaction);

                if (currentActive != null)
                {
                    // 2. Archive the version we are about to replace
                    var archiveRecord = new InstalledModHistory
                    {
                        ModId = currentActive.Id,
                        Version = currentActive.InstalledVersion,
                        AppVersion = string.IsNullOrEmpty(currentActive.SupportedAppVersions) ? appVersion : currentActive.SupportedAppVersions,
                        InstalledAt = currentActive.InstalledDate,
                        RemovedAt = DateOnly.FromDateTime(DateTime.Now),
                        DownloadUrl = currentActive.DownloadUrl,

                    };
                    await _installedModHistoryRepo.InsertAsync(archiveRecord, connection, transaction);

                    // 3. Promote the target history version back to the active record
                    currentActive.InstalledVersion = target.Version;
                    currentActive.InstalledDate = DateOnly.FromDateTime(DateTime.Now);
                    currentActive.DownloadUrl = target.DownloadUrl;


                    await _installedModRepo.UpdateAsync(currentActive, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                _logger.LogError("Error occurred while rolling back to version from history. ModId: {ModId}, Version: {Version}, AppVersion: {AppVersion}", target.ModId, target.Version, appVersion);
                throw;
            }
        }

        public async Task DeleteInstalledModHistoryAsync(int historyId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            await _installedModHistoryRepo.DeleteAsync(historyId, connection);
        }
        #endregion

        #region Hard Wipe Logic

        public async Task HardWipeAppAsync(int appId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                _logger.LogInformation("Performing hard wipe for AppId: {AppId}", appId);
                // 1. Wipe Unused History for this app
                await _unUsedModRepo.DeleteByAppIdAsync(appId, connection, transaction);

                // 2. Wipe sub-tables (Order doesn't matter among these three)
                await _installedModRepo.DeleteByAppIdAsync(appId, connection, transaction);
                await _availableModRepo.DeleteByAppIdAsync(appId, connection, transaction);
                await _installedModHistoryRepo.DeleteByAppIdAsync(appId, connection, transaction);
                await _modCrawlerConfigRepo.DeleteByAppIdAsync(appId, connection, transaction);

                // 3. Wipe Mod shells
                await _modRepo.DeleteByAppIdAsync(appId, connection, transaction);

                // 4. Wipe the App itself
                await _appRepo.DeleteAsync(appId, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                _logger.LogError("Error occurred during hard wipe for AppId: {AppId}", appId);
                throw;
            }
        }

        public async Task HardWipeModAsync(Mod mod, ModdedApp parentApp, ModCrawlerConfig modConfig, string wipeReason)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                
                _logger.LogInformation("Performing hard wipe for ModId: {ModId}, Name: {Name}, AppId: {AppId}. Reason: {Reason}", mod.Id, mod.Name, mod.AppId, wipeReason);
                // 1. Create the shell-based history record
                var history = new UnusedModHistory
                {
                    ModId = mod.Id,
                    ModdedAppId = mod.AppId,
                    Name = mod.Name,
                    AppName = parentApp.Name,
                    AppVersion = parentApp.InstalledVersion ?? "Unknown",
                    RootSourceUrl = mod.RootSourceUrl,
                    RemovedAt = DateOnly.FromDateTime(DateTime.Now),
                    Reason = wipeReason,
                    Description = mod.Description,
                    Author = mod.Author,

                    WatcherXPath = modConfig?.WatcherXPath ?? string.Empty,
                    ModNameRegex = modConfig?.ModNameRegex ?? string.Empty,
                    VersionXPath = modConfig?.VersionXPath ?? string.Empty,
                    ReleaseDateXPath = modConfig?.ReleaseDateXPath ?? string.Empty,
                    SizeXPath = modConfig?.SizeXPath ?? string.Empty,
                    DownloadUrlXPath = modConfig?.DownloadUrlXPath ?? string.Empty,
                    SupportedAppVersionsXPath = modConfig?.SupportedAppVersionsXPath ?? string.Empty,
                    PackageFilesNumberXPath = modConfig?.PackageFilesNumberXPath ?? string.Empty

                };

                await _unUsedModRepo.InsertAsync(history, connection, transaction);

                // 2. Perform bulk deletions in sub-tables (repos call your DeleteByModId logic)
                await _installedModRepo.DeleteByModIdAsync(mod.Id, connection, transaction);
                await _availableModRepo.DeleteByModIdAsync(mod.Id, connection, transaction);
                await _installedModHistoryRepo.DeleteByModIdAsync(mod.Id, connection, transaction);
                await _modCrawlerConfigRepo.DeleteByModIdAsync(mod.Id, connection, transaction);

                // 3. Delete the Mod Shell itself
                await _modRepo.DeleteAsync(mod.Id, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                _logger.LogError("Error occurred during hard wipe for ModId: {ModId}, Name: {Name}, AppId: {AppId}. Reason: {Reason}", mod.Id, mod.Name, mod.AppId, wipeReason);
                throw;
            }
        }

        #endregion

        #region Available Versions Methods

        public async Task<IEnumerable<(Mod Shell, IEnumerable<AvailableMod> Versions)>> GetAvailableVersionsByAppIdAsync(int appId, Guid? modId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            // 1. Get Shells (Filter by specific modId if provided, else all for app)
            var shells = await _modRepo.GetByAppIdAsync(appId, connection);
            if (modId.HasValue)
                shells = shells.Where(s => s.Id == modId.Value);

            var results = new List<(Mod, IEnumerable<AvailableMod>)>();

            foreach (var shell in shells)
            {
                // 2. Fetch crawled versions for each shell
                var versions = await _availableModRepo.FindByModIdAsync(shell.Id, connection);
                if (versions != null && versions.Count() > 0)
                {
                    results.Add((shell, versions));
                }
            }

            return results;
        }

        public async Task SaveCrawledVersionsAsync(Guid modId, IEnumerable<AvailableMod> versions)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {

                foreach (AvailableMod v in versions)
                {
                    v.Id = modId;
                    v.LastCrawled = DateTime.Now;
                    await _availableModRepo.InsertAsync(v, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task PromoteAvailableToInstalledAsync(
    AvailableMod selected,
    string appVersion,
    IDbConnection? connection = null,
    IDbTransaction? transaction = null)
        {
            // If no connection is passed, we create and manage our own
            bool isInternalConn = connection == null;
            var conn = connection ?? _connectionFactory.CreateConnection();
            bool isNew = false;

            try
            {
                _logger.LogInformation("Promoting AvailableMod to InstalledMod. Mod Name: {ModId}, Version: {Version}, AppVersion: {AppVersion}", selected.Name, selected.AvailableVersion, appVersion);

                if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                // 1. Archive current active mod to History
                var currentActive = await _installedModRepo.FindByModIdAsync(selected.Id, conn, transaction);

                isNew = currentActive == null;

                if (!isNew)
                {
                    var history = new InstalledModHistory
                    {
                        ModId = currentActive.Id,
                        Version = currentActive.InstalledVersion,
                        AppVersion = string.IsNullOrEmpty(currentActive.SupportedAppVersions)? appVersion : currentActive.SupportedAppVersions,
                        InstalledAt = currentActive.InstalledDate,
                        RemovedAt = DateOnly.FromDateTime(DateTime.Now),
                        DownloadUrl = currentActive.DownloadUrl

                    };
                    await _installedModHistoryRepo.InsertAsync(history, conn, transaction);

                }
                else
                {
                    currentActive = new InstalledMod
                    {
                        Id = selected.Id
                        
                    };
                }

                // 2. Update active record with new version info
                currentActive.InstalledVersion = selected.AvailableVersion;
                currentActive.InstalledDate = DateOnly.FromDateTime(DateTime.Now);
                currentActive.InstalledSizeMB = selected.SizeMB;
                currentActive.PackageType = selected.PackageType;
                currentActive.PackageFilesNumber = selected.PackageFilesNumber;
                currentActive.SupportedAppVersions = selected.SupportedAppVersions;
                currentActive.DownloadUrl = selected.DownloadUrl;

                if (isNew)
                {
                    await _installedModRepo.InsertAsync(currentActive, conn, transaction);
                }
                else
                {
                    await _installedModRepo.UpdateAsync(currentActive, conn, transaction);
                }
            }catch
            {
                _logger.LogError("Error occurred while promoting AvailableMod to InstalledMod. Mod Name: {ModId}, Version: {Version}, AppVersion: {AppVersion}", selected.Name, selected.AvailableVersion, appVersion);
                throw;
            }
            finally
            {
                // Only close/dispose if WE opened it. 
                // If it was passed in, the caller (ProcessCrawlResultsAsync) owns it.
                if (isInternalConn) conn.Dispose();
            }
        }

        public async Task DeleteAvailableModAsync(int internalId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            // Pattern: Service manages connection, Repository performs the action
            await _availableModRepo.DeleteAsync(internalId, connection);
        }

        public async Task DeleteAvailableModsBatchAsync(IEnumerable<int> internalIds)
        {
            if (internalIds == null || !internalIds.Any()) return;

            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                _logger.LogInformation("Deleting batch of AvailableMods. Count: {Count}", internalIds.Count());
                // Pattern: Loop through the batch using the repository within a transaction
                foreach (var id in internalIds)
                {
                    await _availableModRepo.DeleteAsync(id, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                _logger.LogError("Error occurred while deleting batch of AvailableMods. Count: {Count}", internalIds.Count());
                throw;
            }
        }

        private bool IsModChanged(AvailableMod local, AvailableMod web)
        {
            // Check if critical metadata has drifted
            return local.DownloadUrl != web.DownloadUrl ||
                   local.ReleaseDate != web.ReleaseDate ||
                   local.PackageFilesNumber != web.PackageFilesNumber ||
                   local.PackageType != web.PackageType ||
                   local.SupportedAppVersions != web.SupportedAppVersions ||
                   local.SizeMB != web.SizeMB;
        }

        #endregion

        #region watchers

        public async Task<IEnumerable<(Mod Shell, ModCrawlerConfig Config)>> GetWatchableBundleByAppIdAsync(int appId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            // 1. Get all mods for this app that are flagged as watchable
            var watchableMods = (await _modRepo.GetByAppIdAsync(appId, connection))
                                .Where(m => m.IsWatchable && m.IsUsed);

            var results = new List<(Mod, ModCrawlerConfig)>();

            foreach (var mod in watchableMods)
            {
                // 2. Get the config (Stage 1 WatcherXPath is here)
                var config = await _modCrawlerConfigRepo.GetByModIdAsync(mod.Id, connection);
                if (config != null)
                {
                    results.Add((mod, config));
                }
            }

            return results;
        }

        public async Task<InstalledMod?> ProcessCrawlResultsAsync(string appVersion, Guid shellId, AvailableMod? primary, List<AvailableMod> scrapedMods)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            InstalledMod? result = null;

            try
            {
                _logger.LogInformation("Processing crawl results for ModId: {ModId}, AppVersion: {AppVersion}. Primary Version: {PrimaryVersion}, Scraped Count: {ScrapedCount}", shellId, appVersion, primary?.AvailableVersion, scrapedMods.Count);
                // 1. Get existing versions to avoid duplicates in the history/available list
                var existingVersions = await _availableModRepo.FindByModIdAsync(shellId, connection, transaction);

                foreach (var scraped in scrapedMods)
                {
                    if (!IsDuplicate(scraped, existingVersions))
                    {
                        // Ensure the Foreign Key / ID is set correctly
                        scraped.Id = shellId;
                        scraped.LastCrawled = DateTime.Now;
                        await _availableModRepo.InsertAsync(scraped, connection, transaction);
                    }
                }

                // 2. If a primary version was identified (the update), promote it
                if (primary != null)
                {

                    // REUSE: Call the existing promotion logic
                    // Note: Since we are already in a transaction, ensure Promote handles the passed connection/transaction
                    await PromoteAvailableToInstalledAsync(primary, appVersion, connection, transaction);

                    result = await _installedModRepo.FindByModIdAsync(shellId, connection, transaction);

                }

                transaction.Commit();

                return result;
            }
            catch
            {
                transaction.Rollback();
                _logger.LogError("Error occurred while processing crawl results for ModId: {ModId}, AppVersion: {AppVersion}. Primary Version: {PrimaryVersion}, Scraped Count: {ScrapedCount}", shellId, appVersion, primary?.AvailableVersion, scrapedMods.Count);
                throw;
            }
            finally {
            
            }
        }

        #endregion

        #region Installed Mod Methods

        public async Task SaveInstalledModAsync(InstalledMod installedMod)
        {
            using var connection = _connectionFactory.CreateConnection();
            // Ensure the connection is open for the repository
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            await _installedModRepo.InsertAsync(installedMod, connection);
        }

        public async Task UpdateInstalledModAsync(InstalledMod installedMod)
        {
            using var connection = _connectionFactory.CreateConnection();
            // Ensure the connection is open for the repository
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            await _installedModRepo.UpdateAsync(installedMod, connection);
        }

        #endregion

       
        private bool IsDuplicate(AvailableMod scraped, IEnumerable<AvailableMod> existing)
        {
            return existing.Any(e =>
                // Logic A: Same page and same version string
                (e.CrawledModUrl == scraped.CrawledModUrl && e.AvailableVersion == scraped.AvailableVersion) ||

                // Logic B: Normalized version (ignoring 'v' or spaces) and same game support
                (_commonUtils.NormalizeVersion(e.AvailableVersion) == _commonUtils.NormalizeVersion(scraped.AvailableVersion) &&
                 e.SupportedAppVersions == scraped.SupportedAppVersions) ||

                // Logic C: The exact same download link
                (!string.IsNullOrEmpty(e.DownloadUrl) && e.DownloadUrl == scraped.DownloadUrl)
            );
        }

        

        
    }
}