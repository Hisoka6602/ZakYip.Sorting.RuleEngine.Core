using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;

/// <summary>
/// 旺店通WMS API客户端实现
/// WDT WMS API client implementation
/// 直接实现IWcsApiAdapter接口，无基类继承
/// Directly implements IWcsApiAdapter interface, no base class
/// 配置从LiteDB加载，支持热更新
/// Configuration loaded from LiteDB with hot reload support
/// </summary>
public class WdtWmsApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WdtWmsApiClient> _logger;
    private readonly ISystemClock _clock;
    private readonly IWdtWmsConfigRepository _configRepository;
    
    // 缓存配置以避免每次请求都查询数据库
    private WdtWmsConfig? _cachedConfig;
    private DateTime _configCacheTime = DateTime.MinValue;
    private readonly TimeSpan _configCacheExpiry = TimeSpan.FromMinutes(5);

    public WdtWmsApiClient(
        HttpClient httpClient,
        ILogger<WdtWmsApiClient> logger,
        ISystemClock clock,
        IWdtWmsConfigRepository configRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        _configRepository = configRepository;
    }

    /// <summary>
    /// 获取配置，使用缓存以提高性能
    /// Get configuration with caching for performance
    /// </summary>
    private async Task<WdtWmsConfig> GetConfigAsync()
    {
        // 检查缓存是否有效
        if (_cachedConfig != null && _clock.LocalNow - _configCacheTime < _configCacheExpiry)
        {
            return _cachedConfig;
        }

        // 从数据库加载配置
        var config = await _configRepository.GetByIdAsync(WdtWmsConfig.SingletonId).ConfigureAwait(false);
        
        if (config == null)
        {
            // 如果配置不存在，创建默认配置
            _logger.LogWarning("旺店通WMS配置不存在，使用默认配置");
            config = new WdtWmsConfig
            {
                ConfigId = WdtWmsConfig.SingletonId,
                Url = string.Empty,
                Sid = string.Empty,
                AppKey = string.Empty,
                AppSecret = string.Empty,
                Method = "wms.logistics.Consign.weigh",
                TimeoutMs = 5000,
                MustIncludeBoxBarcode = false,
                DefaultWeight = 0.0,
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

    /// <summary>
    /// 扫描包裹 - 旺店通WMS不支持此功能
    /// Scan parcel - Not supported by WDT WMS
    /// </summary>
    public Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通WMS不支持扫描包裹功能，条码: {Barcode}", barcode);
        
        return Task.FromResult(new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = "旺店通WMS不支持扫描包裹功能 / WDT WMS does not support parcel scanning",
            ParcelId = barcode,
            RequestUrl = string.Empty,
            RequestBody = null,
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseBody = "{\"info\":\"Feature not supported\"}",
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 0,
            FormattedCurl = null
        });
    }

    /// <summary>
    /// 请求格口（上传称重数据）
    /// Request chute (upload weighing data)
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;

        // 加载配置（在try外面以便catch中可以使用）
        var config = await GetConfigAsync().ConfigureAwait(false);

        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        string? bizJson = null;
        
        try
        {
            _logger.LogDebug("旺店通WMS - 开始请求格口，包裹ID: {ParcelId}, 条码: {Barcode}",
                parcelId, dwsData.Barcode);

            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

            // 构造业务参数
            var bizData = new
            {
                outer_no = dwsData.Barcode,
                weight = (double)dwsData.Weight,
                length = (double)dwsData.Width,
                width = (double)dwsData.Width,
                height = (double)dwsData.Height,
                volume = (double)dwsData.Volume
            };

            bizJson = JsonConvert.SerializeObject(bizData);

            var requestData = new Dictionary<string, string>
            {
                { "method", config.Method },
                { "sid", config.Sid },
                { "appkey", config.AppKey },
                { "timestamp", timestamp },
                { "v", "1.0" },
                { "format", "json" },
                { "body", bizJson }
            };

            var sign = GenerateSign(requestData, config.AppSecret);
            requestData.Add("sign", sign);

            _httpClient.Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs);
            var content = new FormUrlEncodedContent(requestData);

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            };
            formattedCurl = ApiRequestHelper.GenerateFormattedCurl("POST", config.Url, headers, bizJson);
            requestHeaders = ApiRequestHelper.FormatHeaders(headers);

            response = await _httpClient.PostAsync(config.Url, content, cancellationToken).ConfigureAwait(false);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            bool isSuccess = false;
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var jObject = JObject.Parse(responseContent);
                // 旺店通WMS成功响应格式: { "status": 0, "data": {...} } 或 { "code": 0 }
                isSuccess = jObject["status"]?.Value<int>() == 0 || jObject["code"]?.Value<int>() == 0;
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation("旺店通WMS - 请求格口成功，包裹ID: {ParcelId}, 耗时: {Duration}ms",
                    parcelId, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "请求格口成功 / Request chute succeeded",
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = bizJson,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseBody = responseContent,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = formattedCurl,
                    OcrData = ocrData
                };
            }
            else
            {
                _logger.LogWarning("旺店通WMS - 请求格口失败，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                    parcelId, response.StatusCode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    ErrorMessage = $"请求格口失败: {response.StatusCode}",
                    FormattedMessage = $"请求格口失败 / Request chute failed: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = bizJson,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseBody = responseContent,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = formattedCurl,
                    OcrData = ocrData
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "旺店通WMS - 请求格口异常，包裹ID: {ParcelId}", parcelId);

            // 获取详细的异常信息，包括所有内部异常
            // Get detailed exception message including all inner exceptions
            var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);

            return new WcsApiResponse
            {
                RequestStatus = ApiRequestStatus.Exception,
                ErrorMessage = detailedMessage,
                FormattedMessage = $"请求格口异常 / Request chute exception: {detailedMessage}",
                ParcelId = parcelId,
                RequestUrl = config.Url,
                RequestBody = bizJson,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                ResponseBody = ex.ToString(),
                ResponseStatusCode = response != null ? (int)response.StatusCode : null,
                ResponseHeaders = responseHeaders,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = formattedCurl,
                OcrData = ocrData
            };
        }
    }

    /// <summary>
    /// 上传图片 - 旺店通WMS不支持此功能
    /// Upload image - Not supported by WDT WMS
    /// </summary>
    public Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通WMS不支持上传图片功能，条码: {Barcode}", barcode);
        
        return Task.FromResult(new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = "旺店通WMS不支持上传图片功能 / WDT WMS does not support image upload",
            ParcelId = barcode,
            RequestUrl = string.Empty,
            RequestBody = null,
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseBody = "{\"info\":\"Feature not supported\"}",
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 0,
            FormattedCurl = null
        });
    }

    /// <summary>
    /// 落格回调 - 旺店通WMS不支持此功能
    /// Chute landing callback - Not supported by WDT WMS
    /// </summary>
    public Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通WMS不支持落格回调功能，包裹ID: {ParcelId}", parcelId);
        
        return Task.FromResult(new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = "旺店通WMS不支持落格回调功能 / WDT WMS does not support chute landing callback",
            ParcelId = parcelId,
            RequestUrl = string.Empty,
            RequestBody = null,
            RequestHeaders = null,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            ResponseBody = "{\"info\":\"Feature not supported\"}",
            ResponseStatusCode = 200,
            ResponseHeaders = null,
            DurationMs = 0,
            FormattedCurl = null
        });
    }

    /// <summary>
    /// 生成签名
    /// Generate signature for API authentication
    /// </summary>
    private static string GenerateSign(Dictionary<string, string> parameters, string appSecret)
    {
        if (string.IsNullOrEmpty(appSecret))
        {
            return string.Empty;
        }

        var sortedKeys = parameters.Keys
            .Where(k => k != "sign")
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        var paramStr = new StringBuilder();
        foreach (var key in sortedKeys)
        {
            paramStr.Append(key).Append(parameters[key]);
        }

        var signStr = appSecret + paramStr + appSecret;

        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("X2")); // X2 = 大写
        }
        return sb.ToString();
    }
}
