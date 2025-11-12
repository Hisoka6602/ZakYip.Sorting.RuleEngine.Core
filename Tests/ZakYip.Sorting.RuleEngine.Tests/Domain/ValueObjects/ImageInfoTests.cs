using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.ValueObjects;

namespace ZakYip.Sorting.RuleEngine.Tests.Domain.ValueObjects;

public class ImageInfoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var imageInfo = new ImageInfo();

        // Assert
        Assert.NotNull(imageInfo);
        Assert.Equal(string.Empty, imageInfo.DeviceName);
        Assert.Equal(string.Empty, imageInfo.LocalPath);
        Assert.Null(imageInfo.CapturedAt);
    }

    [Fact]
    public void Constructor_WithDeviceNameAndLocalPath_ShouldInitializeCorrectly()
    {
        // Arrange
        var deviceName = "Camera01";
        var localPath = @"D:\images\2024\11\12\image001.jpg";

        // Act
        var imageInfo = new ImageInfo(deviceName, localPath);

        // Assert
        Assert.Equal(deviceName, imageInfo.DeviceName);
        Assert.Equal(localPath, imageInfo.LocalPath);
        Assert.Null(imageInfo.CapturedAt);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var deviceName = "Camera01";
        var localPath = @"D:\images\2024\11\12\image001.jpg";
        var capturedAt = DateTime.Now;

        // Act
        var imageInfo = new ImageInfo(deviceName, localPath, capturedAt);

        // Assert
        Assert.Equal(deviceName, imageInfo.DeviceName);
        Assert.Equal(localPath, imageInfo.LocalPath);
        Assert.Equal(capturedAt, imageInfo.CapturedAt);
    }

    [Fact]
    public void SetProperties_ShouldUpdateValues()
    {
        // Arrange
        var imageInfo = new ImageInfo();
        var deviceName = "Camera02";
        var localPath = @"E:\photos\test.jpg";
        var capturedAt = DateTime.Now;

        // Act
        imageInfo.DeviceName = deviceName;
        imageInfo.LocalPath = localPath;
        imageInfo.CapturedAt = capturedAt;

        // Assert
        Assert.Equal(deviceName, imageInfo.DeviceName);
        Assert.Equal(localPath, imageInfo.LocalPath);
        Assert.Equal(capturedAt, imageInfo.CapturedAt);
    }
}
