using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 模拟WCS API适配器，用于自动应答模式
/// Mock WCS API adapter for auto-response mode
/// 返回随机格口ID (1-20)，用于模拟与下游的通信
/// Returns random chute ID (1-20) to simulate communication with downstream systems
/// </summary>
public class MockWcsApiAdapter : IWcsApiAdapter
{
    private readonly ILogger<MockWcsApiAdapter> _logger;

    public MockWcsApiAdapter(ILogger<MockWcsApiAdapter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 扫描包裹（模拟实现）
    /// Scan parcel (mock implementation)
    /// </summary>
    public Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("模拟扫描包裹: {Barcode}", barcode);

        var chuteNumber = GenerateRandomChuteNumber();
        var response = new WcsApiResponse
        {
            Success = true,
            Code = "200",
            Message = "模拟扫描成功 / Mock scan successful",
            Data = $"{{\"chuteNumber\":\"{chuteNumber}\"}}",
            ParcelId = barcode,
            RequestUrl = "/api/mock/scan",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 10
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 请求格口（模拟实现，返回随机格口号1-20）
    /// Request chute (mock implementation, returns random chute number 1-20)
    /// </summary>
    public Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var chuteNumber = GenerateRandomChuteNumber();

        _logger.LogInformation(
            "自动应答模式: 包裹 {ParcelId} 分配模拟格口号 {ChuteNumber}",
            parcelId, chuteNumber);

        var response = new WcsApiResponse
        {
            Success = true,
            Code = "200",
            Message = $"自动应答模式: 已分配模拟格口 {chuteNumber}",
            Data = $"{{\"chuteNumber\":\"{chuteNumber}\"}}",
            ResponseBody = $"{{\"chuteNumber\":\"{chuteNumber}\",\"weight\":{dwsData.Weight},\"volume\":{dwsData.Volume}}}",
            ParcelId = parcelId,
            RequestUrl = "/api/mock/chute-request",
            RequestBody = $"{{\"parcelId\":\"{parcelId}\",\"barcode\":\"{dwsData.Barcode}\"}}",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            ResponseStatusCode = 200,
            DurationMs = 10
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 上传图片（模拟实现）
    /// Upload image (mock implementation)
    /// </summary>
    public Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = "image/jpeg",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "模拟上传图片: {Barcode}, 大小: {Size} bytes",
            barcode, imageData.Length);

        var response = new WcsApiResponse
        {
            Success = true,
            Code = "200",
            Message = "模拟图片上传成功 / Mock image upload successful",
            Data = $"{{\"uploaded\":true,\"size\":{imageData.Length}}}",
            ParcelId = barcode,
            RequestUrl = "/api/mock/upload-image",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 10
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 生成随机格口号（1-20）
    /// Generate random chute number (1-20)
    /// 使用 Random.Shared 以确保线程安全和更好的性能
    /// Uses Random.Shared for thread safety and better performance
    /// </summary>
    private static string GenerateRandomChuteNumber()
    {
        var number = Random.Shared.Next(1, 21); // 1-20
        return number.ToString();
    }
}
