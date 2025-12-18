using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;

namespace ZakYip.Sorting.RuleEngine.WdtErpFlagshipApiClient.ConsoleTest;

class Program
{
    // Configuration parameters - WDT ERP Flagship API
    private const string BASE_URL = "https://api.example.com/flagship";
    private const string KEY = "your-key";
    private const string APPSECRET = "your-appsecret";
    private const string SID = "your-sid";
    private const string METHOD = "wms.stockout.Sales.weighingExt";
    private const string VERSION = "1.0";
    private const string SALT = "salt123";
    private const int PACKAGER_ID = 1001;
    private const int TIMEOUT_MS = 30000;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WDT ERP Flagship API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Infrastructure.ApiClients.WdtErpFlagship.WdtErpFlagshipApiClient>();
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS) };
        
        var clock = new Infrastructure.Services.SystemClock();
        var mockRepo = new MockWdtErpFlagshipConfigRepository();
        var client = new Infrastructure.ApiClients.WdtErpFlagship.WdtErpFlagshipApiClient(httpClient, logger, clock, mockRepo);
        
        Console.WriteLine($"URL: {BASE_URL}");
        Console.WriteLine($"Method: {METHOD}\n");
        
        // Test scan parcel (not supported)
        var scanResult = await client.ScanParcelAsync("TEST-WDT-ERP-001");
        Console.WriteLine($"Scan: {scanResult.Success} - {scanResult.Message}\n");
        
        // Test request chute with sample DWS data
        var dwsData = new DwsData
        {
            Barcode = "TEST-BARCODE-001",
            Weight = 2.567m,
            Length = 30.5m,
            Width = 20.3m,
            Height = 15.8m,
            Volume = 9.82m
        };
        
        Console.WriteLine("Testing RequestChuteAsync with sample data...");
        var chuteResult = await client.RequestChuteAsync("PARCEL-001", dwsData);
        Console.WriteLine($"Request Chute: {chuteResult.Success} - {chuteResult.Message}");
        Console.WriteLine($"Response: {chuteResult.Data}\n");
        
        // Test upload image (not supported)
        var imageData = new byte[] { 0x01, 0x02, 0x03 };
        var uploadResult = await client.UploadImageAsync("TEST-BARCODE-002", imageData);
        Console.WriteLine($"Upload Image: {uploadResult.Success} - {uploadResult.Message}\n");
        
        Console.WriteLine("Test completed. Press any key...");
        Console.ReadKey();
    }
}

// Mock repository for testing
class MockWdtErpFlagshipConfigRepository : Domain.Interfaces.IWdtErpFlagshipConfigRepository
{
    private readonly WdtErpFlagshipConfig _config = new()
    {
        ConfigId = WdtErpFlagshipConfig.SingletonId,
        Url = "https://api.example.com/flagship",
        Key = "your-key",
        Appsecret = "your-appsecret",
        Sid = "your-sid",
        Method = "wms.stockout.Sales.weighingExt",
        V = "1.0",
        Salt = "salt123",
        PackagerId = 1001,
        PackagerNo = string.Empty,
        OperateTableName = string.Empty,
        Force = false,
        TimeoutMs = 30000,
        IsEnabled = true,
        Description = "Test configuration",
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    public Task<bool> AddAsync(WdtErpFlagshipConfig config) => Task.FromResult(true);
    public Task<bool> DeleteAsync(string configId) => Task.FromResult(true);
    public Task<IEnumerable<WdtErpFlagshipConfig>> GetAllAsync() => 
        Task.FromResult<IEnumerable<WdtErpFlagshipConfig>>(new[] { _config });
    public Task<WdtErpFlagshipConfig?> GetByIdAsync(string configId) => 
        Task.FromResult<WdtErpFlagshipConfig?>(_config);
    public Task<IEnumerable<WdtErpFlagshipConfig>> GetEnabledConfigsAsync() => 
        Task.FromResult<IEnumerable<WdtErpFlagshipConfig>>(new[] { _config });
    public Task<bool> UpdateAsync(WdtErpFlagshipConfig config) => Task.FromResult(true);
    public Task<bool> UpsertAsync(WdtErpFlagshipConfig config) => Task.FromResult(true);
}
