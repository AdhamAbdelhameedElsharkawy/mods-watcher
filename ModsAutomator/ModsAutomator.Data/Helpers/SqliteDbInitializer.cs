using System.Data;
using Dapper;

namespace ModsAutomator.Data.Helpers
{
    public static class SqliteDbInitializer
    {
        public static async Task InitializeAsync(IDbConnection conn)
        {
            // Register the handler here
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            var sql = @"

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
    RootSourceUrl TEXT,
    IsDeprecated INTEGER NOT NULL,
    Description TEXT,
    IsUsed INTEGER NOT NULL,
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
    PriorityOrder INTEGER,
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
    AppVersion TEXT,
    RemovedAt TEXT,
    Reason TEXT,
    Description TEXT
);

";
            await conn.ExecuteAsync(sql);
        }
    }

}
