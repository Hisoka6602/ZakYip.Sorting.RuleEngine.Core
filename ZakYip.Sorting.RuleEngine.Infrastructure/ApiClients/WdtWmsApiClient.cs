using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 旺店通WMS API适配器实现
/// WDT (Wang Dian Tong) WMS API adapter implementation
/// </summary>
public class WdtWmsApiClient : IThirdPartyApiAdapter
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
    /// 上传包裹和DWS数据到第三方API（旺店通WMS数据上传）
    /// Upload parcel and DWS data to third-party API (WDT WMS data upload)
    /// </summary>
    public async Task<ThirdPartyResponse> UploadDataAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始上传数据，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // 构造请求数据
            var requestData = new
            {
                appkey = _appKey,
                timestamp,
                barcode = dwsData.Barcode,
                weight = dwsData.Weight.ToString("F3"),
                length = dwsData.Length.ToString("F2"),
                width = dwsData.Width.ToString("F2"),
                height = dwsData.Height.ToString("F2"),
                volume = dwsData.Volume.ToString("F6")
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
            var response = await _httpClient.PostAsync("/openapi/data/upload", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 上传数据成功，包裹ID: {ParcelId}",
                    parcelInfo.ParcelId);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "上传数据成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 上传数据失败，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                    parcelInfo.ParcelId, response.StatusCode);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"上传数据失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WDT WMS - 上传数据异常，包裹ID: {ParcelId}", parcelInfo.ParcelId);

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
    /// 扫描包裹
    /// Scan parcel to register it in the third-party system
    /// </summary>
    public async Task<ThirdPartyResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始扫描包裹，条码: {Barcode}", barcode);

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

            var response = await _httpClient.PostAsync("/openapi/parcel/scan", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 扫描包裹成功，条码: {Barcode}",
                    barcode);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "扫描包裹成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 扫描包裹失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"扫描包裹失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WDT WMS - 扫描包裹异常，条码: {Barcode}", barcode);

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
    /// 请求格口
    /// Request a chute/gate number for the parcel
    /// </summary>
    public async Task<ThirdPartyResponse> RequestChuteAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始查询包裹/请求格口，条码: {Barcode}", barcode);

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
    /// 上传图片
    /// Upload image to third-party API
    /// </summary>
    public async Task<ThirdPartyResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = "image/jpeg",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "WDT WMS - 开始上传图片，条码: {Barcode}, 大小: {Size} bytes",
                barcode, imageData.Length);

            using var formContent = new MultipartFormDataContent();
            
            formContent.Add(new StringContent(_appKey), "appkey");
            formContent.Add(new StringContent(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()), "timestamp");
            formContent.Add(new StringContent(barcode), "barcode");
            
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(imageContent, "image", $"{barcode}.jpg");

            var response = await _httpClient.PostAsync("/openapi/parcel/image", formContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 上传图片成功，条码: {Barcode}",
                    barcode);

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
