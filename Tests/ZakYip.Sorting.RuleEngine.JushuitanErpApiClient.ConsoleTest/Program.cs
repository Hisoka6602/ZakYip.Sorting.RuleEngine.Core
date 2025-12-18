using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;

namespace ZakYip.Sorting.RuleEngine.JushuitanErpApiClient.ConsoleTest;

class Program
{
    // Configuration parameters - Jushuituan ERP API
    private const string BASE_URL = "https://api.jushuitan.com";
    private const string PARTNER_KEY = "your-partner-key";
    private const string PARTNER_SECRET = "your-partner-secret";
    private const string TOKEN = "your-token";
    private const int TIMEOUT_MS = 30000;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Jushuituan ERP API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Infrastructure.ApiClients.JushuitanErp.JushuitanErpApiClient>();
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS) };
        
        var clock = new Infrastructure.Services.SystemClock();
        var mockRepo = new MockJushuitanErpConfigRepository();
        var client = new Infrastructure.ApiClients.JushuitanErp.JushuitanErpApiClient(httpClient, logger, clock, mockRepo);
        
        Console.WriteLine($"URL: {BASE_URL}\n");
        
        // Test scan parcel
        var scanResult = await client.ScanParcelAsync("TEST-JST-001");
        Console.WriteLine($"Scan: {scanResult.Success} - {scanResult.Message}\n");
        
        Console.WriteLine("Test completed. Press any key...");
        Console.ReadKey();
    }
}

// Mock repository for testing
class MockJushuitanErpConfigRepository : Domain.Interfaces.IJushuitanErpConfigRepository
{
    private readonly JushuitanErpConfig _config = new()
    {
        ConfigId = JushuitanErpConfig.SingletonId,
        Url = "https://api.jushuitan.com",
        AppKey = "your-partner-key",
        AppSecret = "your-partner-secret",
        AccessToken = "your-token",
        Version = 2,
        IsUploadWeight = true,
        Type = 1,
        IsUnLid = false,
        Channel = string.Empty,
        DefaultWeight = -1,
        TimeoutMs = 30000,
        IsEnabled = true,
        Description = "Test configuration",
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    public Task<bool> AddAsync(JushuitanErpConfig config) => Task.FromResult(true);
    public Task<bool> DeleteAsync(string configId) => Task.FromResult(true);
    public Task<IEnumerable<JushuitanErpConfig>> GetAllAsync() => 
        Task.FromResult<IEnumerable<JushuitanErpConfig>>(new[] { _config });
    public Task<JushuitanErpConfig?> GetByIdAsync(string configId) => 
        Task.FromResult<JushuitanErpConfig?>(_config);
    public Task<IEnumerable<JushuitanErpConfig>> GetEnabledConfigsAsync() => 
        Task.FromResult<IEnumerable<JushuitanErpConfig>>(new[] { _config });
    public Task<bool> UpdateAsync(JushuitanErpConfig config) => Task.FromResult(true);
    public Task<bool> UpsertAsync(JushuitanErpConfig config) => Task.FromResult(true);
}
