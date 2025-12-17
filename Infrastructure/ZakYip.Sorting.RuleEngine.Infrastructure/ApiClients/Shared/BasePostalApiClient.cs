using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

/// <summary>
/// 邮政API客户端基类 - 提供邮政分揽投机构和处理中心的共享功能
/// Base Postal API Client - Provides shared functionality for postal collection and processing center clients
/// </summary>
/// <remarks>
/// 此抽象基类提取了 PostCollectionApiClient 和 PostProcessingCenterApiClient 的共享逻辑，
/// 消除代码重复并遵循DRY原则。
/// This abstract base class extracts shared logic from PostCollectionApiClient and 
/// PostProcessingCenterApiClient to eliminate code duplication following the DRY principle.
/// </remarks>
public abstract class BasePostalApiClient : IWcsApiAdapter
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly PostalSoapRequestBuilder SoapRequestBuilder;
    protected readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    
    // 使用线程安全的实例级序列号，避免静态字段的并发问题
    // Use thread-safe instance-level sequence number to avoid static field concurrency issues
    private long _sequenceNumber;
    private readonly object _sequenceLock = new();

    // Configuration parameters - should be injected via options pattern in production
    protected const string WorkshopCode = "WS20140010";
    protected const string DeviceId = "20140010";
    protected const string CompanyName = "广东泽业科技有限公司";
    protected const string DeviceBarcode = "141562320001131";
    protected const string OrganizationNumber = "20140011";
    protected const string EmployeeNumber = "00818684";

    /// <summary>
    /// 获取客户端类型名称，用于日志记录
    /// Get client type name for logging purposes
    /// </summary>
    protected abstract string ClientTypeName { get; }

    protected BasePostalApiClient(
        HttpClient httpClient,
        ILogger logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
        HttpClient = httpClient;
        Logger = logger;
        _clock = clock;
        _sequenceNumber = new DateTimeOffset(_clock.UtcNow).ToUnixTimeMilliseconds();
        SoapRequestBuilder = new PostalSoapRequestBuilder();
    }

    protected long GetNextSequenceNumber()
    {
        lock (_sequenceLock)
        {
            return _sequenceNumber++;
        }
    }

    /// <summary>
    /// 扫描包裹到邮政系统
    /// Scan parcel to register it in the postal system
    /// 对应参考代码中的 SubmitScanInfo 方法 (getYJSM)
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var requestTime = _clock.LocalNow;
        
        try
        {
            // Skip processing for "NoRead" barcodes
            if (barcode.Contains("NoRead", StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.LogDebug("跳过NoRead条码扫描: {Barcode}", barcode);
                return new WcsApiResponse
                {
                    Success = true,
                    Code = HttpStatusCodes.Success,
                    Message = "NoRead barcode skipped",
                    Data = "NoRead barcode skipped",
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    DurationMs = 0
                };
            }

            Logger.LogDebug("开始扫描包裹到{ClientType}，条码: {Barcode}", ClientTypeName, barcode);

            // 构造SOAP请求 - getYJSM方法
            var scanParameters = new PostalScanRequestParameters
            {
                DeviceId = DeviceId,
                Barcode = barcode,
                EmployeeNumber = EmployeeNumber,
                ScanTime = _clock.LocalNow
            };

            var soapRequest = SoapRequestBuilder.BuildScanRequest(scanParameters);

            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            // 发送SOAP请求
            var response = await HttpClient.PostAsync("", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseContent = Regex.Unescape(responseContent);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation(
                    "扫描包裹成功（{ClientType}），条码: {Barcode}, 状态码: {StatusCode}",
                    ClientTypeName, barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Parcel scanned successfully at {ClientTypeName}",
                    Data = responseContent,
                    RequestBody = soapRequest,
                    ResponseBody = responseContent,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
            else
            {
                Logger.LogWarning(
                    "扫描包裹失败（{ClientType}），条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    ClientTypeName, barcode, response.StatusCode, responseContent);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Scan Error: {response.StatusCode}",
                    Data = responseContent,
                    RequestBody = soapRequest,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Scan Error: {response.StatusCode}",
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "扫描包裹异常（{ClientType}），条码: {Barcode}", ClientTypeName, barcode);

            return new WcsApiResponse
            {
                Success = false,
                Code = HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow
            };
        }
    }

    /// <summary>
    /// 请求格口号（查询包裹信息并返回格口）
    /// Request a chute/gate number for the parcel (query parcel info and return chute)
    /// 对应参考代码中的 UploadData 方法 (getLTGKCX)
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;
        
        try
        {
            Logger.LogDebug("开始请求格口（{ClientType}），包裹ID: {ParcelId}, 条码: {Barcode}", 
                ClientTypeName, parcelId, dwsData.Barcode);

            // 先提交扫描信息（对应参考代码中UploadData内部调用SubmitScanInfo）
            await ScanParcelAsync(dwsData.Barcode, cancellationToken);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造SOAP请求 - getLTGKCX方法（查询格口）
            var chuteQueryParameters = new PostalChuteQueryRequestParameters
            {
                SequenceId = sequenceId,
                DeviceId = DeviceId,
                Barcode = dwsData.Barcode,
                ScanTime = _clock.LocalNow,
                EmployeeNumber = EmployeeNumber,
                OrganizationNumber = OrganizationNumber,
                CompanyName = CompanyName,
                DeviceBarcode = DeviceBarcode
            };

            var soapRequest = SoapRequestBuilder.BuildChuteQueryRequest(chuteQueryParameters);

            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            // 发送SOAP请求
            var response = await HttpClient.PostAsync("", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseContent = Regex.Unescape(responseContent);
            
            stopwatch.Stop();

            // 解析响应，提取格口信息
            var chute = ExtractChuteFromResponse(responseContent);
            var isSuccess = !string.IsNullOrEmpty(chute);

            if (response.IsSuccessStatusCode && isSuccess)
            {
                Logger.LogInformation(
                    "请求格口成功（{ClientType}），包裹ID: {ParcelId}, 条码: {Barcode}, 格口: {Chute}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    ClientTypeName, parcelId, dwsData.Barcode, chute, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Chute requested successfully from {ClientTypeName}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    OcrData = ocrData
                };
            }
            else
            {
                Logger.LogWarning(
                    "请求格口失败（{ClientType}），包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    ClientTypeName, parcelId, dwsData.Barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Chute Request Error: {response.StatusCode}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Chute Request Error: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    OcrData = ocrData
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "请求格口异常（{ClientType}），包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                ClientTypeName, parcelId, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                OcrData = ocrData
            };
        }
    }

    /// <summary>
    /// 从SOAP响应中提取格口信息
    /// Extract chute information from SOAP response
    /// Response format: #HEAD::{field0}::{field1}::{...}::{field7(chute)}::{...}::||#END
    /// </summary>
    protected virtual string? ExtractChuteFromResponse(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        // Extract data between #HEAD:: and ::||#END
        var pattern = @"#HEAD::(.*?)::\|\|#END";
        var match = Regex.Match(responseContent, pattern);
        if (match.Success)
        {
            var content = match.Groups[1].Value;
            var parts = content.Split(new string[] { "::" }, StringSplitOptions.None);

            // Chute is in field 7 (index 7), take first 4 characters
            if (parts.Length > 7 && parts[7].Length >= 4)
            {
                return parts[7][..4];
            }
        }

        return null;
    }

    /// <summary>
    /// 上传图片到邮政系统
    /// Upload image to postal system
    /// 注意：根据要求，如果不存在或未实现可以留空
    /// </summary>
    public async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("上传图片功能（{ClientType}）当前未实现，条码: {Barcode}", ClientTypeName, barcode);
        
        await Task.CompletedTask;

        return new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = $"{ClientTypeName}图片上传功能未实现",
            Data = "{\"info\":\"Feature not implemented\"}",
            ParcelId = barcode,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 0
        };
    }

    /// <summary>
    /// 落格回调 - 通知邮政系统包裹已经落入指定格口
    /// Chute landing callback - Notify postal system that parcel has landed in the specified chute
    /// 对应参考代码中的落格回调接口
    /// </summary>
    public async Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var requestTime = _clock.LocalNow;
        
        try
        {
            Logger.LogDebug("开始落格回调（{ClientType}），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}", 
                ClientTypeName, parcelId, chuteId, barcode);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造落格回调的SOAP请求
            // 根据参考代码，落格回调需要通知系统包裹已落入格口
            var landingParameters = new PostalChuteLandingRequestParameters
            {
                SequenceId = sequenceId,
                DeviceId = DeviceId,
                Barcode = barcode,
                ChuteId = chuteId,
                LandingTime = _clock.LocalNow,
                EmployeeNumber = EmployeeNumber,
                OrganizationNumber = OrganizationNumber
            };

            var soapRequest = SoapRequestBuilder.BuildChuteLandingRequest(landingParameters);

            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            // 发送SOAP请求
            var response = await HttpClient.PostAsync("", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseContent = Regex.Unescape(responseContent);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation(
                    "落格回调成功（{ClientType}），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}, 状态码: {StatusCode}",
                    ClientTypeName, parcelId, chuteId, barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Chute landing notification sent successfully to {ClientTypeName}",
                    Data = responseContent,
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    ResponseBody = responseContent,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
            else
            {
                Logger.LogWarning(
                    "落格回调失败（{ClientType}），包裹ID: {ParcelId}, 格口: {ChuteId}, 状态码: {StatusCode}, 响应: {Response}",
                    ClientTypeName, parcelId, chuteId, response.StatusCode, responseContent);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Chute landing notification error: {response.StatusCode}",
                    Data = responseContent,
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Chute landing notification error: {response.StatusCode}",
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "落格回调异常（{ClientType}），包裹ID: {ParcelId}, 格口: {ChuteId}", 
                ClientTypeName, parcelId, chuteId);

            return new WcsApiResponse
            {
                Success = false,
                Code = HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow
            };
        }
    }
}
