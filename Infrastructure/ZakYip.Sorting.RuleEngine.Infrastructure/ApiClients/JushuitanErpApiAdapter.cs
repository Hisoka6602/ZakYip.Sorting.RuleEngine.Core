using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 聚水潭ERP API适配器实现
/// Jushuituan ERP API adapter implementation
/// 参考: JushuitanErpApi from reference code
/// </summary>
public class JushuitanErpApiAdapter : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JushuitanErpApiAdapter> _logger;
    private readonly JushuitanErpApiParameters _parameters;

    public JushuitanErpApiAdapter(
        HttpClient httpClient,
        ILogger<JushuitanErpApiAdapter> logger,
        string appKey = "",
        string appSecret = "",
        string accessToken = "")
    {
        _httpClient = httpClient;
        _logger = logger;
        _parameters = new JushuitanErpApiParameters
        {
            AppKey = appKey,
            AppSecret = appSecret,
            AccessToken = accessToken
        };
    }

    /// <summary>
    /// 扫描包裹（提交扫描信息）
    /// Scan parcel to register it in the system
    /// 对应参考代码中的 SubmitScanInfo 方法
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("聚水潭ERP - 开始扫描包裹，条码: {Barcode}", barcode);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var bizContent = new
            {
                so_id = barcode,
                page_index = 1,
                page_size = 1
            };

            var bizContentJson = JsonConvert.SerializeObject(bizContent);

            var requestData = new Dictionary<string, string>
            {
                { "app_key", _parameters.AppKey },
                { "access_token", _parameters.AccessToken },
                { "timestamp", timestamp },
                { "charset", "utf-8" },
                { "version", _parameters.Version.ToString() },
                { "biz", bizContentJson }
            };

            var sign = GenerateSign(requestData, _parameters.AppSecret);
            requestData.Add("sign", sign);

            var content = new FormUrlEncodedContent(requestData);

            var response = await _httpClient.PostAsync(_parameters.Url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "聚水潭ERP - 扫描包裹成功，条码: {Barcode}",
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
                    "聚水潭ERP - 扫描包裹失败，条码: {Barcode}, 状态码: {StatusCode}",
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
            _logger.LogError(ex, "聚水潭ERP - 扫描包裹异常，条码: {Barcode}", barcode);

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
    /// 请求格口（上传数据）
    /// Request a chute/gate number for the parcel
    /// 对应参考代码中的 UploadData 方法
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        try
        {
            _logger.LogDebug(
                "聚水潭ERP - 开始请求格口/上传数据，条码: {Barcode}",
                barcode);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // 构造业务参数
            var biz = new[]
            {
                new
                {
                    l_id = barcode,
                    type = _parameters.Type,
                    is_un_lid = _parameters.IsUnLid,
                    channel = _parameters.Channel,
                    weight = _parameters.IsUploadWeight ? -1 : -1
                }
            };

            var bizJson = Newtonsoft.Json.JsonConvert.SerializeObject(biz);

            var requestData = new Dictionary<string, string>
            {
                { "app_key", _parameters.AppKey },
                { "access_token", _parameters.AccessToken },
                { "biz", bizJson },
                { "timestamp", timestamp },
                { "charset", "utf-8" },
                { "version", _parameters.Version.ToString() }
            };

            var sign = GenerateSign(requestData, _parameters.AppSecret);
            requestData.Add("sign", sign);

            _httpClient.Timeout = TimeSpan.FromMilliseconds(_parameters.TimeOut);
            var content = new FormUrlEncodedContent(requestData);

            var response = await _httpClient.PostAsync(_parameters.Url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            bool isSuccess = false;
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var jObject = JObject.Parse(responseContent);
                // 确保 data 存在，并且 data.datas 是一个非空数组
                if (jObject["data"]?["datas"] is JArray { Count: > 0 } jArray)
                {
                    isSuccess = jArray[0]["is_success"]?.Value<bool>() == true;
                }
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation(
                    "聚水潭ERP - 请求格口成功，条码: {Barcode}, 耗时: {Duration}ms",
                    barcode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ApiConstants.HttpStatusCodes.Success,
                    Message = "请求格口成功",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "聚水潭ERP - 请求格口失败，条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"请求格口失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "聚水潭ERP - HTTP请求异常，条码: {Barcode}, 耗时: {Duration}ms", 
                barcode, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "聚水潭ERP - 请求超时，条码: {Barcode}, 耗时: {Duration}ms", 
                barcode, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = "接口访问返回超时",
                Data = ex.ToString()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "聚水潭ERP - 请求格口异常，条码: {Barcode}, 耗时: {Duration}ms", 
                barcode, stopwatch.ElapsedMilliseconds);

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
                Code = ApiConstants.HttpStatusCodes.Success,
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
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 生成签名
    /// Generate signature for API authentication
    /// 参考代码的签名算法: appSecret + key1value1key2value2... 然后MD5
    /// </summary>
    private static string GenerateSign(Dictionary<string, string> parameters, string appSecret)
    {
        if (string.IsNullOrEmpty(appSecret))
        {
            return string.Empty;
        }

        // 1. 按键名字典序排序
        var sortedKeys = parameters.Keys
            .Where(k => k != "sign")
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        // 2. 按 key+value 拼接
        var paramStr = new StringBuilder();
        foreach (var key in sortedKeys)
        {
            paramStr.Append(key).Append(parameters[key]);
        }

        // 3. 拼接 appSecret 在前
        var signStr = appSecret + paramStr.ToString();

        // 4. 计算 MD5 并转小写
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2")); // x2 = 小写
        }
        return sb.ToString();
    }
}
