using System.Data;
using Dapper;

namespace ModsAutomator.Data.Helpers
{
    public static class SqliteDbInitializer
    {
        public static async Task InitializeAsync(IDbConnection conn)
        {
            // 1. Register Dapper Type Handlers
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            if (conn.State != ConnectionState.Open) conn.Open();

            using var transaction = conn.BeginTransaction();
            try
            {
                // 2. Ensure Version Tracking Table exists
                await conn.ExecuteAsync(
                    "CREATE TABLE IF NOT EXISTS DbSchemaVersion (Version INTEGER NOT NULL);",
                    transaction: transaction);

                // 3. Get current version (default to 0 if table is empty)
                var currentVersion = await conn.ExecuteScalarAsync<int>(
                    "SELECT IFNULL(MAX(Version), 0) FROM DbSchemaVersion;",
                    transaction: transaction);

                // --- VERSION 1: Initial Database Setup ---
                if (currentVersion < 1)
                {
                    var sqlV1 = @"
                        CREATE TABLE IF NOT EXISTS ModdedApp (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT,
                            Description TEXT,
                            LatestVersion TEXT,
                            InstalledVersion TEXT,
                            LastUpdatedDate TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Mod (
                            Id TEXT PRIMARY KEY,
                            AppId INTEGER NOT NULL,
                            Name TEXT NOT NULL,
                            Author TEXT, 
                            RootSourceUrl TEXT,
                            IsDeprecated INTEGER NOT NULL DEFAULT 0,
                            Description TEXT,
                            IsUsed INTEGER NOT NULL,
                            IsWatchable INTEGER NOT NULL DEFAULT 0,
                            IsCrawlable INTEGER NOT NULL DEFAULT 0,
                            LastWatched TEXT,
                            WatcherStatus INTEGER NOT NULL DEFAULT 0,
                            LastWatcherHash TEXT,
                            PriorityOrder INTEGER,
                            FOREIGN KEY(AppId) REFERENCES ModdedApp(Id)
                        );

                        CREATE TABLE IF NOT EXISTS InstalledMod (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ModId TEXT NOT NULL,
                            InstalledVersion TEXT,
                            InstalledDate TEXT,
                            InstalledSizeMB REAL,
                            PackageType INTEGER,
                            PackageFilesNumber INTEGER,
                            SupportedAppVersions TEXT,
                            DownloadUrl TEXT,
                            FOREIGN KEY(ModId) REFERENCES Mod(Id)
                        );

                        CREATE TABLE IF NOT EXISTS AvailableMod (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ModId TEXT NOT NULL,
                            AvailableVersion TEXT,
                            ReleaseDate TEXT,
                            SizeMB REAL,
                            DownloadUrl TEXT,
                            PackageType INTEGER,
                            PackageFilesNumber INTEGER,
                            SupportedAppVersions TEXT,
                            LastCrawled TEXT,
                            CrawledModUrl TEXT, 
                            FOREIGN KEY(ModId) REFERENCES Mod(Id)
                        );

                        CREATE TABLE IF NOT EXISTS InstalledModHistory (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ModId TEXT NOT NULL,
                            Version TEXT,
                            AppVersion TEXT,
                            InstalledAt TEXT,
                            RemovedAt TEXT,
                            LocalFilePath TEXT,
                            IsRollbackTarget INTEGER,
                            FOREIGN KEY(ModId) REFERENCES Mod(Id)
                        );

                        CREATE TABLE IF NOT EXISTS UnusedModHistory (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ModId TEXT NOT NULL,
                            ModdedAppId INTEGER,
                            Name TEXT,
                            Version TEXT,
                            AppName TEXT,
                            AppVersion TEXT,
                            SupportedAppVersions TEXT,
                            RemovedAt TEXT,
                            Reason TEXT,
                            Description TEXT,
                            RootSourceUrl TEXT,
                            WatcherXPath TEXT,
                            ModNameRegex TEXT,
                            VersionXPath TEXT,
                            ReleaseDateXPath TEXT,
                            SizeXPath TEXT,
                            DownloadUrlXPath TEXT,
                            SupportedAppVersionsXPath TEXT,
                            PackageFilesNumberXPath TEXT,
                            Author TEXT
                        );

                        CREATE TABLE IF NOT EXISTS ModCrawlerConfig (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ModId TEXT NOT NULL,
                            WatcherXPath TEXT,
                            ModNameRegex TEXT,
                            VersionXPath TEXT,
                            ReleaseDateXPath TEXT,
                            SizeXPath TEXT,
                            DownloadUrlXPath TEXT,
                            SupportedAppVersionsXPath TEXT,
                            PackageFilesNumberXPath TEXT,
                            FOREIGN KEY(ModId) REFERENCES Mod(Id)
                        );

                        INSERT INTO DbSchemaVersion (Version) VALUES (1);";

                    await conn.ExecuteAsync(sqlV1, transaction: transaction);
                }

                // --- VERSION 2: Your Next Change Goes Here ---
                if (currentVersion < 2)
                {
                    // Renaming 'Author' to 'Creator' in the Mod table
                    await conn.ExecuteAsync("ALTER TABLE InstalledModHistory RENAME COLUMN LocalFilePath TO DownloadUrl;", transaction: transaction);
                    await conn.ExecuteAsync("UPDATE DbSchemaVersion SET Version = 2;", transaction: transaction);
                }


                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw; // Rethrow to see the error in your logs
            }
        }
    }
}