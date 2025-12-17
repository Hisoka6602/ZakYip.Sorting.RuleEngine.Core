using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;

/// <summary>
/// 邮政分揽投机构API客户端实现
/// Postal Collection/Delivery Institution API client implementation
/// 参考: https://github.com/Hisoka6602/JayTom.Dws 分支[聚水潭(正式)] PostInApi.cs
/// 使用SOAP协议进行通信，直接实现IWcsApiAdapter接口
/// Uses SOAP protocol, directly implements IWcsApiAdapter interface
/// </summary>
public class PostCollectionApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostCollectionApiClient> _logger;
    private readonly ISystemClock _clock;
    
    // 使用线程安全的实例级序列号
    private long _sequenceNumber;
    private readonly object _sequenceLock = new();

    // Configuration parameters - should be injected via options pattern in production
    private const string WorkshopCode = "WS20140010";
    private const string DeviceId = "20140010";
    private const string CompanyName = "广东泽业科技有限公司";
    private const string DeviceBarcode = "141562320001131";
    private const string OrganizationNumber = "20140011";
    private const string EmployeeNumber = "00818684";
    
    // SOAP namespaces
    private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WebServiceNamespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/";

    public PostCollectionApiClient(
        HttpClient httpClient,
        ILogger<PostCollectionApiClient> logger,
        ISystemClock clock)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        _sequenceNumber = new DateTimeOffset(_clock.UtcNow).ToUnixTimeMilliseconds();
    }

    private long GetNextSequenceNumber()
    {
        lock (_sequenceLock)
        {
            return _sequenceNumber++;
        }
    }

    /// <summary>
    /// 扫描包裹到邮政分揽投机构
    /// Scan parcel to postal collection institution (getYJSM)
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var requestTime = _clock.LocalNow;
        
        try
        {
            // Skip NoRead barcodes
            if (barcode.Contains("NoRead", StringComparison.OrdinalIgnoreCase))
            {
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

            _logger.LogDebug("扫描包裹到邮政分揽投机构，条码: {Barcode}", barcode);

            // 构造SOAP请求
            var arg0 = new StringBuilder()
                .Append("#HEAD::")
                .Append(DeviceId).Append("::")
                .Append(barcode).Append("::")
                .Append(EmployeeNumber).Append("::")
                .Append(_clock.LocalNow.ToString("yyyyMMddHHmmss")).Append("::")
                .Append("2::001::0000::0000::0::0::0::0::0::0::0")
                .Append("||#END")
                .ToString();

            var soapRequest = BuildSoapEnvelope("getYJSM", arg0);
            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.PostAsync("", content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseContent = Regex.Unescape(responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("扫描包裹成功（邮政分揽投机构），条码: {Barcode}", barcode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Parcel scanned successfully at postal collection institution",
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
                _logger.LogWarning("扫描包裹失败（邮政分揽投机构），条码: {Barcode}, 状态码: {StatusCode}", 
                    barcode, response.StatusCode);

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
            _logger.LogError(ex, "扫描包裹异常（邮政分揽投机构），条码: {Barcode}", barcode);

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
    /// Request chute number (getLTGKCX)
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;
        
        try
        {
            _logger.LogDebug("请求格口（邮政分揽投机构），包裹ID: {ParcelId}, 条码: {Barcode}", 
                parcelId, dwsData.Barcode);

            // 先提交扫描信息
            await ScanParcelAsync(dwsData.Barcode, cancellationToken).ConfigureAwait(false);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造格口查询SOAP请求
            var arg0 = new StringBuilder()
                .Append("#HEAD::")
                .Append(sequenceId).Append("::")
                .Append(DeviceId).Append("::")
                .Append(dwsData.Barcode).Append("::")
                .Append("0:: :: :: ::")
                .Append(_clock.LocalNow.ToString("yyyy-MM-dd HH:mm:ss")).Append("::")
                .Append(EmployeeNumber).Append("::")
                .Append(OrganizationNumber).Append("::")
                .Append(CompanyName).Append("::")
                .Append(DeviceBarcode).Append("::")
                .Append("||#END")
                .ToString();

            var soapRequest = BuildSoapEnvelope("getLTGKCX", arg0);
            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.PostAsync("", content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseContent = Regex.Unescape(responseContent);
            
            stopwatch.Stop();

            // 提取格口信息
            var chute = ExtractChuteFromResponse(responseContent);
            var isSuccess = !string.IsNullOrEmpty(chute);

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation(
                    "请求格口成功（邮政分揽投机构），包裹ID: {ParcelId}, 条码: {Barcode}, 格口: {Chute}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, chute, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Chute requested successfully",
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
                _logger.LogWarning(
                    "请求格口失败（邮政分揽投机构），包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

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
            _logger.LogError(ex, "请求格口异常（邮政分揽投机构），包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

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
    /// 上传图片（当前未实现）
    /// Upload image (not implemented)
    /// </summary>
    public Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("上传图片功能（邮政分揽投机构）当前未实现，条码: {Barcode}", barcode);

        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = "邮政分揽投机构图片上传功能未实现",
            Data = "{\"info\":\"Feature not implemented\"}",
            ParcelId = barcode,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 0
        });
    }

    /// <summary>
    /// 落格回调 - 通知邮政分揽投机构包裹已落入指定格口
    /// Chute landing callback (notifyChuteLanding)
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
            _logger.LogDebug("落格回调（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}", 
                parcelId, chuteId, barcode);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造落格回调SOAP请求
            var arg0 = new StringBuilder()
                .Append("#HEAD::")
                .Append(sequenceId).Append("::")
                .Append(DeviceId).Append("::")
                .Append(barcode).Append("::")
                .Append(chuteId).Append("::")
                .Append(_clock.LocalNow.ToString("yyyy-MM-dd HH:mm:ss")).Append("::")
                .Append(EmployeeNumber).Append("::")
                .Append(OrganizationNumber).Append("::")
                .Append("1::::") // Status: 1=成功落格
                .Append("||#END")
                .ToString();

            var soapRequest = BuildSoapEnvelope("notifyChuteLanding", arg0);
            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.PostAsync("", content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseContent = Regex.Unescape(responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "落格回调成功（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}",
                    parcelId, chuteId, barcode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Chute landing notification sent successfully",
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
                _logger.LogWarning(
                    "落格回调失败（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}, 状态码: {StatusCode}",
                    parcelId, chuteId, response.StatusCode);

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
            _logger.LogError(ex, "落格回调异常（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}", 
                parcelId, chuteId);

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

    /// <summary>
    /// 从SOAP响应中提取格口信息
    /// Extract chute from SOAP response
    /// Response format: #HEAD::{field0}::{field1}::{...}::{field7(chute)}::{...}::||#END
    /// </summary>
    private static string? ExtractChuteFromResponse(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

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
    /// 构建SOAP信封
    /// Build SOAP envelope
    /// </summary>
    private static string BuildSoapEnvelope(string methodName, string arg0Value)
    {
        return $@"<soapenv:Envelope xmlns:soapenv=""{SoapEnvelopeNamespace}"" xmlns:web=""{WebServiceNamespace}"">
    <soapenv:Header/>
    <soapenv:Body>
        <web:{methodName}>
            <arg0>{arg0Value}</arg0>
        </web:{methodName}>
    </soapenv:Body>
</soapenv:Envelope>";
    }
}
