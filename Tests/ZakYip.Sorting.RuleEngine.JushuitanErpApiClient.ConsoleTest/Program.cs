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
    private const int TIMEOUT_SECONDS = 30;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Jushuituan ERP API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Infrastructure.ApiClients.JushuitanErp.JushuitanErpApiClient>();
        var httpClient = new HttpClient { BaseAddress = new Uri(BASE_URL), Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };
        
        var client = new Infrastructure.ApiClients.JushuitanErp.JushuitanErpApiClient(httpClient, logger, PARTNER_KEY, PARTNER_SECRET, TOKEN);
        
        Console.WriteLine($"URL: {BASE_URL}\n");
        
        // Test scan parcel
        var scanResult = await client.ScanParcelAsync("TEST-JST-001");
        Console.WriteLine($"Scan: {scanResult.Success} - {scanResult.Message}\n");
        
        Console.WriteLine("Test completed. Press any key...");
        Console.ReadKey();
    }
}
