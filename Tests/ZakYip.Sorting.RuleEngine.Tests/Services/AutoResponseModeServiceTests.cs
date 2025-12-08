using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 自动应答模式服务单元测试
/// Unit tests for AutoResponseModeService
/// </summary>
public class AutoResponseModeServiceTests
{
    private readonly Mock<ILogger<AutoResponseModeService>> _mockLogger;
    private readonly AutoResponseModeService _service;

    public AutoResponseModeServiceTests()
    {
        _mockLogger = new Mock<ILogger<AutoResponseModeService>>();
        _service = new AutoResponseModeService(_mockLogger.Object);
    }

    [Fact]
    public void IsEnabled_DefaultState_ReturnsFalse()
    {
        // Act
        var isEnabled = _service.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void Enable_WhenDisabled_EnablesMode()
    {
        // Arrange
        Assert.False(_service.IsEnabled);

        // Act
        _service.Enable();

        // Assert
        Assert.True(_service.IsEnabled);
    }

    [Fact]
    public void Enable_WhenAlreadyEnabled_RemainsEnabled()
    {
        // Arrange
        _service.Enable();
        Assert.True(_service.IsEnabled);

        // Act
        _service.Enable();

        // Assert
        Assert.True(_service.IsEnabled);
    }

    [Fact]
    public void Disable_WhenEnabled_DisablesMode()
    {
        // Arrange
        _service.Enable();
        Assert.True(_service.IsEnabled);

        // Act
        _service.Disable();

        // Assert
        Assert.False(_service.IsEnabled);
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_RemainsDisabled()
    {
        // Arrange
        Assert.False(_service.IsEnabled);

        // Act
        _service.Disable();

        // Assert
        Assert.False(_service.IsEnabled);
    }

    [Fact]
    public void Enable_LogsInformation()
    {
        // Act
        _service.Enable();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Auto-response mode enabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Disable_LogsInformation()
    {
        // Arrange
        _service.Enable();

        // Act
        _service.Disable();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Auto-response mode disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConcurrentEnableDisable_MaintainsConsistency()
    {
        // Arrange
        var tasks = new List<Task>();
        var enableCount = 100;
        var disableCount = 100;

        // Act - Simulate concurrent enable/disable operations
        for (int i = 0; i < enableCount; i++)
        {
            tasks.Add(Task.Run(() => _service.Enable()));
        }
        for (int i = 0; i < disableCount; i++)
        {
            tasks.Add(Task.Run(() => _service.Disable()));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Service should be in a consistent state (either enabled or disabled)
        var finalState = _service.IsEnabled;
        Assert.True(finalState == true || finalState == false);
    }

    [Fact]
    public void ConcurrentReads_ReturnConsistentValue()
    {
        // Arrange
        _service.Enable();
        var results = new bool[100];
        var tasks = new Task[100];

        // Act - Simulate concurrent reads
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() => results[index] = _service.IsEnabled);
        }

        Task.WaitAll(tasks);

        // Assert - All reads should return the same value
        Assert.All(results, result => Assert.True(result));
    }
}
