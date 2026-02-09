using ModsAutomator.Core.DTO;
using ModsAutomator.Core.Entities;
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

        // We inject the Repository and the ConnectionFactory
        public StorageService(
            IConnectionFactory connectionFactory,
            IModdedAppRepository appRepo,
            IModRepository modRepo,
            IInstalledModRepository installedModRepo,
            IUnusedModHistoryRepository unUsedModRepo)
        {
            _connectionFactory = connectionFactory;
            _appRepo = appRepo;
            _modRepo = modRepo;
            _installedModRepo = installedModRepo;
            _unUsedModRepo = unUsedModRepo;
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
    }
}