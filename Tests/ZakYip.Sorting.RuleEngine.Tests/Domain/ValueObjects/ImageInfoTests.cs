using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.ValueObjects;

namespace ZakYip.Sorting.RuleEngine.Tests.Domain.ValueObjects;

public class ImageInfoTests
{
    [Fact]
    public void Constructor_WithRequiredProperties_ShouldInitializeCorrectly()
    {
        // Arrange
        var deviceName = "Camera01";
        var localPath = @"D:\images\2024\11\12\image001.jpg";

        // Act
        var imageInfo = new ImageInfo
        {
            DeviceName = deviceName,
            LocalPath = localPath
        };

        // Assert
        Assert.NotNull(imageInfo);
        Assert.Equal(deviceName, imageInfo.DeviceName);
        Assert.Equal(localPath, imageInfo.LocalPath);
        Assert.Null(imageInfo.CapturedAt);
    }

    [Fact]
    public void Constructor_WithAllProperties_ShouldInitializeCorrectly()
    {
        // Arrange
        var deviceName = "Camera01";
        var localPath = @"D:\images\2024\11\12\image001.jpg";
        var capturedAt = DateTime.Now;

        // Act
        var imageInfo = new ImageInfo
        {
            DeviceName = deviceName,
            LocalPath = localPath,
            CapturedAt = capturedAt
        };

        // Assert
        Assert.Equal(deviceName, imageInfo.DeviceName);
        Assert.Equal(localPath, imageInfo.LocalPath);
        Assert.Equal(capturedAt, imageInfo.CapturedAt);
    }

    [Fact]
    public void Record_ShouldSupportValueEquality()
    {
        // Arrange
        var deviceName = "Camera01";
        var localPath = @"D:\images\2024\11\12\image001.jpg";
        var capturedAt = DateTime.Now;

        var imageInfo1 = new ImageInfo
        {
            DeviceName = deviceName,
            LocalPath = localPath,
            CapturedAt = capturedAt
        };

        var imageInfo2 = new ImageInfo
        {
            DeviceName = deviceName,
            LocalPath = localPath,
            CapturedAt = capturedAt
        };

        // Act & Assert
        Assert.Equal(imageInfo1, imageInfo2);
    }

    [Fact]
    public void Record_ShouldSupportWith_Expression()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            DeviceName = "Camera01",
            LocalPath = @"D:\images\test.jpg"
        };
        
        var newCapturedAt = DateTime.Now;

        // Act
        var updatedImageInfo = imageInfo with { CapturedAt = newCapturedAt };

        // Assert
        Assert.Equal("Camera01", updatedImageInfo.DeviceName);
        Assert.Equal(@"D:\images\test.jpg", updatedImageInfo.LocalPath);
        Assert.Equal(newCapturedAt, updatedImageInfo.CapturedAt);
        Assert.Null(imageInfo.CapturedAt); // Original should be unchanged
    }
}
