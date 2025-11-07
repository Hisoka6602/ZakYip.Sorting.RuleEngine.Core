using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 聚水潭ERP API适配器实现
/// Jushuituan ERP API adapter implementation
/// </summary>
public class JushuitanErpApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JushuitanErpApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _partnerKey;
    private readonly string _partnerSecret;
    private readonly string _token;

    public JushuitanErpApiClient(
        HttpClient httpClient,
        ILogger<JushuitanErpApiClient> logger,
        string partnerKey = "",
        string partnerSecret = "",
        string token = "")
    {
        _httpClient = httpClient;
        _logger = logger;
        _partnerKey = partnerKey;
        _partnerSecret = partnerSecret;
        _token = token;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// 上传包裹和DWS数据到WCS API（聚水潭ERP称重回传）
    /// Upload parcel and DWS data to wcs API (Jushuituan ERP weight callback)
    /// </summary>
    public async Task<WcsApiResponse> UploadDataAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("聚水潭ERP - 开始上传数据，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            // 构造业务参数
            var bizContent = new
            {
                so_id = dwsData.Barcode,
                weight = dwsData.Weight.ToString("F3"),
                length = dwsData.Length.ToString("F2"),
                width = dwsData.Width.ToString("F2"),
                height = dwsData.Height.ToString("F2"),
                volume = dwsData.Volume.ToString("F6")
            };

            var bizContentJson = JsonSerializer.Serialize(bizContent, _jsonOptions);

            // 构造完整请求
            var requestData = new Dictionary<string, string>
            {
                { "partnerkey", _partnerKey },
                { "token", _token },
                { "ts", timestamp },
                { "method", "weighing.upload" },
                { "charset", "utf-8" },
                { "biz_content", bizContentJson }
            };

            // 生成签名
            var sign = GenerateSign(requestData);
            requestData.Add("sign", sign);

            var content = new FormUrlEncodedContent(requestData);

            // 发送POST请求
            var response = await _httpClient.PostAsync("/open/api/weigh/upload", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "聚水潭ERP - 上传数据成功，包裹ID: {ParcelId}",
                    parcelInfo.ParcelId);

                return new WcsApiResponse
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
                    "聚水潭ERP - 上传数据失败，包裹ID: {ParcelId}, 状态码: {StatusCode}, 响应: {Response}",
                    parcelInfo.ParcelId, response.StatusCode, responseContent);

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
            _logger.LogError(ex, "聚水潭ERP - 上传数据异常，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            return new WcsApiResponse
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
    /// Scan parcel to register it in the wcs system
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("聚水潭ERP - 开始扫描包裹/查询订单，条码: {Barcode}", barcode);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var bizContent = new
            {
                so_id = barcode,
                page_index = 1,
                page_size = 1
            };

            var bizContentJson = JsonSerializer.Serialize(bizContent, _jsonOptions);

            var requestData = new Dictionary<string, string>
            {
                { "partnerkey", _partnerKey },
                { "token", _token },
                { "ts", timestamp },
                { "method", "orders.single.query" },
                { "charset", "utf-8" },
                { "biz_content", bizContentJson }
            };

            var sign = GenerateSign(requestData);
            requestData.Add("sign", sign);

            var content = new FormUrlEncodedContent(requestData);

            var response = await _httpClient.PostAsync("/open/api/orders/query", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "聚水潭ERP - 扫描包裹/查询订单成功，条码: {Barcode}",
                    barcode);

                return new WcsApiResponse
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
                    "聚水潭ERP - 扫描包裹/查询订单失败，条码: {Barcode}, 状态码: {StatusCode}",
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
            _logger.LogError(ex, "聚水潭ERP - 扫描包裹/查询订单异常，条码: {Barcode}", barcode);

            return new WcsApiResponse
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
    public async Task<WcsApiResponse> RequestChuteAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "聚水潭ERP - 开始请求格口/更新物流，条码: {Barcode}",
                barcode);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var bizContent = new
            {
                so_id = barcode,
                lc_id = "AUTO",  // 自动分配
                l_id = barcode,
                modified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var bizContentJson = JsonSerializer.Serialize(bizContent, _jsonOptions);

            var requestData = new Dictionary<string, string>
            {
                { "partnerkey", _partnerKey },
                { "token", _token },
                { "ts", timestamp },
                { "method", "logistic.upload" },
                { "charset", "utf-8" },
                { "biz_content", bizContentJson }
            };

            var sign = GenerateSign(requestData);
            requestData.Add("sign", sign);

            var content = new FormUrlEncodedContent(requestData);

            var response = await _httpClient.PostAsync("/open/api/logistic/update", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "聚水潭ERP - 请求格口/更新物流成功，条码: {Barcode}",
                    barcode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "请求格口成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "聚水潭ERP - 请求格口/更新物流失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"请求格口失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚水潭ERP - 请求格口/更新物流异常，条码: {Barcode}", barcode);

            return new WcsApiResponse
            {
                Success = false,
                Code = "ERROR",
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 上传图片（聚水潭ERP暂不支持，返回成功响应）
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
                "聚水潭ERP - 上传图片请求（当前版本不支持），条码: {Barcode}",
                barcode);

            // 聚水潭ERP当前版本不支持直接上传图片
            // 返回成功响应以保持接口一致性
            await Task.CompletedTask;

            return new WcsApiResponse
            {
                Success = true,
                Code = "200",
                Message = "聚水潭ERP暂不支持图片上传功能",
                Data = "{\"info\":\"Feature not supported\"}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚水潭ERP - 上传图片异常，条码: {Barcode}", barcode);

            return new WcsApiResponse
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
    private string GenerateSign(Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(_partnerSecret))
        {
            return string.Empty;
        }

        // 按字典序排序参数
        var sortedParams = parameters
            .Where(p => p.Key != "sign")
            .OrderBy(p => p.Key)
            .Select(p => $"{p.Key}{p.Value}");

        // 拼接字符串
        var signString = $"{_partnerSecret}{string.Join("", sortedParams)}{_partnerSecret}";

        // 使用MD5生成签名
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
