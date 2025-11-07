using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;

/// <summary>
/// 旺店通WMS API客户端实现
/// WDT (Wang Dian Tong) WMS API client implementation
/// 参考: https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e
/// </summary>
public class WdtWmsApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WdtWmsApiClient> _logger;
    public WdtWmsApiParameters Parameters { get; set; }

    public WdtWmsApiClient(
        HttpClient httpClient,
        ILogger<WdtWmsApiClient> logger,
        string appKey = "",
        string appSecret = "",
        string sid = "")
    {
        _httpClient = httpClient;
        _logger = logger;
        Parameters = new WdtWmsApiParameters
        {
            AppKey = appKey,
            AppSecret = appSecret,
            Sid = sid
        };
    }

    /// <summary>
    /// 扫描包裹（旺店通WMS不支持此功能）
    /// Scan parcel - Not supported in WDT WMS
    /// 注意：根据要求，WdtWmsApiClient不应该实现ScanParcelAsync
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通WMS不支持扫描包裹功能，条码: {Barcode}", barcode);
        
        await Task.CompletedTask;
        
        return new WcsApiResponse
        {
            Success = true,
            Code = ApiConstants.HttpStatusCodes.Success,
            Message = "旺店通WMS不支持扫描包裹功能",
            Data = "{\"info\":\"Feature not supported\"}"
        };
    }

    /// <summary>
    /// 请求格口（上传称重数据）
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
            _logger.LogDebug("WDT WMS - 开始请求格口/上传数据，条码: {Barcode}", barcode);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            var data = new
            {
                logistics_no = barcode,
                weight = Parameters.DefaultWeight, // 使用配置的默认重量
                is_weight = "Y",
                package_barcode = string.Empty
            };

            var requestData = new Dictionary<string, object>
            {
                { "appkey", Parameters.AppKey },
                { "format", "json" },
                { "method", Parameters.Method },
                { "sid", Parameters.Sid },
                { "sign_method", "md5" },
                { "timestamp", timestamp }
            };

            var sign = GenerateSign(requestData, JsonConvert.SerializeObject(data), Parameters.AppSecret);
            requestData.Add("sign", sign);

            var param = string.Join("&", requestData.OrderBy(o => o.Key).Select(s => $"{s.Key}={s.Value}"));

            _httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);

            // 判断是否必须包含包装条码
            if (Parameters.MustIncludeBoxBarcode && string.IsNullOrEmpty(data.package_barcode))
            {
                stopwatch.Stop();
                _logger.LogWarning("WDT WMS - 包装码不能为空，条码: {Barcode}", barcode);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ApiConstants.HttpStatusCodes.Error,
                    Message = "包装码不能为空",
                    Data = string.Empty
                };
            }

            using var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));
            using HttpContent content = new StreamContent(dataStream);
            content.Headers.Add("Content-Type", "text/xml");

            var response = await _httpClient.PostAsync($"{Parameters.Url}?{param}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseContent = Regex.Unescape(responseContent);

            bool isSuccess = false;
            string? exceptionMsg = null;

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var jObject = JObject.Parse(responseContent);
                if (jObject["flag"]?.ToString()?.ToLower()?.Equals("success") == true)
                {
                    isSuccess = true;
                }
                else
                {
                    exceptionMsg = jObject["message"]?.ToString();
                }
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation(
                    "WDT WMS - 请求格口成功，条码: {Barcode}, 耗时: {Duration}ms",
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
                    "WDT WMS - 请求格口失败，条码: {Barcode}, 状态码: {StatusCode}, 错误: {Error}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, exceptionMsg, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = exceptionMsg ?? $"请求格口失败: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "WDT WMS - HTTP请求异常，条码: {Barcode}, 耗时: {Duration}ms", 
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
            _logger.LogError(ex, "WDT WMS - 请求超时，条码: {Barcode}, 耗时: {Duration}ms", 
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
            _logger.LogError(ex, "WDT WMS - 请求格口异常，条码: {Barcode}, 耗时: {Duration}ms", 
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

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 对于文件上传，WDT通常使用multipart/form-data
            using var formContent = new MultipartFormDataContent();
            
            formContent.Add(new StringContent(Parameters.AppKey), "appkey");
            formContent.Add(new StringContent("json"), "format");
            formContent.Add(new StringContent(Parameters.Method), "method");
            formContent.Add(new StringContent(Parameters.Sid), "sid");
            formContent.Add(new StringContent("md5"), "sign_method");
            formContent.Add(new StringContent(timestamp), "timestamp");
            formContent.Add(new StringContent(barcode), "barcode");
            
            var requestData = new Dictionary<string, object>
            {
                { "appkey", Parameters.AppKey },
                { "format", "json" },
                { "method", Parameters.Method },
                { "sid", Parameters.Sid },
                { "sign_method", "md5" },
                { "timestamp", timestamp },
                { "barcode", barcode }
            };
            
            var sign = GenerateSign(requestData, string.Empty, Parameters.AppSecret);
            formContent.Add(new StringContent(sign), "sign");
            
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(imageContent, "image", $"{barcode}.jpg");

            var response = await _httpClient.PostAsync(Parameters.Url, formContent, cancellationToken);
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
    /// 参考代码的签名算法: appSecret + key1value1key2value2... + body + appSecret 然后MD5
    /// </summary>
    private static string GenerateSign(Dictionary<string, object> parameters, string body, string appSecret)
    {
        if (string.IsNullOrEmpty(appSecret))
        {
            return string.Empty;
        }

        var pairs = parameters.OrderBy(o => o.Key);
        var signString = appSecret +
                         string.Join("", pairs.Select(s => s.Key + s.Value)) +
                         body +
                         appSecret;

        // 转MD5
        using var md5 = MD5.Create();
        var result = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
        var strResult = BitConverter.ToString(result);
        return strResult.Replace("-", "");
    }
}
