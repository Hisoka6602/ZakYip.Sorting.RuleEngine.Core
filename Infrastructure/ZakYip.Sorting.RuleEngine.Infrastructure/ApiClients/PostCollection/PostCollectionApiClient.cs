using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;

/// <summary>
/// 邮政分揽投机构API客户端实现
/// Postal Collection/Delivery Institution API client implementation
/// 参考: https://github.com/Hisoka6602/JayTom.Dws 分支[聚水潭(正式)] PostInApi.cs
/// 方法映射 / Method Mapping:
/// - NotifyChuteLandingAsync → PostInApi.UploadInBackground
/// 使用SOAP协议进行通信，直接实现IWcsApiAdapter接口
/// Uses SOAP protocol, directly implements IWcsApiAdapter interface
/// 配置从LiteDB加载，支持热更新
/// Configuration loaded from LiteDB with hot reload support
/// </summary>
public class PostCollectionApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostCollectionApiClient> _logger;
    private readonly ISystemClock _clock;
    private readonly IPostCollectionConfigRepository _configRepository;

    // 使用线程安全的实例级序列号
    private long _sequenceNumber;

    private readonly object _sequenceLock = new();

    // 缓存配置以避免每次请求都查询数据库
    private PostCollectionConfig? _cachedConfig;

    private DateTime _configCacheTime = DateTime.MinValue;
    private readonly TimeSpan _configCacheExpiry = TimeSpan.FromMinutes(5);

    // SOAP namespaces
    private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";

    private const string WebServiceNamespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/";

    public PostCollectionApiClient(
        HttpClient httpClient,
        ILogger<PostCollectionApiClient> logger,
        ISystemClock clock,
        IPostCollectionConfigRepository configRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        _configRepository = configRepository;
        _sequenceNumber = 1;
    }

    /// <summary>
    /// 获取配置，使用缓存以提高性能
    /// Get configuration with caching for performance
    /// </summary>
    private async Task<PostCollectionConfig> GetConfigAsync()
    {
        // 检查缓存是否有效
        if (_cachedConfig != null && _clock.LocalNow - _configCacheTime < _configCacheExpiry)
        {
            return _cachedConfig;
        }

        // 从数据库加载配置
        var config = await _configRepository.GetByIdAsync(PostCollectionConfig.SingletonId).ConfigureAwait(false);

        if (config == null)
        {
            // 如果配置不存在，创建默认配置
            _logger.LogWarning("邮政分揽投机构配置不存在，使用默认配置");
            config = new PostCollectionConfig
            {
                ConfigId = PostCollectionConfig.SingletonId,
                Url = "http://localhost:8081/postal-collection", // 需要配置实际URL
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
    /// 扫描包裹到邮政分揽投机构
    /// Scan parcel to postal collection institution (getYJSM)
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;

        try
        {
            // 加载配置
            var config = await GetConfigAsync().ConfigureAwait(false);

            // Skip NoRead barcodes
            if (barcode.Contains("NoRead", StringComparison.OrdinalIgnoreCase))
            {
                stopwatch.Stop();
                const string notApplicableUrl = "SKIPPED://noread-barcode";
                var skipMessage = "NoRead barcode skipped";

                // 生成示例curl命令，展示如果处理该条码时的请求格式
                // Generate example curl command showing what the request would look like if processed
                var exampleBody = System.Text.Json.JsonSerializer.Serialize(new { barcode, operation = "scan", timestamp = _clock.LocalNow });
                var skipCurl = ApiRequestHelper.GenerateFormattedCurl(
                    "POST",
                    notApplicableUrl,
                    new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/json"
                    },
                    exampleBody);
                skipCurl = $"# NoRead barcode skipped - Example request format:\n{skipCurl}";

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
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = skipCurl,
                };
            }

            _logger.LogDebug("扫描包裹到邮政分揽投机构，条码: {Barcode}", barcode);

            // 构造SOAP请求
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

            // 生成请求头信息用于日志记录
            var requestHeaders = "Content-Type: text/xml; charset=utf-8\r\nSOAPAction: \"getYJSM\"";

            // 生成curl命令
            var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                config.Url,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/xml; charset=utf-8",
                    ["SOAPAction"] = string.Empty
                },
                soapRequest);

            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.PostAsync(config.Url, content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseContent = Regex.Unescape(responseContent);

            stopwatch.Stop();

            // 获取响应头信息
            var responseHeaders = string.Join("\r\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("扫描包裹成功（邮政分揽投机构），条码: {Barcode}", barcode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "Parcel scanned successfully at postal collection institution",
                    ResponseBody = responseContent,
                    ParcelId = barcode,
                    RequestUrl = config.Url,
                    RequestBody = soapRequest,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = curlCommand,
                };
            }
            else
            {
                _logger.LogWarning("扫描包裹失败（邮政分揽投机构），条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = $"Scan Error: {response.StatusCode}",
                    ResponseBody = responseContent,
                    ErrorMessage = $"Scan Error: {response.StatusCode}",
                    ParcelId = barcode,
                    RequestUrl = config.Url,
                    RequestBody = soapRequest,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = curlCommand,
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "扫描包裹异常（邮政分揽投机构），条码: {Barcode}", barcode);

            // 获取详细的异常信息，包括所有内部异常
            // Get detailed exception message including all inner exceptions
            var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);

            // 加载配置以获取URL（如果可能）
            var config = await GetConfigAsync().ConfigureAwait(false);

            // 构造SOAP请求用于生成curl（即使异常也需要生成curl）
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

            // 生成请求头信息
            var requestHeaders = "Content-Type: text/xml; charset=utf-8\r\nSOAPAction: \"getYJSM\"";

            // 生成curl命令（异常情况下也必须生成）
            var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                config.Url,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/xml; charset=utf-8",
                    ["SOAPAction"] = string.Empty
                },
                soapRequest);
            curlCommand = $"# Exception occurred during request - Curl command for retry:\n{curlCommand}";

            return new WcsApiResponse
            {
                RequestStatus = ApiRequestStatus.Exception,
                FormattedMessage = detailedMessage,
                ResponseBody = ex.ToString(),
                ErrorMessage = detailedMessage,
                ParcelId = barcode,
                RequestUrl = config.Url,
                RequestBody = soapRequest,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                ResponseStatusCode = null,
                ResponseHeaders = null,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = curlCommand,
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
            // 加载配置
            var config = await GetConfigAsync().ConfigureAwait(false);

            // 先提交扫描信息
            await ScanParcelAsync(dwsData.Barcode, cancellationToken).ConfigureAwait(false);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{config.WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造格口查询SOAP请求
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

            // 生成请求头信息用于日志记录
            var requestHeaders = "Content-Type: text/xml; charset=utf-8\r\nSOAPAction: \"getLTGKCX\"";

            // 生成curl命令
            var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                config.Url,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/xml; charset=utf-8",
                    ["SOAPAction"] = string.Empty
                },
                soapRequest);

            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.PostAsync(config.Url, content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseContent = Regex.Unescape(responseContent);

            stopwatch.Stop();

            // 获取响应头信息
            var responseHeaders = string.Join("\r\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

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
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "Chute requested successfully",
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = soapRequest,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = curlCommand,
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
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = $"Chute Request Error: {response.StatusCode}",
                    ResponseBody = responseContent,
                    ErrorMessage = $"Chute Request Error: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = soapRequest,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = curlCommand,
                    OcrData = ocrData
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "请求格口异常（邮政分揽投机构），包裹ID: {ParcelId}, 耗时: {Duration}ms",
                parcelId, stopwatch.ElapsedMilliseconds);

            // 获取详细的异常信息，包括所有内部异常
            // Get detailed exception message including all inner exceptions
            var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);

            // 加载配置以获取URL（如果可能）
            var config = await GetConfigAsync().ConfigureAwait(false);

            // 构造SOAP请求用于生成curl（即使异常也需要生成curl）
            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{config.WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

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

            // 生成请求头信息
            var requestHeaders = "Content-Type: text/xml; charset=utf-8\r\nSOAPAction: \"getLTGKCX\"";

            // 生成curl命令（异常情况下也必须生成）
            var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                config.Url,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/xml; charset=utf-8",
                    ["SOAPAction"] = string.Empty
                },
                soapRequest);
            curlCommand = $"# Exception occurred during request - Curl command for retry:\n{curlCommand}";

            return new WcsApiResponse
            {
                RequestStatus = ApiRequestStatus.Exception,
                FormattedMessage = detailedMessage,
                ResponseBody = ex.ToString(),
                ErrorMessage = detailedMessage,
                ParcelId = parcelId,
                RequestUrl = config.Url,
                RequestBody = soapRequest,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                ResponseStatusCode = null,
                ResponseHeaders = null,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = curlCommand,
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

        const string notApplicableUrl = "NOT_IMPLEMENTED://upload-image";
        var notImplementedMessage = "邮政分揽投机构图片上传功能未实现 / Postal collection institution image upload feature not implemented";

        // 生成示例curl命令，展示如果实现该功能时的请求格式
        // Generate example curl command showing what the request would look like if implemented
        var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
            "POST",
            notApplicableUrl,
            new Dictionary<string, string>
            {
                ["Content-Type"] = $"multipart/form-data; boundary=----WebKitFormBoundary",
                ["X-Barcode"] = barcode
            },
            $"------WebKitFormBoundary\nContent-Disposition: form-data; name=\"file\"; filename=\"{barcode}.jpg\"\nContent-Type: {contentType}\n\n[Binary image data: {imageData.Length} bytes]\n------WebKitFormBoundary--");
        curlCommand = $"# Feature not implemented - Example request format:\n{curlCommand}";

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
        });
    }

    /// <summary>
    /// 落格回调 - 通知邮政分揽投机构包裹已落入指定格口
    /// Chute landing callback (notifyChuteLanding)
    /// 对应参考代码 PostInApi.UploadInBackground 方法
    /// Corresponds to PostInApi.UploadInBackground method in reference code
    /// </summary>
    public async Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;

        try
        {
            // 加载配置
            var config = await GetConfigAsync().ConfigureAwait(false);

            _logger.LogDebug("落格回调（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}",
                parcelId, chuteId, barcode);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{config.WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造落格回调SOAP请求
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

            // 生成请求头信息用于日志记录
            var requestHeaders = "Content-Type: text/xml; charset=utf-8\r\nSOAPAction: \"notifyChuteLanding\"";

            // 生成curl命令
            var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                config.Url,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/xml; charset=utf-8",
                    ["SOAPAction"] = string.Empty
                },
                soapRequest);

            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.PostAsync(config.Url, content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseContent = Regex.Unescape(responseContent);

            stopwatch.Stop();

            // 获取响应头信息
            var responseHeaders = string.Join("\r\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "落格回调成功（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}",
                    parcelId, chuteId, barcode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Success,
                    FormattedMessage = "Chute landing notification sent successfully",
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = soapRequest,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = curlCommand
                };
            }
            else
            {
                _logger.LogWarning(
                    "落格回调失败（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}, 状态码: {StatusCode}",
                    parcelId, chuteId, response.StatusCode);

                return new WcsApiResponse
                {
                    RequestStatus = ApiRequestStatus.Failure,
                    FormattedMessage = $"Chute landing notification error: {response.StatusCode}",
                    ResponseBody = responseContent,
                    ErrorMessage = $"Chute landing notification error: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestUrl = config.Url,
                    RequestBody = soapRequest,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = curlCommand
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "落格回调异常（邮政分揽投机构），包裹ID: {ParcelId}, 格口: {ChuteId}",
                parcelId, chuteId);

            // 获取详细的异常信息，包括所有内部异常
            // Get detailed exception message including all inner exceptions
            var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);

            // 加载配置以获取URL（如果可能）
            var config = await GetConfigAsync().ConfigureAwait(false);

            // 构造SOAP请求用于生成curl（即使异常也需要生成curl）
            var seqNum = GetNextSequenceNumber();
            var yearMonth = _clock.LocalNow.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{config.WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

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

            // 生成请求头信息
            var requestHeaders = "Content-Type: text/xml; charset=utf-8\r\nSOAPAction: \"notifyChuteLanding\"";

            // 生成curl命令（异常情况下也必须生成）
            var curlCommand = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                config.Url,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/xml; charset=utf-8",
                    ["SOAPAction"] = string.Empty
                },
                soapRequest);
            curlCommand = $"# Exception occurred during request - Curl command for retry:\n{curlCommand}";

            return new WcsApiResponse
            {
                RequestStatus = ApiRequestStatus.Exception,
                FormattedMessage = detailedMessage,
                ResponseBody = ex.ToString(),
                ErrorMessage = detailedMessage,
                ParcelId = parcelId,
                RequestUrl = config.Url,
                RequestBody = soapRequest,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                ResponseStatusCode = null,
                ResponseHeaders = null,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = curlCommand
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
