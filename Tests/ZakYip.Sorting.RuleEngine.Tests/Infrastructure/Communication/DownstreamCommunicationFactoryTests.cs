using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Communication;

/// <summary>
/// DownstreamCommunicationFactory 工厂类单元测试
/// DownstreamCommunicationFactory unit tests
/// </summary>
/// <remarks>
/// 测试覆盖 / Test Coverage:
/// 1. ✅ config == null 返回 NullDownstreamCommunication
/// 2. ✅ config.IsEnabled == false 返回 NullDownstreamCommunication
/// 3. ✅ ConnectionMode == "SERVER" 创建 DownstreamTcpJsonServer
/// 4. ✅ ConnectionMode == "CLIENT" 创建 TouchSocketTcpDownstreamClient
/// 5. ✅ 大小写不敏感测试
/// </remarks>
public class DownstreamCommunicationFactoryTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ISystemClock> _mockSystemClock;
    private readonly DownstreamCommunicationFactory _factory;

    public DownstreamCommunicationFactoryTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockSystemClock = new Mock<ISystemClock>();

        // Setup logger factory to return mock loggers
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _factory = new DownstreamCommunicationFactory(_mockLoggerFactory.Object, _mockSystemClock.Object);
    }

    /// <summary>
    /// 测试配置为 null 时返回 NullDownstreamCommunication
    /// Test returns NullDownstreamCommunication when config is null
    /// </summary>
    [Fact]
    public void Create_WhenConfigIsNull_ReturnsNullDownstreamCommunication()
    {
        // Act
        var result = _factory.Create(null);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NullDownstreamCommunication>(result);
        Assert.False(result.IsEnabled);
    }

    /// <summary>
    /// 测试配置 IsEnabled = false 时返回 NullDownstreamCommunication
    /// Test returns NullDownstreamCommunication when config IsEnabled is false
    /// </summary>
    [Fact]
    public void Create_WhenConfigIsDisabled_ReturnsNullDownstreamCommunication()
    {
        // Arrange
        var config = new SorterConfig
        {
            Protocol = "TCP",
            Host = "127.0.0.1",
            Port = 2003,
            IsEnabled = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var result = _factory.Create(config);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NullDownstreamCommunication>(result);
        Assert.False(result.IsEnabled);
    }

    /// <summary>
    /// 测试 ConnectionMode = "SERVER" 时创建 DownstreamTcpJsonServer
    /// Test creates DownstreamTcpJsonServer when ConnectionMode is SERVER
    /// </summary>
    [Fact]
    public void Create_WhenConnectionModeIsServer_ReturnsDownstreamTcpJsonServer()
    {
        // Arrange
        var config = new SorterConfig
        {
            Protocol = "TCP",
            ConnectionMode = "SERVER",
            Host = "127.0.0.1",
            Port = 2003,
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var result = _factory.Create(config);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DownstreamTcpJsonServer>(result);
        Assert.True(result.IsEnabled);
    }

    /// <summary>
    /// 测试 ConnectionMode = "CLIENT" 时创建 TouchSocketTcpDownstreamClient
    /// Test creates TouchSocketTcpDownstreamClient when ConnectionMode is CLIENT
    /// </summary>
    [Fact]
    public void Create_WhenConnectionModeIsClient_ReturnsTouchSocketTcpDownstreamClient()
    {
        // Arrange
        var config = new SorterConfig
        {
            Protocol = "TCP",
            ConnectionMode = "CLIENT",
            Host = "127.0.0.1",
            Port = 2003,
            IsEnabled = true,
            TimeoutSeconds = 30,
            ReconnectIntervalSeconds = 5,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var result = _factory.Create(config);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ZakYip.Sorting.RuleEngine.Infrastructure.Communication.Clients.TouchSocketTcpDownstreamClient>(result);
        Assert.True(result.IsEnabled);
    }

    /// <summary>
    /// 测试大小写不敏感 - "server"、"Server"、"SERVER" 都应该工作
    /// Test case insensitivity - "server", "Server", "SERVER" should all work
    /// </summary>
    [Theory]
    [InlineData("server")]
    [InlineData("Server")]
    [InlineData("SERVER")]
    public void Create_ServerMode_IsCaseInsensitive(string connectionMode)
    {
        // Arrange
        var config = new SorterConfig
        {
            Protocol = "TCP",
            ConnectionMode = connectionMode,
            Host = "127.0.0.1",
            Port = 2003,
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var result = _factory.Create(config);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DownstreamTcpJsonServer>(result);
    }

    /// <summary>
    /// 测试大小写不敏感 - "client"、"Client"、"CLIENT" 都应该工作
    /// Test case insensitivity - "client", "Client", "CLIENT" should all work
    /// </summary>
    [Theory]
    [InlineData("client")]
    [InlineData("Client")]
    [InlineData("CLIENT")]
    public void Create_ClientMode_IsCaseInsensitive(string connectionMode)
    {
        // Arrange
        var config = new SorterConfig
        {
            Protocol = "TCP",
            ConnectionMode = connectionMode,
            Host = "127.0.0.1",
            Port = 2003,
            IsEnabled = true,
            TimeoutSeconds = 30,
            ReconnectIntervalSeconds = 5,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var result = _factory.Create(config);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ZakYip.Sorting.RuleEngine.Infrastructure.Communication.Clients.TouchSocketTcpDownstreamClient>(result);
    }
}
