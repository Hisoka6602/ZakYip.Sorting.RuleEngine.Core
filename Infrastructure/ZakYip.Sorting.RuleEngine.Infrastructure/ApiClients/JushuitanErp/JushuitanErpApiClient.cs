using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;

/// <summary>
/// 聚水潭ERP API客户端实现
/// Jushuituan ERP API client implementation
/// 参考: https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e
/// </summary>
public class JushuitanErpApiClient : BaseErpApiClient
{
    public JushuitanErpApiParameters Parameters { get; set; }

    protected override string ClientTypeName => "聚水潭ERP";
    protected override string FeatureNotSupportedText => "Jushuituan ERP";

    public JushuitanErpApiClient(
        HttpClient httpClient,
        ILogger<JushuitanErpApiClient> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock,
        string appKey = "",
        string appSecret = "",
        string accessToken = "")
        : base(httpClient, logger, clock)
    {
        Parameters = new JushuitanErpApiParameters
        {
            AppKey = appKey,
            AppSecret = appSecret,
            AccessToken = accessToken
        };
    }



    /// <summary>
    /// 请求格口（上传数据）
    /// Request a chute/gate number for the parcel
    /// 对应参考代码中的 UploadData 方法
    /// </summary>
    public override async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var requestTime = _clock.LocalNow;

        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            Logger.LogDebug(
                "聚水潭ERP - 开始请求格口/上传数据，包裹ID: {ParcelId}, 条码: {Barcode}",
                parcelId, dwsData.Barcode);

            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

            // 构造业务参数 (参考代码中的UploadData方法)
            // 使用DWS数据中的重量
            var biz = new[]
            {
                new
                {
                    l_id = dwsData.Barcode,
                    type = Parameters.Type,
                    is_un_lid = Parameters.IsUnLid,
                    channel = Parameters.Channel,
                    weight = Parameters.IsUploadWeight ? (double)dwsData.Weight : -1
                }
            };

            var bizJson = Newtonsoft.Json.JsonConvert.SerializeObject(biz);

            var requestData = new Dictionary<string, string>
            {
                { "app_key", Parameters.AppKey },
                { "access_token", Parameters.AccessToken },
                { "biz", bizJson },
                { "timestamp", timestamp },
                { "charset", "utf-8" },
                { "version", Parameters.Version.ToString() }
            };

            var sign = GenerateSign(requestData, Parameters.AppSecret);
            requestData.Add("sign", sign);

            HttpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
            var content = new FormUrlEncodedContent(requestData);

            // 生成请求信息
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            };
            formattedCurl = ApiRequestHelper.GenerateFormattedCurl(
                "POST",
                Parameters.Url,
                headers,
                bizJson);
            requestHeaders = ApiRequestHelper.FormatHeaders(headers);

            response = await HttpClient.PostAsync(Parameters.Url, content, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            bool isSuccess = false;
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var jObject = JObject.Parse(responseContent);
                // 检查多种可能的成功响应格式
                // 格式1: { "data": { "datas": [{"is_success": true}] } }
                if (jObject["data"]?["datas"] is JArray { Count: > 0 } jArray)
                {
                    isSuccess = jArray[0]["is_success"]?.Value<bool>() == true;
                }
                // 格式2: { "code": 0, "data": { "result": true } }
                else if (jObject["code"]?.Value<int>() == 0 || jObject["data"]?["result"]?.Value<bool>() == true)
                {
                    isSuccess = true;
                }
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                Logger.LogInformation(
                    "聚水潭ERP - 请求格口成功，包裹ID: {ParcelId}, 条码: {Barcode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = HttpStatusCodes.Success,
                    Message = "请求格口成功",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestUrl = Parameters.Url,
                    RequestBody = bizJson,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = formattedCurl
                };
            }
            else
            {
                Logger.LogWarning(
                    "聚水潭ERP - 请求格口失败，包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"请求格口失败: {response.StatusCode}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ErrorMessage = $"请求格口失败: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestUrl = Parameters.Url,
                    RequestBody = bizJson,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = _clock.LocalNow,
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
            Logger.LogError(ex, "聚水潭ERP - HTTP请求异常，包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

            return CreateHttpExceptionResponse(ex, parcelId, Parameters.Url, requestHeaders, 
                responseHeaders, response, requestTime, stopwatch.ElapsedMilliseconds, formattedCurl);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "聚水潭ERP - 请求超时，包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

            return CreateTimeoutResponse(ex, parcelId, Parameters.Url, requestHeaders, 
                responseHeaders, response, requestTime, stopwatch.ElapsedMilliseconds, formattedCurl);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "聚水潭ERP - 请求格口异常，包裹ID: {ParcelId}, 耗时: {Duration}ms", 
                parcelId, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(ex, parcelId, Parameters.Url, requestHeaders, 
                responseHeaders, response, requestTime, stopwatch.ElapsedMilliseconds, formattedCurl);
        }
    }

    /// <summary>
    /// 上传图片（聚水潭ERP暂不支持，返回成功响应）
    /// Upload image to wcs API
    /// </summary>
    public override async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = "image/jpeg",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var requestTime = _clock.LocalNow;
        
        try
        {
            Logger.LogDebug(
                "聚水潭ERP - 上传图片请求（当前版本不支持），条码: {Barcode}",
                barcode);

            // 聚水潭ERP当前版本不支持直接上传图片
            // 返回成功响应以保持接口一致性
            await Task.CompletedTask;
            stopwatch.Stop();

            return new WcsApiResponse
            {
                Success = true,
                Code = HttpStatusCodes.Success,
                Message = "聚水潭ERP暂不支持图片上传功能",
                Data = "{\"info\":\"Feature not supported\"}",
                ParcelId = barcode,
                RequestUrl = "N/A",
                RequestBody = $"[image upload request: size={imageData.Length} bytes]",
                RequestHeaders = "{}",
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = "# Feature not supported by Jushuituan ERP"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "聚水潭ERP - 上传图片异常，条码: {Barcode}", barcode);

            return CreateExceptionResponse(ex, barcode, "N/A", "{}", null, null, 
                requestTime, stopwatch.ElapsedMilliseconds, "# Feature not supported by Jushuituan ERP");
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
