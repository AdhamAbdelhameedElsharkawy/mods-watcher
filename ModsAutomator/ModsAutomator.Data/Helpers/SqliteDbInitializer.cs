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
    RootSourceUrl TEXT
);

CREATE TABLE IF NOT EXISTS ModCrawlerConfig (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ModId TEXT NOT NULL,
    WatcherXPath TEXT,
    LinksCollectionXPath TEXT,
    VersionXPath TEXT,
    ReleaseDateXPath TEXT,
    SizeXPath TEXT,
    DownloadUrlXPath TEXT,
    SupportedAppVersionsXPath TEXT,
    PackageFilesNumberXPath TEXT,
    FOREIGN KEY(ModId) REFERENCES Mod(Id)
);

";
            await conn.ExecuteAsync(sql);
        }
    }

}
