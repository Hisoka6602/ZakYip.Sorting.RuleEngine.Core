using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 聚水潭ERP API客户端实现
/// Jushuituan ERP API client implementation
/// </summary>
public class JushuitanErpApiClient : IJushuitanErpApiClient
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
    /// 称重回传
    /// Weight data callback
    /// </summary>
    public async Task<ThirdPartyResponse> WeightCallbackAsync(
        string barcode,
        decimal weight,
        decimal length,
        decimal width,
        decimal height,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("聚水潭ERP - 开始称重回传，条码: {Barcode}, 重量: {Weight}kg", barcode, weight);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            // 构造业务参数
            var bizContent = new
            {
                so_id = barcode,
                weight = weight.ToString("F3"),
                length = length.ToString("F2"),
                width = width.ToString("F2"),
                height = height.ToString("F2"),
                volume = (length * width * height / 1000000).ToString("F6")
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
                    "聚水潭ERP - 称重回传成功，条码: {Barcode}, 重量: {Weight}kg",
                    barcode, weight);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "称重回传成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "聚水潭ERP - 称重回传失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"称重回传失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚水潭ERP - 称重回传异常，条码: {Barcode}", barcode);

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
    /// 查询订单信息
    /// Query order information
    /// </summary>
    public async Task<ThirdPartyResponse> QueryOrderAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("聚水潭ERP - 开始查询订单，条码: {Barcode}", barcode);

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
                    "聚水潭ERP - 查询订单成功，条码: {Barcode}",
                    barcode);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "查询订单成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "聚水潭ERP - 查询订单失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"查询订单失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚水潭ERP - 查询订单异常，条码: {Barcode}", barcode);

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
    /// 更新物流信息
    /// Update logistics information
    /// </summary>
    public async Task<ThirdPartyResponse> UpdateLogisticsAsync(
        string barcode,
        string logisticsCompany,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "聚水潭ERP - 开始更新物流，条码: {Barcode}, 物流公司: {Company}, 物流单号: {Tracking}",
                barcode, logisticsCompany, trackingNumber);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var bizContent = new
            {
                so_id = barcode,
                lc_id = logisticsCompany,
                l_id = trackingNumber,
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
                    "聚水潭ERP - 更新物流成功，条码: {Barcode}, 物流单号: {Tracking}",
                    barcode, trackingNumber);

                return new ThirdPartyResponse
                {
                    Success = true,
                    Code = "200",
                    Message = "更新物流成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "聚水潭ERP - 更新物流失败，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new ThirdPartyResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"更新物流失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚水潭ERP - 更新物流异常，条码: {Barcode}", barcode);

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
