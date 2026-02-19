using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using System.Security.Cryptography;
using System.Text;

namespace ModsAutomator.Desktop.Services
{
    public class CommonUtils
    {
        // Utility for safe size conversion
        public decimal ParseSize(string? input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            // Basic cleaning (removing 'MB', 'GB', spaces)
            var cleaned = new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return decimal.TryParse(cleaned, out var result) ? result : 0;
        }

        public string GenerateMd5Hash(string input)
        {
            byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes).ToLower(); // Lowercase matches Python's hashlib
        }

        

        public PackageType GetPackageTypeFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return PackageType.Unknown;

            var extension = System.IO.Path.GetExtension(url).ToLower().Replace(".", "");

            return extension switch
            {
                "zip" => PackageType.Zip,
                "rar" => PackageType.Rar,
                "scs" => PackageType.Scs,
                "7z" => PackageType.SevenZip,
                _ => PackageType.Unknown
            };
        }

        public bool IsValidUrl(string url) =>
    !string.IsNullOrEmpty(url) && url.StartsWith("http") && Uri.IsWellFormedUriString(url, UriKind.Absolute);


        public string NormalizeVersion(string? version)
        {
            // Return empty string if null to allow safe comparison
            if (string.IsNullOrWhiteSpace(version)) return string.Empty;

            // Standardize: trim, lowercase, and remove the 'v' prefix
            return version.Trim().ToLower().Replace("v", "");
        }

        public bool CanCheckModWatcherStatus(Mod shell)
        {
            bool isRecentlyChecked = DateTime.Now - shell.LastWatched < TimeSpan.FromHours(6);


            return !isRecentlyChecked;
        }

        public bool IsModCompatibleWithAppVersion(string modVersion, string currentAppVersion)
        {

            return modVersion.Trim().ToLower().Contains(currentAppVersion.Trim().ToLower());
        }
    }
}
