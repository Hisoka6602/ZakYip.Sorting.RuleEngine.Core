using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostProcessingCenter;

/// <summary>
/// 邮政处理中心API客户端实现
/// Postal Processing Center API client implementation
/// 参考: https://github.com/Hisoka6602/JayTom.Dws 分支[聚水潭(正式)] PostApi.cs
/// 使用SOAP协议进行通信，直接实现IWcsApiAdapter接口
/// Uses SOAP protocol, directly implements IWcsApiAdapter interface
/// 配置从LiteDB加载，支持热更新
/// Configuration loaded from LiteDB with hot reload support
/// </summary>
public class PostProcessingCenterApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostProcessingCenterApiClient> _logger;
    private readonly ISystemClock _clock;
    private readonly IPostProcessingCenterConfigRepository _configRepository;
    
    // 使用线程安全的实例级序列号
    private long _sequenceNumber;
    private readonly object _sequenceLock = new();
    
    // 缓存配置以避免每次请求都查询数据库
    private PostProcessingCenterConfig? _cachedConfig;
    private DateTime _configCacheTime = DateTime.MinValue;
    private readonly TimeSpan _configCacheExpiry = TimeSpan.FromMinutes(5);

    // SOAP namespaces
    private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WebServiceNamespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/";

    public PostProcessingCenterApiClient(
        HttpClient httpClient,
        ILogger<PostProcessingCenterApiClient> logger,
        ISystemClock clock,
        IPostProcessingCenterConfigRepository configRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        _configRepository = configRepository;
        _sequenceNumber = new DateTimeOffset(_clock.UtcNow).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 获取配置，使用缓存以提高性能
    /// Get configuration with caching for performance
    /// </summary>
    private async Task<PostProcessingCenterConfig> GetConfigAsync()
    {
        // 检查缓存是否有效
        if (_cachedConfig != null && _clock.LocalNow - _configCacheTime < _configCacheExpiry)
        {
            return _cachedConfig;
        }

        // 从数据库加载配置
        var config = await _configRepository.GetByIdAsync(PostProcessingCenterConfig.SingletonId).ConfigureAwait(false);
        
        if (config == null)
        {
            // 如果配置不存在，创建默认配置
            _logger.LogWarning("邮政处理中心配置不存在，使用默认配置");
            config = new PostProcessingCenterConfig
            {
                ConfigId = PostProcessingCenterConfig.SingletonId,
                Url = "http://localhost:8080/postal-processing", // 需要配置实际URL
                WorkshopCode = "WS20140010",
                DeviceId = "20140010",
                CompanyName = "广东泽业科技有限公司",
                DeviceBarcode = "141562320001131",
                OrganizationNumber = "20140011",
                EmployeeNumber = "00818684",
                TimeoutMs = 5000,
                IsEnabled = true,
                Description = "默认配置 - 请通过API更新",
                CreatedAt = _clock.LocalNow,
                UpdatedAt = _clock.LocalNow
            };
            
            await _configRepository.AddAsync(config).ConfigureAwait(false);
        }

        // 更新缓存
        _cachedConfig = config;
        _configCacheTime = _clock.LocalNow;

        return config;
    }

    private long GetNextSequenceNumber()
    {
        lock (_sequenceLock)
        {
            return _sequenceNumber++;
        }
    }

    /// <summary>
    /// 扫描包裹到邮政处理中心
    /// Scan parcel to postal processing center (getYJSM)
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
                const string notApplicableUrl = "SKIPPED://noread-barcode";
                var skipMessage = "NoRead barcode skipped";
                var curlCommand = $"# {skipMessage}\n# Barcode: {barcode}\n# No actual HTTP request made for NoRead barcodes";
                
                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = skipMessage,
                    ResponseBody = skipMessage,
                    ParcelId = barcode,
                    RequestUrl = notApplicableUrl,
                    RequestBody = null,
                    RequestHeaders = null,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = 200,
                    ResponseHeaders = null,
                    DurationMs = 0,
                    FormattedCurl = curlCommand,
                    CurlData = curlCommand
                };
            }

            // 加载配置
            var config = await GetConfigAsync().ConfigureAwait(false);
            
            if (!config.IsEnabled)
            {
                _logger.LogWarning("邮政处理中心API已禁用");
                const string notApplicableUrl = "DISABLED://api-disabled";
                var disabledMessage = "邮政处理中心API已禁用 / Postal processing center API disabled";
                var curlCommand = $"# {disabledMessage}\n# Barcode: {barcode}\n# API is disabled in configuration";
                
                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = disabledMessage,
                    ResponseBody = "API disabled",
                    ParcelId = barcode,
                    RequestUrl = notApplicableUrl,
                    RequestBody = null,
                    RequestHeaders = null,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = 200,
                    ResponseHeaders = null,
                    DurationMs = 0,
                    FormattedCurl = curlCommand,
                    CurlData = curlCommand
                };
            }

            _logger.LogDebug("扫描包裹到邮政处理中心，条码: {Barcode}", barcode);

            // 构造SOAP请求 - 使用配置中的值
            var arg0 = new StringBuilder()
                .Append("#HEAD::")
                .Append(config.DeviceId).Append("::")
                .Append(barcode).Append("::")
                .Append(config.EmployeeNumber).Append("::")
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
                _logger.LogInformation("扫描包裹成功（邮政处理中心），条码: {Barcode}", barcode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "Parcel scanned successfully at postal processing center",
                    ResponseBody = responseContent,
                    RequestBody = soapRequest,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
            else
            {
                _logger.LogWarning("扫描包裹失败（邮政处理中心），条码: {Barcode}, 状态码: {StatusCode}", 
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = $"Scan Error: {response.StatusCode}",
                    ResponseBody = responseContent,
                    RequestBody = soapRequest,
                    ErrorMessage = $"Scan Error: {response.StatusCode}",
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描包裹异常（邮政处理中心），条码: {Barcode}", barcode);

            // 获取详细的异常信息，包括所有内部异常
            // Get detailed exception message including all inner exceptions
            var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);

            return new WcsApiResponse
            {
                RequestStatus = ApiRequestStatus.Exception,
                FormattedMessage = detailedMessage,
                ResponseBody = ex.ToString(),
                ErrorMessage = detailedMessage,
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
            _logger.LogDebug("请求格口（邮政处理中心），包裹ID: {ParcelId}, 条码: {Barcode}", 
                parcelId, dwsData.Barcode);

            // 先提交扫描信息
            await ScanParcelAsync(dwsData.Barcode, cancellationToken).ConfigureAwait(false);

            // 加载配置
            var config = await GetConfigAsync().ConfigureAwait(false);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{config.WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造格口查询SOAP请求 - 使用配置中的值
            var arg0 = new StringBuilder()
                .Append("#HEAD::")
                .Append(sequenceId).Append("::")
                .Append(config.DeviceId).Append("::")
                .Append(dwsData.Barcode).Append("::")
                .Append("0:: :: :: ::")
                .Append(_clock.LocalNow.ToString("yyyy-MM-dd HH:mm:ss")).Append("::")
                .Append(config.EmployeeNumber).Append("::")
                .Append(config.OrganizationNumber).Append("::")
                .Append(config.CompanyName).Append("::")
                .Append(config.DeviceBarcode).Append("::")
                .Append("||#END")
                .ToString();

            var soapRequest = BuildSoapEnvelope("getLTGKCX", arg0);
            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.PostAsync(config.Url, content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseContent = Regex.Unescape(responseContent);
            
            stopwatch.Stop();

            // 提取格口信息
            var chute = ExtractChuteFromResponse(responseContent);
            var isSuccess = !string.IsNullOrEmpty(chute);

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation(
                    "请求格口成功（邮政处理中心），包裹ID: {ParcelId}, 条码: {Barcode}, 格口: {Chute}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, chute, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "Chute requested successfully",
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
                    "请求格口失败（邮政处理中心），包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = $"Chute Request Error: {response.StatusCode}",
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
            _logger.LogError(ex, "请求格口异常（邮政处理中心），包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

            // 获取详细的异常信息，包括所有内部异常
            // Get detailed exception message including all inner exceptions
            var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);

            return new WcsApiResponse
            {
                RequestStatus = ApiRequestStatus.Exception,
                FormattedMessage = detailedMessage,
                ResponseBody = ex.ToString(),
                ErrorMessage = detailedMessage,
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
        _logger.LogDebug("上传图片功能（邮政处理中心）当前未实现，条码: {Barcode}", barcode);

        const string notApplicableUrl = "NOT_IMPLEMENTED://upload-image";
        var notImplementedMessage = "邮政处理中心图片上传功能未实现 / Postal processing center image upload feature not implemented";
        var curlCommand = $"# {notImplementedMessage}\n# Barcode: {barcode}\n# This feature is planned but not yet implemented";
        
        return Task.FromResult(new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = notImplementedMessage,
            ResponseBody = "{\"info\":\"Feature not implemented\"}",
            ParcelId = barcode,
            RequestUrl = notApplicableUrl,
            RequestBody = null,
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 0,
            FormattedCurl = curlCommand,
            CurlData = curlCommand
        });
    }

    /// <summary>
    /// 落格回调 - 通知邮政处理中心包裹已落入指定格口
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
            _logger.LogDebug("落格回调（邮政处理中心），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}", 
                parcelId, chuteId, barcode);

            // 加载配置
            var config = await GetConfigAsync().ConfigureAwait(false);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{config.WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造落格回调SOAP请求 - 使用配置中的值
            var arg0 = new StringBuilder()
                .Append("#HEAD::")
                .Append(sequenceId).Append("::")
                .Append(config.DeviceId).Append("::")
                .Append(barcode).Append("::")
                .Append(chuteId).Append("::")
                .Append(_clock.LocalNow.ToString("yyyy-MM-dd HH:mm:ss")).Append("::")
                .Append(config.EmployeeNumber).Append("::")
                .Append(config.OrganizationNumber).Append("::")
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
                    "落格回调成功（邮政处理中心），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}",
                    parcelId, chuteId, barcode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "Chute landing notification sent successfully",
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
            else
            {
                _logger.LogWarning(
                    "落格回调失败（邮政处理中心），包裹ID: {ParcelId}, 格口: {ChuteId}, 状态码: {StatusCode}",
                    parcelId, chuteId, response.StatusCode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = $"Chute landing notification error: {response.StatusCode}",
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    ErrorMessage = $"Chute landing notification error: {response.StatusCode}",
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "落格回调异常（邮政处理中心），包裹ID: {ParcelId}, 格口: {ChuteId}", 
                parcelId, chuteId);

            // 获取详细的异常信息，包括所有内部异常
            // Get detailed exception message including all inner exceptions
            var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);

            return new WcsApiResponse
            {
                RequestStatus = ApiRequestStatus.Exception,
                FormattedMessage = detailedMessage,
                ResponseBody = ex.ToString(),
                ErrorMessage = detailedMessage,
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
