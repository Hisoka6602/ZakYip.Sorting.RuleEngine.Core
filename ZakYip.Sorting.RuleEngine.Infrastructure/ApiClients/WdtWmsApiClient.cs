using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 旺店通WMS API适配器实现
/// WDT (Wang Dian Tong) WMS API adapter implementation
/// </summary>
public class WdtWmsApiClient : IWcsApiAdapter
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
    /// 上传包裹和DWS数据到WCS API（旺店通WMS数据上传）
    /// Upload parcel and DWS data to wcs API (WDT WMS data upload)
    /// </summary>
    public async Task<WcsApiResponse> UploadDataAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始上传数据，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            // 构造业务参数 (body)
            var bodyData = new
            {
                barcode = dwsData.Barcode,
                weight = dwsData.Weight.ToString("F3"),
                length = dwsData.Length.ToString("F2"),
                width = dwsData.Width.ToString("F2"),
                height = dwsData.Height.ToString("F2"),
                volume = dwsData.Volume.ToString("F6")
            };

            var bodyJson = JsonSerializer.Serialize(bodyData, _jsonOptions);

            // 构造完整请求参数
            var requestData = new Dictionary<string, string>
            {
                { "method", ApiConstants.WdtWmsApi.Methods.WeighUpload },
                { "app_key", _appKey },
                { "timestamp", timestamp },
                { "format", ApiConstants.WdtWmsApi.CommonParams.FormatJson },
                { "v", ApiConstants.WdtWmsApi.CommonParams.Version },
                { "body", bodyJson }
            };

            // 生成签名
            var sign = GenerateSign(requestData);
            requestData.Add("sign", sign);

            var content = new FormUrlEncodedContent(requestData);

            // 发送POST请求
            var response = await _httpClient.PostAsync(ApiConstants.WdtWmsApi.RouterEndpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 上传数据成功，包裹ID: {ParcelId}",
                    parcelInfo.ParcelId);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ApiConstants.HttpStatusCodes.Success,
                    Message = "上传数据成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 上传数据失败，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                    parcelInfo.ParcelId, response.StatusCode);

                return new WcsApiResponse
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

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 扫描包裹
    /// Scan parcel to register it in the wcs system
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始扫描包裹，条码: {Barcode}", barcode);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            var bodyData = new
            {
                barcode
            };

            var bodyJson = JsonSerializer.Serialize(bodyData, _jsonOptions);

            var requestData = new Dictionary<string, string>
            {
                { "method", ApiConstants.WdtWmsApi.Methods.ParcelScan },
                { "app_key", _appKey },
                { "timestamp", timestamp },
                { "format", ApiConstants.WdtWmsApi.CommonParams.FormatJson },
                { "v", ApiConstants.WdtWmsApi.CommonParams.Version },
                { "body", bodyJson }
            };

            var sign = GenerateSign(requestData);
            requestData.Add("sign", sign);

            var content = new FormUrlEncodedContent(requestData);

            var response = await _httpClient.PostAsync(ApiConstants.WdtWmsApi.RouterEndpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 扫描包裹成功，条码: {Barcode}",
                    barcode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ApiConstants.HttpStatusCodes.Success,
                    Message = "扫描包裹成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 扫描包裹失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
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

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 请求格口
    /// Request a chute/gate number for the parcel
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("WDT WMS - 开始查询包裹/请求格口，条码: {Barcode}", barcode);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            var bodyData = new
            {
                barcode
            };

            var bodyJson = JsonSerializer.Serialize(bodyData, _jsonOptions);

            var requestData = new Dictionary<string, string>
            {
                { "method", ApiConstants.WdtWmsApi.Methods.ParcelQuery },
                { "app_key", _appKey },
                { "timestamp", timestamp },
                { "format", "json" },
                { "v", "1.0" },
                { "body", bodyJson }
            };

            var sign = GenerateSign(requestData);
            requestData.Add("sign", sign);

            var content = new FormUrlEncodedContent(requestData);

            var response = await _httpClient.PostAsync(ApiConstants.WdtWmsApi.RouterEndpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 查询包裹成功，条码: {Barcode}",
                    barcode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ApiConstants.HttpStatusCodes.Success,
                    Message = "查询包裹成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 查询包裹失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
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

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 上传图片
    /// Upload image to wcs API
    /// </summary>
    public async Task<WcsApiResponse> UploadImageAsync(
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

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // 对于文件上传，WDT通常使用multipart/form-data
            using var formContent = new MultipartFormDataContent();
            
            formContent.Add(new StringContent(ApiConstants.WdtWmsApi.Methods.ImageUpload), "method");
            formContent.Add(new StringContent(_appKey), "app_key");
            formContent.Add(new StringContent(timestamp), "timestamp");
            formContent.Add(new StringContent("json"), "format");
            formContent.Add(new StringContent("1.0"), "v");
            formContent.Add(new StringContent(barcode), "barcode");
            
            // 生成签名（签名不包含文件内容，只包含非文件参数）
            // Generate signature (signature excludes file content, only includes non-file parameters)
            var signParams = new Dictionary<string, string>
            {
                { "method", ApiConstants.WdtWmsApi.Methods.ImageUpload },
                { "app_key", _appKey },
                { "timestamp", timestamp },
                { "format", "json" },
                { "v", "1.0" },
                { "barcode", barcode }
            };
            
            var sign = GenerateSign(signParams);
            formContent.Add(new StringContent(sign), "sign");
            
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(imageContent, "image", $"{barcode}.jpg");

            var response = await _httpClient.PostAsync(ApiConstants.WdtWmsApi.RouterEndpoint, formContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WDT WMS - 上传图片成功，条码: {Barcode}",
                    barcode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ApiConstants.HttpStatusCodes.Success,
                    Message = "上传图片成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT WMS - 上传图片失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
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

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 生成签名
    /// Generate signature for API authentication
    /// WDT signature: md5(appsecret + key1value1key2value2... + appsecret)
    /// Parameters are sorted alphabetically by key before concatenation
    /// </summary>
    private string GenerateSign(Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(_appSecret))
        {
            return string.Empty;
        }

        // 按字典序排序参数（排除sign字段）
        var sortedParams = parameters
            .Where(p => p.Key != "sign")
            .OrderBy(p => p.Key)
            .Select(p => $"{p.Key}{p.Value}");

        // 拼接字符串: appsecret + key1value1key2value2... + appsecret
        var signString = $"{_appSecret}{string.Join("", sortedParams)}{_appSecret}";

        // 使用MD5生成签名
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
