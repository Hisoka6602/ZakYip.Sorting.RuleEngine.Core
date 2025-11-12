using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Application.Services;

public class ImagePathServiceTests
{
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<ILogger<ImagePathService>> _mockLogger;
    private readonly ImagePathService _service;

    public ImagePathServiceTests()
    {
        _mockLogRepository = new Mock<ILogRepository>();
        _mockLogger = new Mock<ILogger<ImagePathService>>();
        _service = new ImagePathService(_mockLogRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task BulkUpdateImagePathsAsync_WithValidPrefixes_ShouldCallRepository()
    {
        // Arrange
        var oldPrefix = @"D:\images\";
        var newPrefix = @"E:\images\";
        var expectedCount = 1000;
        _mockLogRepository.Setup(x => x.BulkUpdateImagePathsAsync(oldPrefix, newPrefix, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _service.BulkUpdateImagePathsAsync(oldPrefix, newPrefix);

        // Assert
        Assert.Equal(expectedCount, result);
        _mockLogRepository.Verify(x => x.BulkUpdateImagePathsAsync(oldPrefix, newPrefix, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateImagePathsAsync_WithNullOldPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        string? oldPrefix = null;
        var newPrefix = @"E:\images\";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.BulkUpdateImagePathsAsync(oldPrefix!, newPrefix));
    }

    [Fact]
    public async Task BulkUpdateImagePathsAsync_WithEmptyOldPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        var oldPrefix = string.Empty;
        var newPrefix = @"E:\images\";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.BulkUpdateImagePathsAsync(oldPrefix, newPrefix));
    }

    [Fact]
    public async Task BulkUpdateImagePathsAsync_WithNullNewPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        var oldPrefix = @"D:\images\";
        string? newPrefix = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.BulkUpdateImagePathsAsync(oldPrefix, newPrefix!));
    }

    [Fact]
    public void ConvertLocalPathToUrl_WithValidPath_ShouldReturnUrl()
    {
        // Arrange
        var localPath = @"D:\images\2024\11\12\image001.jpg";
        var baseUrl = "http://api.example.com/images";
        var expectedUrl = "http://api.example.com/images/images/2024/11/12/image001.jpg";

        // Act
        var result = _service.ConvertLocalPathToUrl(localPath, baseUrl);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Fact]
    public void ConvertLocalPathToUrl_WithTrailingSlashInBaseUrl_ShouldReturnUrl()
    {
        // Arrange
        var localPath = @"D:\images\2024\11\12\image001.jpg";
        var baseUrl = "http://api.example.com/images/";
        var expectedUrl = "http://api.example.com/images/images/2024/11/12/image001.jpg";

        // Act
        var result = _service.ConvertLocalPathToUrl(localPath, baseUrl);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Fact]
    public void ConvertLocalPathToUrl_WithEmptyLocalPath_ShouldReturnEmpty()
    {
        // Arrange
        var localPath = string.Empty;
        var baseUrl = "http://api.example.com/images";

        // Act
        var result = _service.ConvertLocalPathToUrl(localPath, baseUrl);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConvertLocalPathToUrl_WithNullBaseUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var localPath = @"D:\images\test.jpg";
        string? baseUrl = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.ConvertLocalPathToUrl(localPath, baseUrl!));
    }

    [Fact]
    public void ConvertLocalPathToUrl_WithForwardSlashesInPath_ShouldReturnUrl()
    {
        // Arrange
        var localPath = "D:/images/2024/11/12/image001.jpg";
        var baseUrl = "http://api.example.com/images";
        var expectedUrl = "http://api.example.com/images/images/2024/11/12/image001.jpg";

        // Act
        var result = _service.ConvertLocalPathToUrl(localPath, baseUrl);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Fact]
    public void ConvertLocalPathToUrl_WithRelativePath_ShouldReturnUrl()
    {
        // Arrange
        var localPath = @"images\2024\11\12\image001.jpg";
        var baseUrl = "http://api.example.com";
        var expectedUrl = "http://api.example.com/images/2024/11/12/image001.jpg";

        // Act
        var result = _service.ConvertLocalPathToUrl(localPath, baseUrl);

        // Assert
        Assert.Equal(expectedUrl, result);
    }
}
