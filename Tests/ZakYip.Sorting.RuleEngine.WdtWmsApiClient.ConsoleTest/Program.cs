using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;

namespace ZakYip.Sorting.RuleEngine.WdtWmsApiClient.ConsoleTest;

class Program
{
    // Configuration parameters - WDT WMS API
    private const string BASE_URL = "https://api.wdt.com";
    private const string APP_KEY = "your-app-key";
    private const string APP_SECRET = "your-app-secret";
    private const string SID = "your-sid";
    private const int TIMEOUT_MS = 30000;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WDT WMS API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Infrastructure.ApiClients.WdtWms.WdtWmsApiClient>();
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS) };
        
        var clock = new Infrastructure.Services.SystemClock();
        var mockRepo = new MockWdtWmsConfigRepository();
        var client = new Infrastructure.ApiClients.WdtWms.WdtWmsApiClient(httpClient, logger, clock, mockRepo);
        
        Console.WriteLine($"URL: {BASE_URL}\n");
        
        // Test scan parcel
        var scanResult = await client.ScanParcelAsync("TEST-WDT-001");
        Console.WriteLine($"Scan: {scanResult.Success} - {scanResult.Message}\n");
        
        Console.WriteLine("Test completed. Press any key...");
        Console.ReadKey();
    }
}

// Mock repository for testing
class MockWdtWmsConfigRepository : Domain.Interfaces.IWdtWmsConfigRepository
{
    private readonly WdtWmsConfig _config = new()
    {
        ConfigId = WdtWmsConfig.SingletonId,
        Url = "https://api.wdt.com",
        Sid = "your-sid",
        AppKey = "your-app-key",
        AppSecret = "your-app-secret",
        Method = "wms.logistics.Consign.weigh",
        TimeoutMs = 30000,
        MustIncludeBoxBarcode = false,
        DefaultWeight = 0.0,
        IsEnabled = true,
        Description = "Test configuration",
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    public Task<bool> AddAsync(WdtWmsConfig config) => Task.FromResult(true);
    public Task<bool> DeleteAsync(string configId) => Task.FromResult(true);
    public Task<IEnumerable<WdtWmsConfig>> GetAllAsync() => 
        Task.FromResult<IEnumerable<WdtWmsConfig>>(new[] { _config });
    public Task<WdtWmsConfig?> GetByIdAsync(string configId) => 
        Task.FromResult<WdtWmsConfig?>(_config);
    public Task<IEnumerable<WdtWmsConfig>> GetEnabledConfigsAsync() => 
        Task.FromResult<IEnumerable<WdtWmsConfig>>(new[] { _config });
    public Task<bool> UpdateAsync(WdtWmsConfig config) => Task.FromResult(true);
    public Task<bool> UpsertAsync(WdtWmsConfig config) => Task.FromResult(true);
}
