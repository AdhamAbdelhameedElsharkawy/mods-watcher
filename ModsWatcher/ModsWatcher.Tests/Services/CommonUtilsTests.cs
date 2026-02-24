using Microsoft.Extensions.Options;
using ModsWatcher.Core.Entities;
using ModsWatcher.Core.Enums;
using ModsWatcher.Services;
using ModsWatcher.Services.Config;
using Moq;
using Xunit;

namespace ModsWatcher.Tests.Services
{
    public class CommonUtilsTests
    {
        private readonly Mock<IOptions<WatcherSettings>> _optionsMock;
        private readonly CommonUtils _utils;

        public CommonUtilsTests()
        {
            _optionsMock = new Mock<IOptions<WatcherSettings>>();

            // Default config setup
            _optionsMock.Setup(o => o.Value).Returns(new WatcherSettings
            {
                CheckingThresholdHours = 24
            });

            _utils = new CommonUtils(_optionsMock.Object);
        }

        #region Size Parsing Tests

        [Theory]
        [InlineData("150 MB", 150)]
        [InlineData("1.5 GB", 1.5)]
        [InlineData("500", 500)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("Size: 25.5MB", 25.5)]
        public void ParseSize_ShouldExtractCleanDecimal(string input, decimal expected)
        {
            // Act
            var result = _utils.ParseSize(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Package Type Tests

        [Theory]
        [InlineData("https://site.com/mod.zip", PackageType.Zip)]
        [InlineData("mod.RAR", PackageType.Rar)]
        [InlineData("file.7z", PackageType.SevenZip)]
        [InlineData("truck.scs", PackageType.Scs)]
        [InlineData("image.png", PackageType.Unknown)]
        [InlineData(null, PackageType.Unknown)]
        public void GetPackageTypeFromUrl_ShouldReturnCorrectEnum(string url, PackageType expected)
        {
            // Act
            var result = _utils.GetPackageTypeFromUrl(url);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Version Normalization & Compatibility

        [Theory]
        [InlineData("v1.2.3", "1.2.3")]
        [InlineData("  V2.0  ", "2.0")]
        [InlineData(null, "")]
        public void NormalizeVersion_ShouldTrimAndRemoveVPrefix(string input, string expected)
        {
            // Act
            var result = _utils.NormalizeVersion(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsModCompatibleWithAppVersion_ShouldMatchPartialStrings()
        {
            // Arrange
            string modVer = "Built for 1.50.x";
            string appVer = "1.50";

            // Act
            var result = _utils.IsModCompatibleWithAppVersion(modVer, appVer);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Watcher Status Tests

        [Fact]
        public void CanCheckModWatcherStatus_ShouldReturnTrue_WhenThresholdExceeded()
        {
            // Arrange
            var shell = new Mod { LastWatched = DateTime.Now.AddHours(-25) };

            // Act
            var result = _utils.CanCheckModWatcherStatus(shell);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanCheckModWatcherStatus_ShouldReturnFalse_WhenCheckedRecently()
        {
            // Arrange
            var shell = new Mod { LastWatched = DateTime.Now.AddHours(-1) };

            // Act
            var result = _utils.CanCheckModWatcherStatus(shell);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region URL & Hash Tests

        [Theory]
        [InlineData("http://valid.com", true)]
        [InlineData("https://valid.com/page", true)]
        [InlineData("ftp://invalid.com", false)]
        [InlineData("not-a-url", false)]
        public void IsValidUrl_ShouldValidateProtocolAndFormat(string url, bool expected)
        {
            // Act
            var result = _utils.IsValidUrl(url);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateMd5Hash_ShouldReturnLowercaseHex()
        {
            // Arrange
            string input = "test-string";
            string expected = "661f8009fa8e56a9d0e94a0a644397d7"; // Known MD5 for 'test-string'

            // Act
            var result = _utils.GenerateMd5Hash(input);

            // Assert
            Assert.Equal(expected, result);
            // Verify lowercase
            Assert.Equal(result.ToLower(), result);
        }

        #endregion
    }
}