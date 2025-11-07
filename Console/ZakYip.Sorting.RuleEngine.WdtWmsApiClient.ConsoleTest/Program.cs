using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.WdtWmsApiClient.ConsoleTest;

class Program
{
    // Configuration parameters - WDT WMS API
    private const string BASE_URL = "https://api.wdt.com";
    private const string APP_KEY = "your-app-key";
    private const string APP_SECRET = "your-app-secret";
    private const int TIMEOUT_SECONDS = 30;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WDT WMS API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Infrastructure.ApiClients.WdtWmsApiAdapter>();
        var httpClient = new HttpClient { BaseAddress = new Uri(BASE_URL), Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };
        
        var client = new Infrastructure.ApiClients.WdtWmsApiAdapter(httpClient, logger, APP_KEY, APP_SECRET);
        
        Console.WriteLine($"URL: {BASE_URL}\n");
        
        // Test scan parcel
        var scanResult = await client.ScanParcelAsync("TEST-WDT-001");
        Console.WriteLine($"Scan: {scanResult.Success} - {scanResult.Message}\n");
        
        Console.WriteLine("Test completed. Press any key...");
        Console.ReadKey();
    }
}
