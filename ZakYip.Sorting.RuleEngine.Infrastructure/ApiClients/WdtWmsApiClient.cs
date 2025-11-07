using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 旺店通WMS API客户端实现
/// WDT (Wang Dian Tong) WMS API client implementation
/// </summary>
public class WdtWmsApiClient : IWdtWmsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WdtWmsApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _appKey;
    private readonly string _appSecret;

    public WdtWmsApiClient(
        HttpClient httpClient,
        ILogger<WdtWmsApiClient> logger,
        string appKey = "",
        string appSecret = "")
    {
        _httpClient = httpClient;
        _logger = logger;
        _appKey = appKey;
        _appSecret = appSecret;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// 包裹称重扫描
    /// Parcel weight scanning
    /// </summary>
    public async Task<ThirdPartyResponse> WeighScanAsync(
        string barcode,
        decimal weight,
        decimal length,
        decimal width,
        decimal height,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始称重扫描，条码: {Barcode}, 重量: {Weight}kg", barcode, weight);

            // 构造请求数据
            var requestData = new
            {
                appkey = _appKey,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                barcode,
                weight = weight.ToString("F3"),
                length = length.ToString("F2"),
                width = width.ToString("F2"),
                height = height.ToString("F2"),
                volume = (length * width * height / 1000000).ToString("F6") // 转换为立方米
            };

            // 生成签名
            var sign = GenerateSign(requestData);
            
            var requestWithSign = new
            {
                requestData.appkey,
                requestData.timestamp,
                requestData.barcode,
                requestData.weight,
                requestData.length,
                requestData.width,
                requestData.height,
                requestData.volume,
                sign
            };

            var json = JsonSerializer.Serialize(requestWithSign, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 发送POST请求
            var response = await _httpClient.PostAsync("/openapi/weigh/scan", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 称重扫描成功，条码: {Barcode}, 重量: {Weight}kg",
                    barcode, weight);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "称重扫描成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 称重扫描失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"称重扫描失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WDT WMS - 称重扫描异常，条码: {Barcode}", barcode);

            return new ThirdPartyResponse
            {
                Success = false,
                Code = "ERROR",
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 查询包裹信息
    /// Query parcel information
    /// </summary>
    public async Task<ThirdPartyResponse> QueryParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始查询包裹，条码: {Barcode}", barcode);

            var requestData = new
            {
                appkey = _appKey,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                barcode
            };

            var sign = GenerateSign(requestData);
            
            var requestWithSign = new
            {
                requestData.appkey,
                requestData.timestamp,
                requestData.barcode,
                sign
            };

            var json = JsonSerializer.Serialize(requestWithSign, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/openapi/parcel/query", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 查询包裹成功，条码: {Barcode}",
                    barcode);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "查询包裹成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 查询包裹失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"查询包裹失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WDT WMS - 查询包裹异常，条码: {Barcode}", barcode);

            return new ThirdPartyResponse
            {
                Success = false,
                Code = "ERROR",
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 上传包裹图片
    /// Upload parcel image
    /// </summary>
    public async Task<ThirdPartyResponse> UploadParcelImageAsync(
        string barcode,
        byte[] imageData,
        string imageType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "WDT WMS - 开始上传图片，条码: {Barcode}, 类型: {ImageType}, 大小: {Size} bytes",
                barcode, imageType, imageData.Length);

            using var formContent = new MultipartFormDataContent();
            
            formContent.Add(new StringContent(_appKey), "appkey");
            formContent.Add(new StringContent(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()), "timestamp");
            formContent.Add(new StringContent(barcode), "barcode");
            formContent.Add(new StringContent(imageType), "imageType");
            
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            formContent.Add(imageContent, "image", $"{barcode}_{imageType}.jpg");

            var response = await _httpClient.PostAsync("/openapi/parcel/image", formContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 上传图片成功，条码: {Barcode}, 类型: {ImageType}",
                    barcode, imageType);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "上传图片成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 上传图片失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"上传图片失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WDT WMS - 上传图片异常，条码: {Barcode}", barcode);

            return new ThirdPartyResponse
            {
                Success = false,
                Code = "ERROR",
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 生成签名
    /// Generate signature for API authentication
    /// </summary>
    private string GenerateSign(object requestData)
    {
        if (string.IsNullOrEmpty(_appSecret))
        {
            return string.Empty;
        }

        // 将对象转换为JSON字符串
        var json = JsonSerializer.Serialize(requestData, _jsonOptions);
        
        // 使用MD5生成签名
        var signString = $"{json}{_appSecret}";
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
