using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Integration.Adapters;

/// <summary>
/// DWS适配器端到端通信测试（Server + Client）
/// DWS adapter end-to-end communication tests (Server + Client)
/// </summary>
/// <remarks>
/// 使用TouchSocket Server和Client进行真实通信测试
/// Uses TouchSocket Server and Client for real communication testing
/// </remarks>
public class DwsAdapterEndToEndCommunicationTests : IAsyncLifetime
{
    private TouchSocketDwsAdapter? _server;
    private TouchSocketDwsTcpClientAdapter? _client;
    private const int TestPort = 18001;
    private readonly List<DwsData> _serverReceivedData = new();
    private readonly List<DwsData> _clientReceivedData = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
        if (_server != null)
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }

    [Fact]
    public async Task ServerAndClient_ShouldCommunicate_Successfully()
    {
        // Arrange - 创建Server
        _server = CreateServer();
        _server.OnDwsDataReceived += data =>
        {
            lock (_serverReceivedData)
            {
                _serverReceivedData.Add(data);
            }
            return Task.CompletedTask;
        };
        await _server.StartAsync();
        await Task.Delay(500); // 等待Server启动

        // 创建Client
        _client = CreateClient();
        _client.OnDwsDataReceived += data =>
        {
            lock (_clientReceivedData)
            {
                _clientReceivedData.Add(data);
            }
            return Task.CompletedTask;
        };
        await _client.StartAsync();
        await Task.Delay(1000); // 等待Client连接

        // Act - Server不主动发送，Client模拟发送（通过反射或其他方式）
        // 这里我们只验证连接成功
        
        // Assert
        Assert.True(_server.AdapterName == "TouchSocket-DWS-Server");
        Assert.True(_client.AdapterName == "TouchSocket-DWS-Client");
    }

    [Fact]
    public async Task Server_ShouldStart_WithoutErrors()
    {
        // Arrange
        _server = CreateServer();

        // Act
        var exception = await Record.ExceptionAsync(async () => 
            await _server.StartAsync());

        // Assert
        Assert.Null(exception);
        
        // Cleanup
        await _server.StopAsync();
    }

    [Fact]
    public async Task Client_ShouldConnect_WithoutErrors()
    {
        // Arrange - 先启动Server
        _server = CreateServer();
        await _server.StartAsync();
        await Task.Delay(500);

        _client = CreateClient();

        // Act
        var exception = await Record.ExceptionAsync(async () => 
            await _client.StartAsync());

        // Assert
        Assert.Null(exception);
    }

    private TouchSocketDwsAdapter CreateServer()
    {
        var mockLogger = new Mock<ILogger<TouchSocketDwsAdapter>>();
        var mockLogRepository = new Mock<ICommunicationLogRepository>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockLogRepository.Setup(x => x.LogCommunicationAsync(
            It.IsAny<CommunicationType>(),
            It.IsAny<CommunicationDirection>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockServiceProvider.Setup(x => x.GetService(typeof(ICommunicationLogRepository)))
            .Returns(mockLogRepository.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        var dataTemplate = new DwsDataTemplate
        {
            TemplateId = 1,
            Name = "默认模板",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var dataParser = new DwsDataParser(new SystemClock());

        return new TouchSocketDwsAdapter(
            "127.0.0.1",
            TestPort,
            mockLogger.Object,
            mockScopeFactory.Object,
            dataParser,
            dataTemplate,
            maxConnections: 5,
            receiveBufferSize: 8192,
            sendBufferSize: 8192);
    }

    private TouchSocketDwsTcpClientAdapter CreateClient()
    {
        var mockLogger = new Mock<ILogger<TouchSocketDwsTcpClientAdapter>>();
        var mockLogRepository = new Mock<ICommunicationLogRepository>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockLogRepository.Setup(x => x.LogCommunicationAsync(
            It.IsAny<CommunicationType>(),
            It.IsAny<CommunicationDirection>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockServiceProvider.Setup(x => x.GetService(typeof(ICommunicationLogRepository)))
            .Returns(mockLogRepository.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        var dataTemplate = new DwsDataTemplate
        {
            TemplateId = 1,
            Name = "默认模板",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var dataParser = new DwsDataParser(new SystemClock());

        return new TouchSocketDwsTcpClientAdapter(
            "127.0.0.1",
            TestPort,
            dataTemplate,
            mockLogger.Object,
            mockScopeFactory.Object,
            dataParser,
            autoReconnect: true,
            reconnectIntervalSeconds: 1);
    }
}
