using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

namespace ZakYip.Sorting.RuleEngine.PostalApi.ConsoleTest;

class Program
{
    // 配置参数 / Configuration parameters
    private const string PROCESSING_CENTER_URL = "https://api.post-processing.example.com";
    private const string COLLECTION_INSTITUTION_URL = "https://api.post-collection.example.com";
    private const int TIMEOUT_SECONDS = 30;
    private const string API_KEY = "your-api-key-here";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 邮政API控制台测试 / Postal API Console Test ===\n");
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        // 测试邮政处理中心API / Test Postal Processing Center API
        await TestPostProcessingCenterAsync(loggerFactory);
        
        Console.WriteLine("\n" + new string('-', 80) + "\n");
        
        // 测试邮政分揽投机构API / Test Postal Collection Institution API
        await TestPostCollectionAsync(loggerFactory);
        
        Console.WriteLine("\n测试完成，按任意键退出... / Test completed. Press any key to exit...");
        Console.ReadKey();
    }
    
    static async Task TestPostProcessingCenterAsync(ILoggerFactory loggerFactory)
    {
        Console.WriteLine("### 测试邮政处理中心API / Testing Postal Processing Center API ###\n");
        
        var logger = loggerFactory.CreateLogger<PostProcessingCenterApiAdapter>();
        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(PROCESSING_CENTER_URL), 
            Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) 
        };
        
        if (!string.IsNullOrEmpty(API_KEY))
        {
            httpClient.DefaultRequestHeaders.Add("X-API-Key", API_KEY);
        }
        
        var adapter = new PostProcessingCenterApiAdapter(httpClient, logger);
        
        Console.WriteLine($"URL: {PROCESSING_CENTER_URL}\n");
        
        // 测试1: 扫描包裹 / Test 1: Scan Parcel
        Console.WriteLine("测试1: 扫描包裹 / Test 1: Scan Parcel");
        var scanResult = await adapter.ScanParcelAsync("POST-CENTER-001");
        Console.WriteLine($"结果 / Result: {scanResult.Success} - {scanResult.Message}\n");
        
        // 测试2: 请求格口（查询路由） / Test 2: Request Chute (Query Routing)
        Console.WriteLine("测试2: 请求格口（查询路由） / Test 2: Request Chute (Query Routing)");
        var chuteResult = await adapter.RequestChuteAsync("POST-CENTER-001");
        Console.WriteLine($"结果 / Result: {chuteResult.Success} - {chuteResult.Message}\n");
        
        // 测试3: 上传图片 / Test 3: Upload Image
        Console.WriteLine("测试3: 上传图片 / Test 3: Upload Image");
        var testImage = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var imageResult = await adapter.UploadImageAsync("POST-CENTER-001", testImage, "image/jpeg");
        Console.WriteLine($"结果 / Result: {imageResult.Success} - {imageResult.Message}\n");
    }
    
    static async Task TestPostCollectionAsync(ILoggerFactory loggerFactory)
    {
        Console.WriteLine("### 测试邮政分揽投机构API / Testing Postal Collection Institution API ###\n");
        
        var logger = loggerFactory.CreateLogger<PostCollectionApiAdapter>();
        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(COLLECTION_INSTITUTION_URL), 
            Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) 
        };
        
        if (!string.IsNullOrEmpty(API_KEY))
        {
            httpClient.DefaultRequestHeaders.Add("X-API-Key", API_KEY);
        }
        
        var adapter = new PostCollectionApiAdapter(httpClient, logger);
        
        Console.WriteLine($"URL: {COLLECTION_INSTITUTION_URL}\n");
        
        // 测试1: 扫描包裹 / Test 1: Scan Parcel
        Console.WriteLine("测试1: 扫描包裹 / Test 1: Scan Parcel");
        var scanResult = await adapter.ScanParcelAsync("POST-COLLECT-001");
        Console.WriteLine($"结果 / Result: {scanResult.Success} - {scanResult.Message}\n");
        
        // 测试2: 请求格口（查询包裹） / Test 2: Request Chute (Query Parcel)
        Console.WriteLine("测试2: 请求格口（查询包裹） / Test 2: Request Chute (Query Parcel)");
        var chuteResult = await adapter.RequestChuteAsync("POST-COLLECT-001");
        Console.WriteLine($"结果 / Result: {chuteResult.Success} - {chuteResult.Message}\n");
        
        // 测试3: 上传图片 / Test 3: Upload Image
        Console.WriteLine("测试3: 上传图片 / Test 3: Upload Image");
        var testImage = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var imageResult = await adapter.UploadImageAsync("POST-COLLECT-001", testImage, "image/jpeg");
        Console.WriteLine($"结果 / Result: {imageResult.Success} - {imageResult.Message}\n");
    }
}
