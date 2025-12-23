using ZakYip.Sorting.RuleEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.WcsApiClient.ConsoleTest;

// Simple test config repository implementation for console test
file class TestWcsApiConfigRepository : IWcsApiConfigRepository
{
    private readonly WcsApiConfig _config;

    public TestWcsApiConfigRepository(WcsApiConfig config)
    {
        _config = config;
    }

    public Task<WcsApiConfig?> GetByIdAsync(string configId) => Task.FromResult<WcsApiConfig?>(_config);
    public Task<bool> AddAsync(WcsApiConfig entity) => Task.FromResult(true);
    public Task<bool> UpdateAsync(WcsApiConfig entity) => Task.FromResult(true);
    public Task<bool> DeleteAsync(string id) => Task.FromResult(true);
    public Task<bool> UpsertAsync(WcsApiConfig entity) => Task.FromResult(true);
    public Task<IEnumerable<WcsApiConfig>> GetAllAsync() => Task.FromResult<IEnumerable<WcsApiConfig>>(new[] { _config });
    public Task<IEnumerable<WcsApiConfig>> GetEnabledConfigsAsync() => 
        Task.FromResult<IEnumerable<WcsApiConfig>>(_config.IsEnabled ? new[] { _config } : Array.Empty<WcsApiConfig>());
}

class Program
{
    // Configuration parameters
    private const string BASE_URL = "https://api.example.com";
    private const int TIMEOUT_SECONDS = 30;
    private const string API_KEY = "your-api-key-here";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WCS API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Infrastructure.ApiClients.WcsApiClient>();
        var httpClient = new HttpClient { BaseAddress = new Uri(BASE_URL), Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };
        if (!string.IsNullOrEmpty(API_KEY)) httpClient.DefaultRequestHeaders.Add("X-API-Key", API_KEY);
        
        var clock = new Infrastructure.Services.SystemClock();
        
        // Create test config repository
        var testConfig = new WcsApiConfig
        {
            ConfigId = WcsApiConfig.SingletonId,
            ActiveAdapterType = "WcsApiClient",
            Url = BASE_URL,
            ApiKey = API_KEY,
            TimeoutMs = TIMEOUT_SECONDS * 1000,
            IsEnabled = true,
            Description = "Console test config",
            CreatedAt = clock.LocalNow,
            UpdatedAt = clock.LocalNow
        };
        var configRepo = new TestWcsApiConfigRepository(testConfig);
        
        var client = new Infrastructure.ApiClients.WcsApiClient(httpClient, logger, clock, configRepo);
        
        Console.WriteLine($"URL: {BASE_URL}\n");
        
        // Test scan parcel
        var scanResult = await client.ScanParcelAsync("TEST-001");
        Console.WriteLine($"Scan: {scanResult.RequestStatus == ApiRequestStatus.Success} - {scanResult.FormattedMessage}\n");
        
        Console.WriteLine("Test completed. Press any key...");
        Console.ReadKey();
    }
}
