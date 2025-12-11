using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

/// <summary>
/// ERP API客户端基类 - 提供ERP客户端的共享功能
/// Base ERP API Client - Provides shared functionality for ERP API clients
/// </summary>
/// <remarks>
/// 此抽象基类提取了 WdtErpFlagshipApiClient、WdtWmsApiClient 和 JushuitanErpApiClient 的共享逻辑，
/// 消除代码重复并遵循DRY原则。
/// This abstract base class extracts shared logic from WdtErpFlagshipApiClient, WdtWmsApiClient, 
/// and JushuitanErpApiClient to eliminate code duplication following the DRY principle.
/// </remarks>
public abstract class BaseErpApiClient : IWcsApiAdapter
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;

    /// <summary>
    /// 获取客户端类型名称，用于日志记录
    /// Get client type name for logging purposes
    /// </summary>
    protected abstract string ClientTypeName { get; }

    /// <summary>
    /// 获取功能不支持的说明文字
    /// Get feature not supported description text
    /// </summary>
    protected abstract string FeatureNotSupportedText { get; }

    protected BaseErpApiClient(
        HttpClient httpClient,
        ILogger logger)
    {
        HttpClient = httpClient;
        Logger = logger;
    }

    /// <summary>
    /// 扫描包裹（大部分ERP系统不支持此功能）
    /// Scan parcel - Not supported in most ERP systems
    /// </summary>
    public virtual async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var requestTime = DateTime.Now;
        
        Logger.LogWarning("{ClientType}不支持扫描包裹功能，条码: {Barcode}", ClientTypeName, barcode);
        
        await Task.CompletedTask;
        stopwatch.Stop();
        
        return new WcsApiResponse
        {
            Success = true,
            Code = ApiConstants.HttpStatusCodes.Success,
            Message = $"{ClientTypeName}不支持扫描包裹功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestUrl = "N/A",
            RequestBody = "N/A",
            RequestHeaders = "{}",
            RequestTime = requestTime,
            ResponseTime = DateTime.Now,
            DurationMs = stopwatch.ElapsedMilliseconds,
            FormattedCurl = $"# Feature not supported by {FeatureNotSupportedText}"
        };
    }

    /// <summary>
    /// 上传图片（大部分ERP系统不支持此功能）
    /// Upload image - Not supported in most ERP systems
    /// </summary>
    public virtual async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = "image/jpeg",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var requestTime = DateTime.Now;
        
        Logger.LogWarning("{ClientType}不支持上传图片功能，条码: {Barcode}", ClientTypeName, barcode);
        
        await Task.CompletedTask;
        stopwatch.Stop();
        
        return new WcsApiResponse
        {
            Success = true,
            Code = ApiConstants.HttpStatusCodes.Success,
            Message = $"{ClientTypeName}不支持上传图片功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestUrl = "N/A",
            RequestBody = $"[image upload request: size={imageData.Length} bytes]",
            RequestHeaders = "{}",
            RequestTime = requestTime,
            ResponseTime = DateTime.Now,
            DurationMs = stopwatch.ElapsedMilliseconds,
            FormattedCurl = $"# Feature not supported by {FeatureNotSupportedText}"
        };
    }

    /// <summary>
    /// 请求格口（上传数据）- 子类必须实现
    /// Request a chute/gate number for the parcel - Must be implemented by subclasses
    /// </summary>
    public abstract Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建异常响应的辅助方法
    /// Helper method to create exception response
    /// </summary>
    protected WcsApiResponse CreateExceptionResponse(
        Exception ex,
        string parcelId,
        string requestUrl,
        string? requestHeaders,
        string? responseHeaders,
        HttpResponseMessage? response,
        DateTime requestTime,
        long elapsedMilliseconds,
        string? formattedCurl)
    {
        return new WcsApiResponse
        {
            Success = false,
            Code = ApiConstants.HttpStatusCodes.Error,
            Message = ex.Message,
            Data = ex.ToString(),
            ErrorMessage = ex.Message,
            ParcelId = parcelId,
            RequestUrl = requestUrl,
            RequestHeaders = requestHeaders,
            RequestTime = requestTime,
            ResponseTime = DateTime.Now,
            ResponseStatusCode = response?.StatusCode != null ? (int)response.StatusCode : null,
            ResponseHeaders = responseHeaders,
            DurationMs = elapsedMilliseconds,
            FormattedCurl = formattedCurl
        };
    }

    /// <summary>
    /// 创建超时响应的辅助方法
    /// Helper method to create timeout response
    /// </summary>
    protected WcsApiResponse CreateTimeoutResponse(
        TaskCanceledException ex,
        string parcelId,
        string requestUrl,
        string? requestHeaders,
        string? responseHeaders,
        HttpResponseMessage? response,
        DateTime requestTime,
        long elapsedMilliseconds,
        string? formattedCurl)
    {
        return new WcsApiResponse
        {
            Success = false,
            Code = ApiConstants.HttpStatusCodes.Error,
            Message = "接口访问返回超时",
            Data = ex.ToString(),
            ErrorMessage = "接口访问返回超时",
            ParcelId = parcelId,
            RequestUrl = requestUrl,
            RequestHeaders = requestHeaders,
            RequestTime = requestTime,
            ResponseTime = DateTime.Now,
            ResponseStatusCode = response?.StatusCode != null ? (int)response.StatusCode : null,
            ResponseHeaders = responseHeaders,
            DurationMs = elapsedMilliseconds,
            FormattedCurl = formattedCurl
        };
    }

    /// <summary>
    /// 创建HTTP请求异常响应的辅助方法
    /// Helper method to create HTTP request exception response
    /// </summary>
    protected WcsApiResponse CreateHttpExceptionResponse(
        HttpRequestException ex,
        string parcelId,
        string requestUrl,
        string? requestHeaders,
        string? responseHeaders,
        HttpResponseMessage? response,
        DateTime requestTime,
        long elapsedMilliseconds,
        string? formattedCurl)
    {
        return new WcsApiResponse
        {
            Success = false,
            Code = ApiConstants.HttpStatusCodes.Error,
            Message = "接口访问异常",
            Data = ex.ToString(),
            ErrorMessage = ex.Message,
            ParcelId = parcelId,
            RequestUrl = requestUrl,
            RequestHeaders = requestHeaders,
            RequestTime = requestTime,
            ResponseTime = DateTime.Now,
            ResponseStatusCode = response?.StatusCode != null ? (int)response.StatusCode : null,
            ResponseHeaders = responseHeaders,
            DurationMs = elapsedMilliseconds,
            FormattedCurl = formattedCurl
        };
    }
}
