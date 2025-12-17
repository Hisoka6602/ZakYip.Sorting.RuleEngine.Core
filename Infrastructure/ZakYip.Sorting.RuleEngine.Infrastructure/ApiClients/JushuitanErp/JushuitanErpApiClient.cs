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
/// 直接实现IWcsApiAdapter接口，无基类继承
/// Directly implements IWcsApiAdapter interface, no base class
/// </summary>
public class JushuitanErpApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JushuitanErpApiClient> _logger;
    private readonly ISystemClock _clock;
    
    public JushuitanErpApiParameters Parameters { get; set; }

    public JushuitanErpApiClient(
        HttpClient httpClient,
        ILogger<JushuitanErpApiClient> logger,
        ISystemClock clock,
        string appKey = "",
        string appSecret = "",
        string accessToken = "")
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        Parameters = new JushuitanErpApiParameters
        {
            AppKey = appKey,
            AppSecret = appSecret,
            AccessToken = accessToken
        };
    }

    /// <summary>
    /// 扫描包裹 - 聚水潭ERP不支持此功能
    /// Scan parcel - Not supported by Jushuituan ERP
    /// </summary>
    public Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("聚水潭ERP不支持扫描包裹功能，条码: {Barcode}", barcode);
        
        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = "聚水潭ERP不支持扫描包裹功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 0
        });
    }

    /// <summary>
    /// 请求格口（上传数据）
    /// Request chute (upload data)
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = _clock.LocalNow;

        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            _logger.LogDebug("聚水潭ERP - 开始请求格口，包裹ID: {ParcelId}, 条码: {Barcode}",
                parcelId, dwsData.Barcode);

            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

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

            var bizJson = JsonConvert.SerializeObject(biz);

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

            _httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
            var content = new FormUrlEncodedContent(requestData);

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            };
            formattedCurl = ApiRequestHelper.GenerateFormattedCurl("POST", Parameters.Url, headers, bizJson);
            requestHeaders = ApiRequestHelper.FormatHeaders(headers);

            response = await _httpClient.PostAsync(Parameters.Url, content, cancellationToken).ConfigureAwait(false);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            bool isSuccess = false;
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var jObject = JObject.Parse(responseContent);
                if (jObject["data"]?["datas"] is JArray { Count: > 0 } jArray)
                {
                    isSuccess = jArray[0]["is_success"]?.Value<bool>() == true;
                }
                else if (jObject["code"]?.Value<int>() == 0 || jObject["data"]?["result"]?.Value<bool>() == true)
                {
                    isSuccess = true;
                }
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation("聚水潭ERP - 请求格口成功，包裹ID: {ParcelId}, 耗时: {Duration}ms",
                    parcelId, stopwatch.ElapsedMilliseconds);

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
                    FormattedCurl = formattedCurl,
                    OcrData = ocrData
                };
            }
            else
            {
                _logger.LogWarning("聚水潭ERP - 请求格口失败，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                    parcelId, response.StatusCode);

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
                    FormattedCurl = formattedCurl,
                    OcrData = ocrData
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "聚水潭ERP - 请求格口异常，包裹ID: {ParcelId}", parcelId);

            return new WcsApiResponse
            {
                Success = false,
                Code = HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestUrl = Parameters.Url,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = _clock.LocalNow,
                ResponseStatusCode = response != null ? (int)response.StatusCode : null,
                ResponseHeaders = responseHeaders,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = formattedCurl,
                OcrData = ocrData
            };
        }
    }

    /// <summary>
    /// 上传图片 - 聚水潭ERP不支持此功能
    /// Upload image - Not supported by Jushuituan ERP
    /// </summary>
    public Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("聚水潭ERP不支持上传图片功能，条码: {Barcode}", barcode);
        
        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = "聚水潭ERP不支持上传图片功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 0
        });
    }

    /// <summary>
    /// 落格回调 - 聚水潭ERP不支持此功能
    /// Chute landing callback - Not supported by Jushuituan ERP
    /// </summary>
    public Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("聚水潭ERP不支持落格回调功能，包裹ID: {ParcelId}", parcelId);
        
        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = "聚水潭ERP不支持落格回调功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = parcelId,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 0
        });
    }

    /// <summary>
    /// 生成签名
    /// Generate signature for API authentication
    /// </summary>
    private static string GenerateSign(Dictionary<string, string> parameters, string appSecret)
    {
        if (string.IsNullOrEmpty(appSecret))
        {
            return string.Empty;
        }

        var sortedKeys = parameters.Keys
            .Where(k => k != "sign")
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        var paramStr = new StringBuilder();
        foreach (var key in sortedKeys)
        {
            paramStr.Append(key).Append(parameters[key]);
        }

        var signStr = appSecret + paramStr.ToString();

        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
