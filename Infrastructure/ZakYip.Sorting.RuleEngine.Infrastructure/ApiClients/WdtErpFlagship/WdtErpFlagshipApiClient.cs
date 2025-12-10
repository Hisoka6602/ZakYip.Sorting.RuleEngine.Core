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
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;

/// <summary>
/// 旺店通ERP旗舰版 API客户端实现
/// WDT (Wang Dian Tong) ERP Flagship API client implementation
/// 参考: https://gist.github.com/Hisoka6602/7d6a8ab67247306ae51ebe7a865cdaee
/// </summary>
public class WdtErpFlagshipApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WdtErpFlagshipApiClient> _logger;
    public WdtErpFlagshipApiParameters Parameters { get; set; }

    public WdtErpFlagshipApiClient(
        HttpClient httpClient,
        ILogger<WdtErpFlagshipApiClient> logger,
        string key = "",
        string appsecret = "",
        string sid = "")
    {
        _httpClient = httpClient;
        _logger = logger;
        Parameters = new WdtErpFlagshipApiParameters
        {
            Key = key,
            Appsecret = appsecret,
            Sid = sid
        };
    }

    /// <summary>
    /// 扫描包裹（旺店通ERP旗舰版不支持此功能）
    /// Scan parcel - Not supported in WDT ERP Flagship
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var requestTime = DateTime.Now;
        
        _logger.LogWarning("旺店通ERP旗舰版不支持扫描包裹功能，条码: {Barcode}", barcode);
        
        await Task.CompletedTask;
        stopwatch.Stop();
        
        return new WcsApiResponse
        {
            Success = true,
            Code = ApiConstants.HttpStatusCodes.Success,
            Message = "旺店通ERP旗舰版不支持扫描包裹功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestUrl = "N/A",
            RequestBody = "N/A",
            RequestHeaders = "{}",
            RequestTime = requestTime,
            ResponseTime = DateTime.Now,
            DurationMs = stopwatch.ElapsedMilliseconds,
            FormattedCurl = "# Feature not supported by WDT ERP Flagship"
        };
    }

    /// <summary>
    /// 请求格口（上传称重数据）
    /// Request a chute/gate number for the parcel
    /// 对应参考代码中的 UploadData 方法
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var requestTime = DateTime.Now;

        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            _logger.LogDebug("WDT ERP Flagship - 开始请求格口/上传数据，包裹ID: {ParcelId}, 条码: {Barcode}", parcelId, dwsData.Barcode);

            // 重量保留3位小数
            var roundedWeight = Math.Round(Convert.ToDecimal(dwsData.Weight), 3);
            
            // 根据不同的Method构建不同的请求体
            object[] requestBody = Parameters.Method switch
            {
                "wms.stockout.Sales.weighingExt" => new object[]
                {
                    dwsData.Barcode,
                    string.Empty,
                    roundedWeight,
                    Parameters.PackagerId,
                    Parameters.Force
                },
                "wms.stockout.Sales.onceWeighing" => new object[]
                {
                    dwsData.Barcode,
                    string.Empty,
                    roundedWeight,
                    Parameters.PackagerId,
                    Parameters.OperateTableName,
                    Parameters.Force
                },
                "wms.stockout.Sales.onceWeighingByNo" => new object[]
                {
                    dwsData.Barcode,
                    string.Empty,
                    roundedWeight,
                    Parameters.PackagerNo,
                    Parameters.OperateTableName,
                    Parameters.Force
                },
                _ => Array.Empty<object>()
            };

            // WDT使用Unix时间戳减去1325347200 (2012-01-01的时间戳)
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds() - 1325347200;

            // 构建参数字典（不包含body和sign）
            var dictionary = new Dictionary<string, object>
            {
                {"body", JsonConvert.SerializeObject(requestBody)},
                {"key", Parameters.Key},
                {"sid", Parameters.Sid},
                {"method", Parameters.Method},
                {"v", Parameters.V},
                {"salt", Parameters.Salt},
                {"timestamp", timestamp},
            };

            // 生成签名: appsecret + key1value1key2value2... + appsecret，然后MD5
            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = Parameters.Appsecret + 
                           string.Join("", pairs.Select(s => s.Key + s.Value)) + 
                           Parameters.Appsecret;

            string sign;
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
                var strResult = BitConverter.ToString(result);
                sign = strResult.Replace("-", "", StringComparison.Ordinal).ToLower();
            }

            // 将sign加入字典，移除body（body通过POST传递）
            dictionary.Add("sign", sign);
            dictionary.Remove("body");

            // 拼接URL参数
            var param = string.Join("&", dictionary.OrderBy(o => o.Key).Select(s => $"{s.Key}={s.Value}"));
            var requestUrl = $"{Parameters.Url}?{param}";

            _httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);

            // 准备请求体
            var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody)));
            HttpContent content = new StreamContent(dataStream);
            content.Headers.Add("Content-Type", "application/json");

            // 生成请求信息
            var reqHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            formattedCurl = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                requestUrl,
                reqHeaders,
                JsonConvert.SerializeObject(requestBody));
            requestHeaders = ApiRequestHelper.FormatHeaders(reqHeaders);

            response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseContent = Regex.Unescape(responseContent);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            bool isSuccess = false;
            string? exceptionMsg = null;

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    var jObject = JObject.Parse(responseContent);
                    // 检查status字段，值为"0"表示成功
                    if (jObject["status"]?.ToString()?.ToUpper()?.Equals("0", StringComparison.Ordinal) == true)
                    {
                        isSuccess = true;
                    }
                    else
                    {
                        exceptionMsg = jObject["message"]?.ToString() ?? jObject["msg"]?.ToString();
                    }
                }
                catch (JsonException)
                {
                    exceptionMsg = "报文解析异常";
                }
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation(
                    "WDT ERP Flagship - 上传称重数据成功，包裹ID: {ParcelId}, 条码: {Barcode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ApiConstants.HttpStatusCodes.Success,
                    Message = "上传称重数据成功",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestUrl = requestUrl,
                    RequestBody = JsonConvert.SerializeObject(requestBody),
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = formattedCurl
                };
            }
            else
            {
                _logger.LogWarning(
                    "WDT ERP Flagship - 请求格口失败，包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 错误: {Error}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, exceptionMsg, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = exceptionMsg ?? $"请求格口失败: {response.StatusCode}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ErrorMessage = exceptionMsg ?? $"请求格口失败: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestUrl = requestUrl,
                    RequestBody = JsonConvert.SerializeObject(requestBody),
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = formattedCurl
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "WDT ERP Flagship - HTTP请求异常，包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = "接口访问异常",
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestUrl = Parameters.Url,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now,
                ResponseStatusCode = response != null ? (int)response.StatusCode : null,
                ResponseHeaders = responseHeaders,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = formattedCurl
            };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "WDT ERP Flagship - 请求超时，包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = "接口访问返回超时",
                Data = ex.ToString(),
                ErrorMessage = "接口访问返回超时",
                ParcelId = parcelId,
                RequestUrl = Parameters.Url,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now,
                ResponseStatusCode = response != null ? (int)response.StatusCode : null,
                ResponseHeaders = responseHeaders,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = formattedCurl
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "WDT ERP Flagship - 请求格口异常，包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestUrl = Parameters.Url,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now,
                ResponseStatusCode = response != null ? (int)response.StatusCode : null,
                ResponseHeaders = responseHeaders,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = formattedCurl
            };
        }
    }

    /// <summary>
    /// 上传图片（旺店通ERP旗舰版不支持此功能）
    /// Upload image - Not supported in WDT ERP Flagship
    /// </summary>
    public async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = "image/jpeg",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var requestTime = DateTime.Now;
        
        _logger.LogWarning("旺店通ERP旗舰版不支持上传图片功能，条码: {Barcode}", barcode);
        
        await Task.CompletedTask;
        stopwatch.Stop();
        
        return new WcsApiResponse
        {
            Success = true,
            Code = ApiConstants.HttpStatusCodes.Success,
            Message = "旺店通ERP旗舰版不支持上传图片功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestUrl = "N/A",
            RequestBody = "N/A",
            RequestHeaders = "{}",
            RequestTime = requestTime,
            ResponseTime = DateTime.Now,
            DurationMs = stopwatch.ElapsedMilliseconds,
            FormattedCurl = "# Feature not supported by WDT ERP Flagship"
        };
    }
}
