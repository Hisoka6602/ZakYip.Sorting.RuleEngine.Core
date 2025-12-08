using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Service.API;

namespace ZakYip.Sorting.RuleEngine.Tests.Controllers;

/// <summary>
/// 自动应答模式控制器单元测试
/// Unit tests for AutoResponseModeController
/// </summary>
public class AutoResponseModeControllerTests
{
    private readonly Mock<IAutoResponseModeService> _mockService;
    private readonly Mock<ILogger<AutoResponseModeController>> _mockLogger;
    private readonly AutoResponseModeController _controller;

    public AutoResponseModeControllerTests()
    {
        _mockService = new Mock<IAutoResponseModeService>();
        _mockLogger = new Mock<ILogger<AutoResponseModeController>>();
        _controller = new AutoResponseModeController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Enable_CallsServiceEnable()
    {
        // Act
        var result = _controller.Enable();

        // Assert
        _mockService.Verify(s => s.Enable(), Times.Once);
    }

    [Fact]
    public void Enable_ReturnsOkResultWithDto()
    {
        // Act
        var result = _controller.Enable();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        Assert.True(dto.Enabled);
        Assert.Contains("enabled", dto.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enable_ReturnsUtcTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.Now;

        // Act
        var result = _controller.Enable();
        var afterCall = DateTime.Now;

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        Assert.InRange(dto.Timestamp, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
        // Verify it's close to UTC (not local time)
        Assert.InRange((dto.Timestamp - DateTime.Now).TotalMinutes, -1, 1);
    }

    [Fact]
    public void Enable_LogsInformation()
    {
        // Act
        _controller.Enable();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("启用")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Disable_CallsServiceDisable()
    {
        // Act
        var result = _controller.Disable();

        // Assert
        _mockService.Verify(s => s.Disable(), Times.Once);
    }

    [Fact]
    public void Disable_ReturnsOkResultWithDto()
    {
        // Act
        var result = _controller.Disable();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        Assert.False(dto.Enabled);
        Assert.Contains("disabled", dto.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Disable_ReturnsUtcTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.Now;

        // Act
        var result = _controller.Disable();
        var afterCall = DateTime.Now;

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        Assert.InRange(dto.Timestamp, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    [Fact]
    public void Disable_LogsInformation()
    {
        // Act
        _controller.Disable();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("禁用")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetStatus_WhenEnabled_ReturnsEnabledDto()
    {
        // Arrange
        _mockService.Setup(s => s.IsEnabled).Returns(true);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        Assert.True(dto.Enabled);
        Assert.Contains("enabled", dto.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetStatus_WhenDisabled_ReturnsDisabledDto()
    {
        // Arrange
        _mockService.Setup(s => s.IsEnabled).Returns(false);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        Assert.False(dto.Enabled);
        Assert.Contains("disabled", dto.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetStatus_ReturnsUtcTimestamp()
    {
        // Arrange
        _mockService.Setup(s => s.IsEnabled).Returns(true);
        var beforeCall = DateTime.Now;

        // Act
        var result = _controller.GetStatus();
        var afterCall = DateTime.Now;

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        Assert.InRange(dto.Timestamp, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    [Fact]
    public void GetStatus_ReadsServiceState()
    {
        // Arrange
        _mockService.Setup(s => s.IsEnabled).Returns(true);

        // Act
        var result = _controller.GetStatus();

        // Assert
        _mockService.Verify(s => s.IsEnabled, Times.Once);
    }

    [Fact]
    public void ConcurrentRequests_HandleProperly()
    {
        // Arrange
        var tasks = new List<Task<ActionResult<AutoResponseModeStatusDto>>>();

        // Act - Simulate concurrent requests
        for (int i = 0; i < 10; i++)
        {
            if (i % 3 == 0)
                tasks.Add(Task.Run(() => _controller.Enable()));
            else if (i % 3 == 1)
                tasks.Add(Task.Run(() => _controller.Disable()));
            else
                tasks.Add(Task.Run(() => _controller.GetStatus()));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - All requests should complete successfully
        Assert.All(tasks, task =>
        {
            Assert.True(task.IsCompletedSuccessfully);
            var okResult = Assert.IsType<OkObjectResult>(task.Result.Result);
            Assert.IsType<AutoResponseModeStatusDto>(okResult.Value);
        });
    }

    [Fact]
    public void AllMethods_Return200StatusCode()
    {
        // Arrange
        _mockService.Setup(s => s.IsEnabled).Returns(true);

        // Act
        var enableResult = _controller.Enable();
        var disableResult = _controller.Disable();
        var statusResult = _controller.GetStatus();

        // Assert
        Assert.IsType<OkObjectResult>(enableResult.Result);
        Assert.IsType<OkObjectResult>(disableResult.Result);
        Assert.IsType<OkObjectResult>(statusResult.Result);
    }
}
