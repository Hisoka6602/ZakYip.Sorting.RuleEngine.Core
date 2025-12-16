using System.Text.Json;
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
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILogger<MockWcsApiAdapter> _logger;
    private readonly IAutoResponseModeService _autoResponseModeService;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public MockWcsApiAdapter(
        ILogger<MockWcsApiAdapter> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock,
        IAutoResponseModeService autoResponseModeService)
    {
        _logger = logger;
        _clock = clock;
        _autoResponseModeService = autoResponseModeService;
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
            Data = JsonSerializer.Serialize(new { chuteNumber }, JsonOptions),
            ParcelId = barcode,
            RequestUrl = "/api/mock/scan",
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
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
            Data = JsonSerializer.Serialize(new { chuteNumber }, JsonOptions),
            ResponseBody = JsonSerializer.Serialize(new 
            { 
                chuteNumber, 
                weight = dwsData.Weight, 
                volume = dwsData.Volume 
            }, JsonOptions),
            ParcelId = parcelId,
            RequestUrl = "/api/mock/chute-request",
            RequestBody = JsonSerializer.Serialize(new 
            { 
                parcelId, 
                barcode = dwsData.Barcode 
            }, JsonOptions),
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
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
            Data = JsonSerializer.Serialize(new { uploaded = true, size = imageData.Length }, JsonOptions),
            ParcelId = barcode,
            RequestUrl = "/api/mock/upload-image",
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 10
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 生成随机格口号（从配置的格口数组中选择）
    /// Generate random chute number (selected from configured chute array)
    /// 使用 Random.Shared 以确保线程安全和更好的性能
    /// Uses Random.Shared for thread safety and better performance
    /// </summary>
    private string GenerateRandomChuteNumber()
    {
        var chuteNumbers = _autoResponseModeService.ChuteNumbers;
        var index = Random.Shared.Next(0, chuteNumbers.Length);
        return chuteNumbers[index].ToString();
    }
}
