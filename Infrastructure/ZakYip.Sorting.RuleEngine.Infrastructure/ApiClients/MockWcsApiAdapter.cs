using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
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
        var responseData = JsonSerializer.Serialize(new { chuteNumber }, JsonOptions);
        
        var response = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = "模拟扫描成功 / Mock scan successful",
            ParcelId = barcode,
            RequestUrl = "/api/mock/scan",
            RequestBody = null,
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseBody = responseData,
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 10,
            FormattedCurl = null
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

        var requestBody = JsonSerializer.Serialize(new 
        { 
            parcelId, 
            barcode = dwsData.Barcode 
        }, JsonOptions);
        
        var responseBody = JsonSerializer.Serialize(new 
        { 
            chuteNumber, 
            weight = dwsData.Weight, 
            volume = dwsData.Volume 
        }, JsonOptions);

        var response = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = $"自动应答模式: 已分配模拟格口 {chuteNumber} / Auto-response mode: Assigned mock chute {chuteNumber}",
            ParcelId = parcelId,
            RequestUrl = "/api/mock/chute-request",
            RequestBody = requestBody,
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseBody = responseBody,
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 10,
            FormattedCurl = null,
            OcrData = ocrData
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

        var responseBody = JsonSerializer.Serialize(new { uploaded = true, size = imageData.Length }, JsonOptions);

        var response = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = "模拟图片上传成功 / Mock image upload successful",
            ParcelId = barcode,
            RequestUrl = "/api/mock/upload-image",
            RequestBody = $"[Binary image data: {imageData.Length} bytes]",
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseBody = responseBody,
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 10,
            FormattedCurl = null
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 落格回调（模拟实现）
    /// Chute landing notification (mock implementation)
    /// </summary>
    public Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "模拟落格回调: 包裹ID={ParcelId}, 格口ID={ChuteId}, 条码={Barcode}",
            parcelId, chuteId, barcode);

        var requestBody = JsonSerializer.Serialize(new { parcelId, chuteId, barcode }, JsonOptions);
        var responseBody = JsonSerializer.Serialize(new { parcelId, chuteId, barcode, landed = true }, JsonOptions);

        var response = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = "模拟落格回调成功 / Mock chute landing notification successful",
            ParcelId = parcelId,
            RequestUrl = "/api/mock/chute-landing",
            RequestBody = requestBody,
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseBody = responseBody,
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 10,
            FormattedCurl = null
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
