using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.WcsApiClient.ConsoleTest;

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
        var client = new Infrastructure.ApiClients.WcsApiClient(httpClient, logger, clock);
        
        Console.WriteLine($"URL: {BASE_URL}\n");
        
        // Test scan parcel
        var scanResult = await client.ScanParcelAsync("TEST-001");
        Console.WriteLine($"Scan: {scanResult.Success} - {scanResult.Message}\n");
        
        Console.WriteLine("Test completed. Press any key...");
        Console.ReadKey();
    }
}
