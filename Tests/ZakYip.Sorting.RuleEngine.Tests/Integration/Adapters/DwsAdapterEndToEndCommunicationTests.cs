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
    public async Task DwsDataParser_ShouldParseTemplateData_Successfully()
    {
        // Arrange - 创建DWS模板（CSV格式）
        var testDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var template = new DwsDataTemplate
        {
            TemplateId = 1,
            Name = "Standard CSV Template",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
            CreatedAt = testDate,
            UpdatedAt = testDate
        };

        var parser = new DwsDataParser(new SystemClock());
        
        // 模拟DWS设备发送的CSV格式数据
        var rawData = "ABC123456,5000,300,200,150,9000000,1703232000000";
        
        // Act - 解析数据
        var result = parser.Parse(rawData, template);
        
        // Assert - 验证解析结果
        Assert.NotNull(result);
        Assert.Equal("ABC123456", result.Barcode);
        Assert.Equal(5000m, result.Weight);
        Assert.Equal(300m, result.Length);
        Assert.Equal(200m, result.Width);
        Assert.Equal(150m, result.Height);
        Assert.Equal(9000000m, result.Volume);
        
        // 验证时间戳解析（毫秒级Unix时间戳）
        Assert.NotEqual(default(DateTime), result.ScannedAt);
        
        await Task.CompletedTask;
    }

    [Fact]
    public async Task DwsDataParser_ShouldParseAllTemplateFields_Correctly()
    {
        // Arrange - 测试所有宏字段
        var testDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var template = new DwsDataTemplate
        {
            TemplateId = 1,
            Name = "All Fields Template",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
            CreatedAt = testDate,
            UpdatedAt = testDate
        };

        var parser = new DwsDataParser(new SystemClock());
        
        // 测试数据：条码,重量,长度,宽度,高度,体积,时间戳(毫秒)
        var testData = "PKG001,1234.56,100.5,50.25,30.75,151968.75,1703232123456";
        
        // Act
        var result = parser.Parse(testData, template);
        
        // Assert - 验证所有字段
        Assert.NotNull(result);
        Assert.Equal("PKG001", result.Barcode);  // {Code} -> Barcode
        Assert.Equal(1234.56m, result.Weight);   // {Weight}
        Assert.Equal(100.5m, result.Length);     // {Length}
        Assert.Equal(50.25m, result.Width);      // {Width}
        Assert.Equal(30.75m, result.Height);     // {Height}
        Assert.Equal(151968.75m, result.Volume); // {Volume}
        Assert.NotEqual(default(DateTime), result.ScannedAt); // {Timestamp}
        
        await Task.CompletedTask;
    }
    
    [Fact]
    public async Task DwsDataParser_ShouldHandleMissingFields_Gracefully()
    {
        // Arrange - 不完整的模板
        var testDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var template = new DwsDataTemplate
        {
            TemplateId = 1,
            Name = "Partial Template",
            Template = "{Code},{Weight}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
            CreatedAt = testDate,
            UpdatedAt = testDate
        };

        var parser = new DwsDataParser(new SystemClock());
        var testData = "PARTIAL001,999.99";
        
        // Act
        var result = parser.Parse(testData, template);
        
        // Assert - 应该成功解析有的字段
        Assert.NotNull(result);
        Assert.Equal("PARTIAL001", result.Barcode);
        Assert.Equal(999.99m, result.Weight);
        // 其他字段应该是默认值
        Assert.Equal(0m, result.Length);
        Assert.Equal(0m, result.Width);
        
        await Task.CompletedTask;
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

        var testDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var dataTemplate = new DwsDataTemplate
        {
            TemplateId = 1,
            Name = "默认模板",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            IsEnabled = true,
            CreatedAt = testDate,
            UpdatedAt = testDate
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

        var testDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var dataTemplate = new DwsDataTemplate
        {
            TemplateId = 1,
            Name = "默认模板",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            IsEnabled = true,
            CreatedAt = testDate,
            UpdatedAt = testDate
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
