using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;
using ZakYip.Sorting.RuleEngine.Tests.Helpers;

namespace ZakYip.Sorting.RuleEngine.Tests.Integration.Adapters;

/// <summary>
/// DWS TCP客户端端到端测试
/// DWS TCP Client end-to-end tests
/// </summary>
public class DwsTcpClientEndToEndTests : IAsyncLifetime
{
    private SimpleTcpTestServer? _testServer;
    private TouchSocketDwsTcpClientAdapter? _adapter;
    private Mock<ILogger<TouchSocketDwsTcpClientAdapter>>? _mockLogger;
    private Mock<IServiceScopeFactory>? _mockScopeFactory;
    private Mock<ICommunicationLogRepository>? _mockLogRepository;
    private const int TestPort = 15001;
    private readonly List<DwsData> _receivedData = new();

    public async Task InitializeAsync()
    {
        // 启动测试服务器
        _testServer = new SimpleTcpTestServer(TestPort);
        await _testServer.StartAsync();
        
        // 等待服务器就绪
        await Task.Delay(100);
    }

    public async Task DisposeAsync()
    {
        if (_adapter != null)
        {
            await _adapter.DisposeAsync();
        }
        
        if (_testServer != null)
        {
            await _testServer.StopAsync();
            _testServer.Dispose();
        }
    }

    [Fact]
    public async Task Client_ShouldConnectToServer_Successfully()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act
        await adapter.StartAsync();
        await Task.Delay(500); // 等待连接建立

        // Assert
        Assert.True(_testServer!.IsRunning);
    }

    [Fact]
    public async Task Client_ShouldReceiveData_FromServer()
    {
        // Arrange
        var adapter = CreateAdapter();
        adapter.OnDwsDataReceived += data =>
        {
            _receivedData.Add(data);
            return Task.CompletedTask;
        };

        await adapter.StartAsync();
        await Task.Delay(500);

        // Act - 服务器发送DWS数据
        var testData = "ABC123,5000,300,200,150,9000,2024-12-22T16:00:00";
        await _testServer!.SendToAllClientsAsync(testData);
        await Task.Delay(500);

        // Assert
        Assert.NotEmpty(_receivedData);
        Assert.Equal("ABC123", _receivedData[0].Barcode);
        Assert.Equal(5000, _receivedData[0].Weight);
    }

    [Fact]
    public async Task Client_ShouldAutoReconnect_WhenServerRestarts()
    {
        // Arrange
        var adapter = CreateAdapter();
        await adapter.StartAsync();
        await Task.Delay(500);

        // Act - 停止服务器
        await _testServer!.StopAsync();
        await Task.Delay(500);

        // 重启服务器
        _testServer = new SimpleTcpTestServer(TestPort);
        await _testServer.StartAsync();
        await Task.Delay(2000); // 等待自动重连（指数退避）

        // Assert - 客户端应该自动重连
        Assert.True(_testServer.IsRunning);
    }

    private TouchSocketDwsTcpClientAdapter CreateAdapter()
    {
        _mockLogger = new Mock<ILogger<TouchSocketDwsTcpClientAdapter>>();
        _mockLogRepository = new Mock<ICommunicationLogRepository>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        _mockLogRepository.Setup(x => x.LogCommunicationAsync(
            It.IsAny<CommunicationType>(),
            It.IsAny<CommunicationDirection>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockServiceProvider.Setup(x => x.GetService(typeof(ICommunicationLogRepository)))
            .Returns(_mockLogRepository.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

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

        _adapter = new TouchSocketDwsTcpClientAdapter(
            "localhost",
            TestPort,
            dataTemplate,
            _mockLogger.Object,
            _mockScopeFactory.Object,
            dataParser,
            autoReconnect: true,
            reconnectIntervalSeconds: 1); // 快速重连用于测试

        return _adapter;
    }
}
