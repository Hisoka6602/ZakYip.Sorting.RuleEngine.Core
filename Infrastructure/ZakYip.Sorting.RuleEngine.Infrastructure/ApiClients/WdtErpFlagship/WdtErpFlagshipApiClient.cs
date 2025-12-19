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

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;

/// <summary>
/// 旺店通ERP旗舰版 API客户端实现
/// WDT ERP Flagship API client implementation
/// 直接实现IWcsApiAdapter接口，无基类继承
/// Directly implements IWcsApiAdapter interface, no base class
/// 配置从LiteDB加载，支持热更新
/// Configuration loaded from LiteDB with hot reload support
/// </summary>
public class WdtErpFlagshipApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WdtErpFlagshipApiClient> _logger;
    private readonly ISystemClock _clock;
    private readonly IWdtErpFlagshipConfigRepository _configRepository;
    
    // 缓存配置以避免每次请求都查询数据库
    private WdtErpFlagshipConfig? _cachedConfig;
    private DateTime _configCacheTime = DateTime.MinValue;
    private readonly TimeSpan _configCacheExpiry = TimeSpan.FromMinutes(5);

    public WdtErpFlagshipApiClient(
        HttpClient httpClient,
        ILogger<WdtErpFlagshipApiClient> logger,
        ISystemClock clock,
        IWdtErpFlagshipConfigRepository configRepository)
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
    private async Task<WdtErpFlagshipConfig> GetConfigAsync()
    {
        // 检查缓存是否有效
        if (_cachedConfig != null && _clock.LocalNow - _configCacheTime < _configCacheExpiry)
        {
            return _cachedConfig;
        }

        // 从数据库加载配置
        var config = await _configRepository.GetByIdAsync(WdtErpFlagshipConfig.SingletonId).ConfigureAwait(false);
        
        if (config == null)
        {
            // 如果配置不存在，创建默认配置
            _logger.LogWarning("旺店通ERP旗舰版配置不存在，使用默认配置");
            config = new WdtErpFlagshipConfig
            {
                ConfigId = WdtErpFlagshipConfig.SingletonId,
                Url = string.Empty,
                Key = string.Empty,
                Appsecret = string.Empty,
                Sid = string.Empty,
                Method = "wms.stockout.Sales.weighingExt",
                V = string.Empty,
                Salt = string.Empty,
                PackagerId = 0,
                PackagerNo = string.Empty,
                OperateTableName = string.Empty,
                Force = false,
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

    /// <summary>
    /// 扫描包裹 - 旺店通ERP旗舰版不支持此功能
    /// Scan parcel - Not supported by WDT ERP Flagship
    /// </summary>
    public Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通ERP旗舰版不支持扫描包裹功能，条码: {Barcode}", barcode);
        
        const string notApplicableUrl = "NOT_SUPPORTED://scan-parcel";
        var notSupportedMessage = "旺店通ERP旗舰版不支持扫描包裹功能 / WDT ERP Flagship does not support parcel scanning";
        
        // 生成示例curl命令，展示如果支持该操作时的请求格式
        // Generate example curl command showing what the request would look like if supported
        var exampleBody = System.Text.Json.JsonSerializer.Serialize(new { barcode, operation = "scan", timestamp = _clock.LocalNow });
        var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
            "POST",
            notApplicableUrl,
            new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            exampleBody);
        curlCommand = $"# Feature not supported - Example request format:\n{curlCommand}";
        
        return Task.FromResult(new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = notSupportedMessage,
            ResponseBody = "{\"info\":\"Feature not supported\"}",
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
    /// 请求格口（扩展称重/一次称重）
    /// Request chute (weighing extended/once weighing)
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
        
        try
        {
            _logger.LogDebug("旺店通ERP旗舰版 - 开始请求格口，包裹ID: {ParcelId}, 条码: {Barcode}",
                parcelId, dwsData.Barcode);

            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

            // 根据Method构造不同的业务参数
            object bizData;
            if (config.Method == "wms.stockout.Sales.weighingExt")
            {
                // 扩展称重
                bizData = new
                {
                    outer_no = dwsData.Barcode,
                    weight = (double)dwsData.Weight,
                    length = (double)dwsData.Length,
                    width = (double)dwsData.Width,
                    height = (double)dwsData.Height,
                    volume = (double)dwsData.Volume,
                    packager_id = config.PackagerId
                };
            }
            else if (config.Method == "wms.stockout.Sales.onceWeighing")
            {
                // 一次称重(按打包员ID)
                bizData = new
                {
                    outer_no = dwsData.Barcode,
                    weight = (double)dwsData.Weight,
                    packager_id = config.PackagerId,
                    operate_table_name = config.OperateTableName,
                    force = config.Force
                };
            }
            else if (config.Method == "wms.stockout.Sales.onceWeighingByNo")
            {
                // 一次称重(按打包员编号)
                bizData = new
                {
                    outer_no = dwsData.Barcode,
                    weight = (double)dwsData.Weight,
                    packager_no = config.PackagerNo,
                    operate_table_name = config.OperateTableName,
                    force = config.Force
                };
            }
            else
            {
                throw new InvalidOperationException($"不支持的方法: {config.Method}");
            }

            var bizJson = JsonConvert.SerializeObject(bizData);

            var requestData = new Dictionary<string, string>
            {
                { "method", config.Method },
                { "sid", config.Sid },
                { "key", config.Key },
                { "timestamp", timestamp },
                { "v", config.V },
                { "format", "json" },
                { "body", bizJson }
            };

            // 如果有salt，添加到请求中
            if (!string.IsNullOrEmpty(config.Salt))
            {
                requestData.Add("salt", config.Salt);
            }

            var sign = GenerateSign(requestData, config.Appsecret);
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
                // 旺店通ERP旗舰版成功响应格式: { "status": 0, "data": {...} } 或 { "code": 0 }
                isSuccess = jObject["status"]?.Value<int>() == 0 || jObject["code"]?.Value<int>() == 0;
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation("旺店通ERP旗舰版 - 请求格口成功，包裹ID: {ParcelId}, 耗时: {Duration}ms",
                    parcelId, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "请求格口成功 / Request chute succeeded",
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = bizJson,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = formattedCurl,
                    OcrData = ocrData
                };
            }
            else
            {
                _logger.LogWarning("旺店通ERP旗舰版 - 请求格口失败，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                    parcelId, response.StatusCode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = $"请求格口失败 / Request chute failed: {response.StatusCode}",
                    ResponseBody = responseContent,
                    ErrorMessage = $"请求格口失败: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = bizJson,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
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
            _logger.LogError(ex, "旺店通ERP旗舰版 - 请求格口异常，包裹ID: {ParcelId}", parcelId);

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
                RequestUrl = config.Url,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                ResponseStatusCode = response != null ? (int)response.StatusCode : null,
                ResponseHeaders = responseHeaders,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = formattedCurl,
                OcrData = ocrData
            };
        }
    }

    /// <summary>
    /// 上传图片 - 旺店通ERP旗舰版不支持此功能
    /// Upload image - Not supported by WDT ERP Flagship
    /// </summary>
    public Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通ERP旗舰版不支持上传图片功能，条码: {Barcode}", barcode);
        
        const string notApplicableUrl = "NOT_SUPPORTED://upload-image";
        var notSupportedMessage = "旺店通ERP旗舰版不支持上传图片功能 / WDT ERP Flagship does not support image upload";
        
        // 生成示例curl命令，展示如果支持该操作时的请求格式
        // Generate example curl command showing what the request would look like if supported
        var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
            "POST",
            notApplicableUrl,
            new Dictionary<string, string> 
            { 
                ["Content-Type"] = $"multipart/form-data; boundary=----WebKitFormBoundary",
                ["X-Barcode"] = barcode
            },
            $"------WebKitFormBoundary\nContent-Disposition: form-data; name=\"file\"; filename=\"{barcode}.jpg\"\nContent-Type: {contentType}\n\n[Binary image data: {imageData.Length} bytes]\n------WebKitFormBoundary--");
        curlCommand = $"# Feature not supported - Example request format:\n{curlCommand}";
        
        return Task.FromResult(new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = notSupportedMessage,
            ResponseBody = "{\"info\":\"Feature not supported\"}",
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
    /// 落格回调 - 旺店通ERP旗舰版不支持此功能
    /// Chute landing callback - Not supported by WDT ERP Flagship
    /// </summary>
    public Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通ERP旗舰版不支持落格回调功能，包裹ID: {ParcelId}", parcelId);
        
        const string notApplicableUrl = "NOT_SUPPORTED://notify-chute-landing";
        var notSupportedMessage = "旺店通ERP旗舰版不支持落格回调功能 / WDT ERP Flagship does not support chute landing callback";
        
        // 生成示例curl命令，展示如果支持该操作时的请求格式
        // Generate example curl command showing what the request would look like if supported
        var exampleBody = System.Text.Json.JsonSerializer.Serialize(new { parcelId, chuteId, barcode, timestamp = _clock.LocalNow });
        var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
            "POST",
            notApplicableUrl,
            new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            exampleBody);
        curlCommand = $"# Feature not supported - Example request format:\n{curlCommand}";
        
        return Task.FromResult(new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = notSupportedMessage,
            ResponseBody = "{\"info\":\"Feature not supported\"}",
            ParcelId = parcelId,
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
