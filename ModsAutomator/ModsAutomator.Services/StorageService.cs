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

        // We inject the Repository and the ConnectionFactory
        public StorageService(
            IConnectionFactory connectionFactory,
            IModdedAppRepository appRepo,
            IModRepository modRepo,
            IInstalledModRepository installedModRepo)
        {
            _connectionFactory = connectionFactory;
            _appRepo = appRepo;
            _modRepo = modRepo;
            _installedModRepo = installedModRepo;
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
    }
}