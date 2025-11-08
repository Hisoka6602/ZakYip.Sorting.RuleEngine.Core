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
    private const int TIMEOUT_SECONDS = 30;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WDT ERP Flagship API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Infrastructure.ApiClients.WdtErpFlagship.WdtErpFlagshipApiClient>();
        var httpClient = new HttpClient { BaseAddress = new Uri(BASE_URL), Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };
        
        var client = new Infrastructure.ApiClients.WdtErpFlagship.WdtErpFlagshipApiClient(httpClient, logger, KEY, APPSECRET, SID);
        
        // Configure additional parameters
        client.Parameters.Url = BASE_URL;
        client.Parameters.Method = "wms.stockout.Sales.weighingExt";
        client.Parameters.V = "1.0";
        client.Parameters.Salt = "salt123";
        client.Parameters.PackagerId = 1001;
        client.Parameters.Force = false;
        
        Console.WriteLine($"URL: {BASE_URL}");
        Console.WriteLine($"Method: {client.Parameters.Method}\n");
        
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
