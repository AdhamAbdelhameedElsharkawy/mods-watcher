using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using ModsAutomator.Core.Interfaces;
using ModsAutomator.Data.Interfaces;
using ModsAutomator.Services.Interfaces;

namespace ModsAutomator.Services
{
    public class StorageService : IStorageService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IModdedAppRepository _appRepo;
        private readonly IModRepository _modRepo;
        private readonly IInstalledModRepository _installedModRepo;
        private readonly IUnusedModHistoryRepository _unUsedModRepo;
        private readonly IInstalledModHistoryRepository _installedModHistoryRepo;
        private readonly IAvailableModRepository _availableModRepo;

        // We inject the Repository and the ConnectionFactory
        public StorageService(
            IConnectionFactory connectionFactory,
            IModdedAppRepository appRepo,
            IModRepository modRepo,
            IInstalledModRepository installedModRepo,
            IUnusedModHistoryRepository unUsedModRepo,
            IInstalledModHistoryRepository installedModHistoryRepo,
            IAvailableModRepository availableModRepo)
        {
            _connectionFactory = connectionFactory;
            _appRepo = appRepo;
            _modRepo = modRepo;
            _installedModRepo = installedModRepo;
            _unUsedModRepo = unUsedModRepo;
            _installedModHistoryRepo = installedModHistoryRepo;
            _availableModRepo = availableModRepo;
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

            // 1. Get all apps using the existing connection
            var apps = await _appRepo.QueryAllAsync(connection);
            var summaries = new List<AppSummaryDto>();

            // 2. Loop and fetch stats for each app using the SAME connection
            foreach (var app in apps)
            {
                var stats = await _installedModRepo.GetAppSummaryStatsAsync(app.Id, app.InstalledVersion, connection);

                summaries.Add(new AppSummaryDto
                {
                    App = app,
                    ActiveCount = stats.ActiveCount,
                    TotalSize = stats.TotalSize,
                    IncompatibleCount = stats.IncompatibleCount
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


        public async Task<IEnumerable<(Mod Shell, InstalledMod? Installed)>> GetModsByAppId(int appId)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            // 1. Fetch all Mod Shells belonging to this App
            // Assuming you have a ModRepository injected or accessible
            var shells = await _modRepo.GetByAppIdAsync(appId, connection);

            var results = new List<(Mod, InstalledMod?)>();

            foreach (Mod shell in shells)
            {
                // 2. Fetch the current installation record for this shell
                var installed = await _installedModRepo.FindByModIdAsync(shell.Id, connection);
                results.Add((shell, installed));
            }

            return results;
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
                // 1. Re-create the Mod Shell using the 3 essential props: 
                // ModId (Guid), RootSourceUrl (for crawler), and Name
                var restoredShell = new Mod
                {
                    Id = history.ModId,
                    AppId = history.ModdedAppId,
                    Name = history.Name,
                    RootSourceUrl = history.RootSourceUrl,
                    Description = history.Description ?? "Restored from Retired History"
                };

                // 2. Insert the restored shell into the active Mods table
                await _modRepo.InsertAsync(restoredShell, connection, transaction);

                // 3. Remove the snapshot from history now that it is back in the library
                await _unUsedModRepo.DeleteAsync(history.Id, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
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
                // 1. Get current active record to archive it
                InstalledMod? currentActive = await _installedModRepo.FindByModIdAsync(target.ModId, connection, transaction);

                if (currentActive != null)
                {
                    // 2. Archive the version we are about to replace
                    var archiveRecord = new InstalledModHistory
                    {
                        ModId = currentActive.Id,
                        Version = currentActive.InstalledVersion,
                        AppVersion = appVersion,
                        InstalledAt = currentActive.InstalledDate,
                        RemovedAt = DateOnly.FromDateTime(DateTime.Now)
                    };
                    await _installedModHistoryRepo.InsertAsync(archiveRecord, connection, transaction);

                    // 3. Promote the target history version back to the active record
                    //TODO:R2, refine logic (thorugh AvailableMods?) to get accurate InstalledDate, Size, PackageType, etc. or drive InstalledModHistory from InstalledMod records instead of just snapshots of version/appversion?
                    currentActive.InstalledVersion = target.Version;

                    await _installedModRepo.UpdateAsync(currentActive, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
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
                // 1. Wipe Unused History for this app
                await _unUsedModRepo.DeleteByAppIdAsync(appId, connection, transaction);

                // 2. Wipe sub-tables (Order doesn't matter among these three)
                await _installedModRepo.DeleteByAppIdAsync(appId, connection, transaction);
                await _availableModRepo.DeleteByAppIdAsync(appId, connection, transaction);
                await _installedModHistoryRepo.DeleteByAppIdAsync(appId, connection, transaction);

                // 3. Wipe Mod shells
                await _modRepo.DeleteByAppIdAsync(appId, connection, transaction);

                // 4. Wipe the App itself
                await _appRepo.DeleteAsync(appId, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task HardWipeModAsync(Mod mod, ModdedApp parentApp)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
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
                    Reason = "User Hard Wipe" // Default until prompt is added
                };

                await _unUsedModRepo.InsertAsync(history, connection, transaction);

                // 2. Perform bulk deletions in sub-tables (repos call your DeleteByModId logic)
                await _installedModRepo.DeleteByModIdAsync(mod.Id, connection, transaction);
                await _availableModRepo.DeleteByModIdAsync(mod.Id, connection, transaction);
                await _installedModHistoryRepo.DeleteByModIdAsync(mod.Id, connection, transaction);

                // 3. Delete the Mod Shell itself
                await _modRepo.DeleteAsync(mod.Id, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
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
                results.Add((shell, versions));
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

        public async Task PromoteAvailableToInstalledAsync(AvailableMod selected, string appVersion)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Archive current active mod to History
                var currentActive = await _installedModRepo.FindByModIdAsync(selected.Id, connection, transaction);
                if (currentActive != null)
                {
                    var history = new InstalledModHistory
                    {
                        ModId = currentActive.Id,
                        Version = currentActive.InstalledVersion,
                        AppVersion = appVersion,
                        InstalledAt = currentActive.InstalledDate,
                        RemovedAt = DateOnly.FromDateTime(DateTime.Now)
                    };
                    await _installedModHistoryRepo.InsertAsync(history, connection, transaction);

                    // 2. Update active record with new version info
                    currentActive.InstalledVersion = selected.AvailableVersion;
                    currentActive.InstalledDate = DateOnly.FromDateTime(DateTime.Now);
                    currentActive.InstalledSizeMB = selected.SizeMB;
                    currentActive.PackageType = selected.PackageType;
                    currentActive.PackageFilesNumber = selected.PackageFilesNumber;
                    currentActive.SupportedAppVersions = selected.SupportedAppVersions;

                    await _installedModRepo.UpdateAsync(currentActive, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<(AvailableMod Entity, SyncChangeType Type)>> CompareAndIdentifyChangesAsync(Guid modId, int appId, List<AvailableMod> webVersions)
        {
            var changes = new List<(AvailableMod Entity, SyncChangeType Type)>();

            // 1. Get the current local state for this specific mod
            var localData = await GetAvailableVersionsByAppIdAsync(appId, modId);
            var localVersions = localData.SelectMany(x => x.Versions).ToList();

            // 2. Identify NEW or MODIFIED items
            foreach (var webMod in webVersions)
            {
                var localMatch = localVersions.FirstOrDefault(l => l.AvailableVersion == webMod.AvailableVersion);

                if (localMatch == null)
                {
                    // Brand new version found online
                    changes.Add((webMod, SyncChangeType.New));
                }
                else if (IsModChanged(localMatch, webMod))
                {
                    // Version exists but data (URL, Date, Compatibility) is different
                    changes.Add((webMod, SyncChangeType.Modified));
                }
            }

            // 3. Identify STALE items (in local storage but no longer on Web)
            foreach (var localMod in localVersions)
            {
                if (!webVersions.Any(w => w.AvailableVersion == localMod.AvailableVersion))
                {
                    changes.Add((localMod, SyncChangeType.Stale));
                }
            }

            return changes;
        }

        public async Task CommitSyncChangeAsync(Guid modId, AvailableMod entity, SyncChangeType type)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                entity.Id = modId;
                entity.LastCrawled = DateTime.Now;

                switch (type)
                {
                    case SyncChangeType.New:
                        await _availableModRepo.InsertAsync(entity, connection, transaction);
                        break;

                    case SyncChangeType.Modified:
                        await _availableModRepo.UpdateAsync(entity, connection, transaction);
                        break;

                    case SyncChangeType.Stale:
                        await _availableModRepo.DeleteAsync(entity.InternalId, connection, transaction);
                        break;
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private bool IsModChanged(AvailableMod local, AvailableMod web)
        {
            // Check if critical metadata has drifted
            return local.DownloadUrl != web.DownloadUrl ||
                   local.ReleaseDate != web.ReleaseDate ||
                   local.PackageFilesNumber != web.PackageFilesNumber||
                   local.PackageType != web.PackageType||
                   local.SupportedAppVersions != web.SupportedAppVersions||
                   local.SizeMB != web.SizeMB;
        }

        #endregion
    }
}