using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// WCS API客户端实现
/// WCS API client implementation
/// 配置从LiteDB加载，支持热更新
/// Configuration loaded from LiteDB with hot reload support
/// </summary>
public class WcsApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WcsApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ISystemClock _clock;
    private readonly IWcsApiConfigRepository _configRepository;
    
    // 缓存配置以避免每次请求都查询数据库
    // 使用 SemaphoreSlim 保证线程安全
    private WcsApiConfig? _cachedConfig;
    private DateTime _configCacheTime = DateTime.MinValue;
    private readonly TimeSpan _configCacheExpiry = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _configLock = new(1, 1);

    public WcsApiClient(
        HttpClient httpClient,
        ILogger<WcsApiClient> logger,
        ISystemClock clock,
        IWcsApiConfigRepository configRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        _configRepository = configRepository;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    /// <summary>
    /// 获取配置，使用缓存以提高性能（线程安全）
    /// Get configuration with caching for performance (thread-safe)
    /// </summary>
    private async Task<WcsApiConfig> GetConfigAsync()
    {
        // 快速检查缓存（无锁）
        if (_cachedConfig != null && _clock.LocalNow - _configCacheTime < _configCacheExpiry)
        {
            return _cachedConfig;
        }

        // 使用锁保护配置加载
        await _configLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // 双重检查：其他线程可能已经加载了配置
            if (_cachedConfig != null && _clock.LocalNow - _configCacheTime < _configCacheExpiry)
            {
                return _cachedConfig;
            }

            // 从数据库加载配置
            var config = await _configRepository.GetByIdAsync(WcsApiConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                // 如果配置不存在，创建默认配置
                _logger.LogWarning("WCS API配置不存在，使用默认配置");
                config = new WcsApiConfig
                {
                    ConfigId = WcsApiConfig.SingletonId,
                    ActiveAdapterType = "WcsApiClient", // 默认适配器类型
                    Url = "http://localhost:8080/wcs", // 默认URL，需要通过API更新
                    ApiKey = null,
                    TimeoutMs = 30000,
                    DisableSslValidation = false,
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
        finally
        {
            _configLock.Release();
        }
    }

    /// <summary>
    /// 创建成功响应 / Create success response
    /// </summary>
    private WcsApiResponse CreateSuccessResponse(
        string message,
        string? responseContent,
        string parcelId,
        string requestUrl,
        string requestBody,
        string? requestHeaders,
        DateTime requestTime,
        int statusCode,
        string? responseHeaders,
        long durationMs,
        string? formattedCurl,
        OcrData? ocrData = null) => new()
    {
        RequestStatus = ApiRequestStatus.Success,
        FormattedMessage = message,
        ResponseBody = responseContent,
        ParcelId = parcelId,
        RequestUrl = requestUrl,
        RequestBody = requestBody,
        RequestHeaders = requestHeaders,
        RequestTime = requestTime,
        ResponseTime = _clock.LocalNow,
        ResponseStatusCode = statusCode,
        ResponseHeaders = responseHeaders,
        DurationMs = durationMs,
        FormattedCurl = formattedCurl,
        CurlData = formattedCurl ?? string.Empty,
        OcrData = ocrData
    };

    /// <summary>
    /// 创建错误响应 / Create error response
    /// </summary>
    private WcsApiResponse CreateErrorResponse(
        string message,
        string? responseContent,
        string parcelId,
        string requestUrl,
        string requestBody,
        string? requestHeaders,
        DateTime requestTime,
        int statusCode,
        string? responseHeaders,
        long durationMs,
        string? formattedCurl,
        OcrData? ocrData = null) => new()
    {
        RequestStatus = ApiRequestStatus.Failure,
        FormattedMessage = message,
        ErrorMessage = message,
        ResponseBody = responseContent,
        ParcelId = parcelId,
        RequestUrl = requestUrl,
        RequestBody = requestBody,
        RequestHeaders = requestHeaders,
        RequestTime = requestTime,
        ResponseTime = _clock.LocalNow,
        ResponseStatusCode = statusCode,
        ResponseHeaders = responseHeaders,
        DurationMs = durationMs,
        FormattedCurl = formattedCurl,
        CurlData = formattedCurl ?? string.Empty,
        OcrData = ocrData
    };

    /// <summary>
    /// 创建异常响应 / Create exception response
    /// </summary>
    private WcsApiResponse CreateExceptionResponse(
        Exception ex,
        string parcelId,
        string requestUrl,
        string requestBody,
        string? requestHeaders,
        DateTime requestTime,
        HttpResponseMessage? response,
        string? responseHeaders,
        long durationMs,
        string? formattedCurl,
        OcrData? ocrData = null)
    {
        // 获取详细的异常信息，包括所有内部异常
        // Get detailed exception message including all inner exceptions
        var detailedMessage = ApiRequestHelper.GetDetailedExceptionMessage(ex);
        
        return new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Exception,
            FormattedMessage = $"请求异常 / Request exception: {detailedMessage}",
            ErrorMessage = detailedMessage,
            ResponseBody = ex.ToString(),
            ParcelId = parcelId,
            RequestUrl = requestUrl,
            RequestBody = requestBody,
            RequestHeaders = requestHeaders,
            RequestTime = requestTime,
            ResponseTime = _clock.LocalNow,
            ResponseStatusCode = response?.StatusCode != null ? (int)response.StatusCode : null,
            ResponseHeaders = responseHeaders,
            DurationMs = durationMs,
            FormattedCurl = formattedCurl,
            CurlData = formattedCurl ?? string.Empty,
            OcrData = ocrData
        };
    }

    /// <summary>
    /// 扫描包裹
    /// Scan parcel to register it in the wcs system
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        // 加载配置
        var config = await GetConfigAsync().ConfigureAwait(false);
        
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;
        
        // 使用配置中的URL构建完整请求URL
        var requestUrl = $"{config.Url.TrimEnd('/')}/{WcsEndpoints.ParcelScan.TrimStart('/')}";
        
        // 构造请求数据
        var requestData = new
        {
            barcode,
            scanTime = _clock.LocalNow
        };
        var json = JsonSerializer.Serialize(requestData, _jsonOptions);
        
        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            _logger.LogDebug("开始扫描包裹，条码: {Barcode}, URL: {Url}", barcode, requestUrl);

            var content = new StringContent(json, Encoding.UTF8, ContentTypes.ApplicationJson);

            // 生成请求信息
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = content
            };
            
            // 如果配置了ApiKey，添加到请求头
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                request.Headers.Add("X-API-Key", config.ApiKey);
            }
            
            formattedCurl = await ApiRequestHelper.GenerateFormattedCurlFromRequestAsync(request);
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

            // 🔧 新增：记录完整的请求详情到日志文件
            _logger.LogInformation(
                "WCS API请求 [ScanParcel] - URL: {Url}, Barcode: {Barcode}, RequestBody: {RequestBody}, Headers: {Headers}",
                requestUrl, barcode, json, requestHeaders);

            // 发送POST请求
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            stopwatch.Stop();

            // 🔧 新增：记录完整的响应详情到日志文件
            _logger.LogInformation(
                "WCS API响应 [ScanParcel] - Barcode: {Barcode}, StatusCode: {StatusCode}, Duration: {Duration}ms, ResponseBody: {ResponseBody}, ResponseHeaders: {ResponseHeaders}",
                barcode, response.StatusCode, stopwatch.ElapsedMilliseconds, responseContent, responseHeaders);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "扫描包裹成功，条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return CreateSuccessResponse(
                    "Parcel scanned successfully",
                    responseContent,
                    barcode,
                    requestUrl,
                    json,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl);
            }
            else
            {
                _logger.LogWarning(
                    "扫描包裹失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return CreateErrorResponse(
                    $"Scan Error: {response.StatusCode}",
                    responseContent,
                    barcode,
                    requestUrl,
                    json,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 🔧 新增：记录异常详情到日志文件
            _logger.LogError(ex, 
                "WCS API异常 [ScanParcel] - Barcode: {Barcode}, Duration: {Duration}ms, RequestBody: {RequestBody}, Exception: {Exception}",
                barcode, stopwatch.ElapsedMilliseconds, json, ex.ToString());

            _logger.LogError(ex, "扫描包裹异常，条码: {Barcode}, 耗时: {Duration}ms", barcode, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(
                ex,
                barcode,
                requestUrl,
                json,
                requestHeaders,
                requestTime,
                response,
                responseHeaders,
                stopwatch.ElapsedMilliseconds,
                formattedCurl);
        }
    }

    /// <summary>
    /// 请求格口
    /// Request a chute/gate number for the parcel
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        // 加载配置
        var config = await GetConfigAsync().ConfigureAwait(false);
        
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;
        
        // 使用配置中的URL构建完整请求URL
        var requestUrl = $"{config.Url.TrimEnd('/')}/{WcsEndpoints.ChuteRequest.TrimStart('/')}";
        
        // 构造请求数据 - 包含DWS数据
        var requestData = new
        {
            parcelId,
            barcode = dwsData.Barcode,
            weight = dwsData.Weight,
            length = dwsData.Length,
            width = dwsData.Width,
            height = dwsData.Height,
            volume = dwsData.Volume,
            scanTime = dwsData.ScannedAt,
            ocrData = ocrData != null ? new
            {
                threeSegmentCode = ocrData.ThreeSegmentCode,
                firstSegmentCode = ocrData.FirstSegmentCode,
                secondSegmentCode = ocrData.SecondSegmentCode,
                thirdSegmentCode = ocrData.ThirdSegmentCode,
                recipientAddress = ocrData.RecipientAddress
            } : null,
            requestTime = _clock.LocalNow
        };
        
        var json = JsonSerializer.Serialize(requestData, _jsonOptions);
        
        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            _logger.LogDebug("开始请求格口，包裹ID: {ParcelId}, 条码: {Barcode}, URL: {Url}", parcelId, dwsData.Barcode, requestUrl);

            var content = new StringContent(json, Encoding.UTF8, ContentTypes.ApplicationJson);

            // 生成请求信息
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = content
            };
            
            // 如果配置了ApiKey，添加到请求头
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                request.Headers.Add("X-API-Key", config.ApiKey);
            }
            
            formattedCurl = await ApiRequestHelper.GenerateFormattedCurlFromRequestAsync(request);
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

            // 🔧 新增：记录完整的请求详情到日志文件
            _logger.LogInformation(
                "WCS API请求 [RequestChute] - URL: {Url}, ParcelId: {ParcelId}, Barcode: {Barcode}, RequestBody: {RequestBody}, Headers: {Headers}",
                requestUrl, parcelId, dwsData.Barcode, json, requestHeaders);

            // 发送POST请求
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);
            
            stopwatch.Stop();

            // 🔧 新增：记录完整的响应详情到日志文件
            _logger.LogInformation(
                "WCS API响应 [RequestChute] - ParcelId: {ParcelId}, Barcode: {Barcode}, StatusCode: {StatusCode}, Duration: {Duration}ms, ResponseBody: {ResponseBody}, ResponseHeaders: {ResponseHeaders}",
                parcelId, dwsData.Barcode, response.StatusCode, stopwatch.ElapsedMilliseconds, responseContent, responseHeaders);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "请求格口成功，包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return CreateSuccessResponse(
                    "Chute requested successfully",
                    responseContent,
                    parcelId,
                    requestUrl,
                    json,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl,
                    ocrData);
            }
            else
            {
                _logger.LogWarning(
                    "请求格口失败，包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return CreateErrorResponse(
                    $"Chute Request Error: {response.StatusCode}",
                    responseContent,
                    parcelId,
                    requestUrl,
                    json,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl,
                    ocrData);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 🔧 新增：记录异常详情到日志文件
            _logger.LogError(ex, 
                "WCS API异常 [RequestChute] - ParcelId: {ParcelId}, Barcode: {Barcode}, Duration: {Duration}ms, RequestBody: {RequestBody}, Exception: {Exception}",
                parcelId, dwsData.Barcode, stopwatch.ElapsedMilliseconds, json, ex.ToString());
            
            _logger.LogError(ex, "请求格口异常，包裹ID: {ParcelId}, 耗时: {Duration}ms", parcelId, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(
                ex,
                parcelId,
                requestUrl,
                json,
                requestHeaders,
                requestTime,
                response,
                responseHeaders,
                stopwatch.ElapsedMilliseconds,
                formattedCurl,
                ocrData);
        }
    }

    /// <summary>
    /// 上传图片
    /// Upload image to wcs API
    /// </summary>
    public async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        // 加载配置
        var config = await GetConfigAsync().ConfigureAwait(false);
        
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;
        
        // 使用配置中的URL构建完整请求URL
        var requestUrl = $"{config.Url.TrimEnd('/')}/{WcsEndpoints.ImageUpload.TrimStart('/')}";
        
        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        var requestBody = $"[multipart form data: barcode={barcode}, image size={imageData.Length} bytes]";
        
        try
        {
            _logger.LogDebug("开始上传图片，条码: {Barcode}, 图片大小: {Size} bytes, 类型: {ContentType}, URL: {Url}", 
                barcode, imageData.Length, contentType, requestUrl);

            // 构造multipart/form-data请求
            using var formContent = new MultipartFormDataContent();
            
            // 添加条码字段
            formContent.Add(new StringContent(barcode), "barcode");
            
            // 根据内容类型确定文件扩展名
            var extension = contentType switch
            {
                ContentTypes.ImageJpeg => ".jpg",
                ContentTypes.ImagePng => ".png",
                ContentTypes.ImageGif => ".gif",
                ContentTypes.ImageBmp => ".bmp",
                ContentTypes.ImageWebp => ".webp",
                _ => ".bin"
            };
            
            // 添加图片文件
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(imageContent, "image", $"{barcode}{extension}");

            // 生成请求信息
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = formContent
            };
            
            // 如果配置了ApiKey，添加到请求头
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                request.Headers.Add("X-API-Key", config.ApiKey);
            }
            
            // 注意：对于multipart/form-data，我们简化FormattedCurl（因为包含二进制数据）
            var boundaryParam = formContent.Headers.ContentType?.Parameters.FirstOrDefault(p => p.Name == "boundary");
            var boundary = boundaryParam?.Value ?? "----WebKitFormBoundary";
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = $"multipart/form-data; boundary={boundary}"
            };
            formattedCurl = ApiRequestHelper.GenerateFormattedCurl(
                "POST", 
                requestUrl, 
                headers, 
                requestBody);
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

            // 🔧 新增：记录完整的请求详情到日志文件（不包含二进制数据）
            _logger.LogInformation(
                "WCS API请求 [UploadImage] - URL: {Url}, Barcode: {Barcode}, ImageSize: {Size} bytes, ContentType: {ContentType}, Headers: {Headers}",
                requestUrl, barcode, imageData.Length, contentType, requestHeaders);

            // 发送POST请求
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            stopwatch.Stop();

            // 🔧 新增：记录完整的响应详情到日志文件
            _logger.LogInformation(
                "WCS API响应 [UploadImage] - Barcode: {Barcode}, StatusCode: {StatusCode}, Duration: {Duration}ms, ResponseBody: {ResponseBody}, ResponseHeaders: {ResponseHeaders}",
                barcode, response.StatusCode, stopwatch.ElapsedMilliseconds, responseContent, responseHeaders);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传图片成功，条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return CreateSuccessResponse(
                    "Image uploaded successfully",
                    responseContent,
                    barcode,
                    requestUrl,
                    requestBody,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl);
            }
            else
            {
                _logger.LogWarning(
                    "上传图片失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return CreateErrorResponse(
                    $"Image Upload Error: {response.StatusCode}",
                    responseContent,
                    barcode,
                    requestUrl,
                    requestBody,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 🔧 新增：记录异常详情到日志文件
            _logger.LogError(ex, 
                "WCS API异常 [UploadImage] - Barcode: {Barcode}, ImageSize: {Size} bytes, Duration: {Duration}ms, Exception: {Exception}",
                barcode, imageData.Length, stopwatch.ElapsedMilliseconds, ex.ToString());
            
            _logger.LogError(ex, "上传图片异常，条码: {Barcode}, 耗时: {Duration}ms", barcode, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(
                ex,
                barcode,
                requestUrl,
                requestBody,
                requestHeaders,
                requestTime,
                response,
                responseHeaders,
                stopwatch.ElapsedMilliseconds,
                formattedCurl);
        }
    }

    /// <summary>
    /// 落格回调 - 通知WCS系统包裹已落入指定格口
    /// Chute landing callback - Notify WCS system that parcel has landed in the specified chute
    /// </summary>
    public async Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        // 加载配置
        var config = await GetConfigAsync().ConfigureAwait(false);
        
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;
        
        // 使用配置中的URL构建完整请求URL
        const string endpoint = "/api/wcs/chute-landing"; // This should be defined in WcsEndpoints
        var requestUrl = $"{config.Url.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        
        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        string requestBody = string.Empty;
        
        try
        {
            _logger.LogDebug("开始落格回调，包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}, URL: {Url}", 
                parcelId, chuteId, barcode, requestUrl);

            // 构造请求体
            var requestData = new
            {
                parcelId,
                chuteId,
                barcode,
                landingTime = _clock.LocalNow
            };

            requestBody = System.Text.Json.JsonSerializer.Serialize(requestData, _jsonOptions);
            using var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            // 生成格式化的curl命令
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            formattedCurl = ApiRequestHelper.GenerateFormattedCurl("POST", requestUrl, headers, requestBody);

            // 创建请求
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = content
            };
            
            // 如果配置了ApiKey，添加到请求头
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                request.Headers.Add("X-API-Key", config.ApiKey);
            }
            
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

            // 🔧 新增：记录完整的请求详情到日志文件
            _logger.LogInformation(
                "WCS API请求 [NotifyChuteLanding] - URL: {Url}, ParcelId: {ParcelId}, ChuteId: {ChuteId}, Barcode: {Barcode}, RequestBody: {RequestBody}, Headers: {Headers}",
                requestUrl, parcelId, chuteId, barcode, requestBody, requestHeaders);

            // 发送POST请求
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            stopwatch.Stop();

            // 🔧 新增：记录完整的响应详情到日志文件
            _logger.LogInformation(
                "WCS API响应 [NotifyChuteLanding] - ParcelId: {ParcelId}, ChuteId: {ChuteId}, Barcode: {Barcode}, StatusCode: {StatusCode}, Duration: {Duration}ms, ResponseBody: {ResponseBody}, ResponseHeaders: {ResponseHeaders}",
                parcelId, chuteId, barcode, response.StatusCode, stopwatch.ElapsedMilliseconds, responseContent, responseHeaders);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "落格回调成功，包裹ID: {ParcelId}, 格口: {ChuteId}, 条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, chuteId, barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return CreateSuccessResponse(
                    "Chute landing notification sent successfully",
                    responseContent,
                    parcelId,
                    requestUrl,
                    requestBody,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl);
            }
            else
            {
                _logger.LogWarning(
                    "落格回调失败，包裹ID: {ParcelId}, 格口: {ChuteId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    parcelId, chuteId, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return CreateErrorResponse(
                    $"Chute landing notification error: {response.StatusCode}",
                    responseContent,
                    parcelId,
                    requestUrl,
                    requestBody,
                    requestHeaders,
                    requestTime,
                    (int)response.StatusCode,
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds,
                    formattedCurl);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 🔧 新增：记录异常详情到日志文件
            _logger.LogError(ex, 
                "WCS API异常 [NotifyChuteLanding] - ParcelId: {ParcelId}, ChuteId: {ChuteId}, Barcode: {Barcode}, Duration: {Duration}ms, RequestBody: {RequestBody}, Exception: {Exception}",
                parcelId, chuteId, barcode, stopwatch.ElapsedMilliseconds, requestBody, ex.ToString());
            
            _logger.LogError(ex, "落格回调异常，包裹ID: {ParcelId}, 格口: {ChuteId}, 耗时: {Duration}ms", 
                parcelId, chuteId, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(
                ex,
                parcelId,
                requestUrl,
                requestBody,
                requestHeaders,
                requestTime,
                response,
                responseHeaders,
                stopwatch.ElapsedMilliseconds,
                formattedCurl);
        }
    }
}
