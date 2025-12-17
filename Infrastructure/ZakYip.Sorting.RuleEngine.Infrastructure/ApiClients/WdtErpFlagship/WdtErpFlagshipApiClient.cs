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

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;

/// <summary>
/// 旺店通ERP旗舰版 API客户端实现
/// WDT ERP Flagship API client implementation
/// 直接实现IWcsApiAdapter接口，无基类继承
/// Directly implements IWcsApiAdapter interface, no base class
/// </summary>
public class WdtErpFlagshipApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WdtErpFlagshipApiClient> _logger;
    private readonly ISystemClock _clock;
    
    public WdtErpFlagshipApiParameters Parameters { get; set; }

    public WdtErpFlagshipApiClient(
        HttpClient httpClient,
        ILogger<WdtErpFlagshipApiClient> logger,
        ISystemClock clock,
        string url = "",
        string key = "",
        string appsecret = "",
        string sid = "")
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        Parameters = new WdtErpFlagshipApiParameters
        {
            Url = url,
            Key = key,
            Appsecret = appsecret,
            Sid = sid
        };
    }

    /// <summary>
    /// 扫描包裹 - 旺店通ERP旗舰版不支持此功能
    /// Scan parcel - Not supported by WDT ERP Flagship
    /// </summary>
    public Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通ERP旗舰版不支持扫描包裹功能，条码: {Barcode}", barcode);
        
        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = "旺店通ERP旗舰版不支持扫描包裹功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 0
        });
    }

    /// <summary>
    /// 请求格口（扩展称重/一次称重）
    /// Request chute (weighing extended/once weighing)
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
            _logger.LogDebug("旺店通ERP旗舰版 - 开始请求格口，包裹ID: {ParcelId}, 条码: {Barcode}",
                parcelId, dwsData.Barcode);

            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

            // 根据Method构造不同的业务参数
            object bizData;
            if (Parameters.Method == "wms.stockout.Sales.weighingExt")
            {
                // 扩展称重
                bizData = new
                {
                    outer_no = dwsData.Barcode,
                    weight = (double)dwsData.Weight,
                    length = (double)dwsData.Length,
                    width = (double)dwsData.Width,
                    height = (double)dwsData.Height,
                    volume = (double)dwsData.Volume,
                    packager_id = Parameters.PackagerId
                };
            }
            else if (Parameters.Method == "wms.stockout.Sales.onceWeighing")
            {
                // 一次称重(按打包员ID)
                bizData = new
                {
                    outer_no = dwsData.Barcode,
                    weight = (double)dwsData.Weight,
                    packager_id = Parameters.PackagerId,
                    operate_table_name = Parameters.OperateTableName,
                    force = Parameters.Force
                };
            }
            else if (Parameters.Method == "wms.stockout.Sales.onceWeighingByNo")
            {
                // 一次称重(按打包员编号)
                bizData = new
                {
                    outer_no = dwsData.Barcode,
                    weight = (double)dwsData.Weight,
                    packager_no = Parameters.PackagerNo,
                    operate_table_name = Parameters.OperateTableName,
                    force = Parameters.Force
                };
            }
            else
            {
                throw new InvalidOperationException($"不支持的方法: {Parameters.Method}");
            }

            var bizJson = JsonConvert.SerializeObject(bizData);

            var requestData = new Dictionary<string, string>
            {
                { "method", Parameters.Method },
                { "sid", Parameters.Sid },
                { "key", Parameters.Key },
                { "timestamp", timestamp },
                { "v", Parameters.V },
                { "format", "json" },
                { "body", bizJson }
            };

            // 如果有salt，添加到请求中
            if (!string.IsNullOrEmpty(Parameters.Salt))
            {
                requestData.Add("salt", Parameters.Salt);
            }

            var sign = GenerateSign(requestData, Parameters.Appsecret);
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
                // 旺店通ERP旗舰版成功响应格式: { "status": 0, "data": {...} } 或 { "code": 0 }
                isSuccess = jObject["status"]?.Value<int>() == 0 || jObject["code"]?.Value<int>() == 0;
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation("旺店通ERP旗舰版 - 请求格口成功，包裹ID: {ParcelId}, 耗时: {Duration}ms",
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
                _logger.LogWarning("旺店通ERP旗舰版 - 请求格口失败，包裹ID: {ParcelId}, 状态码: {StatusCode}",
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
            _logger.LogError(ex, "旺店通ERP旗舰版 - 请求格口异常，包裹ID: {ParcelId}", parcelId);

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
    /// 上传图片 - 旺店通ERP旗舰版不支持此功能
    /// Upload image - Not supported by WDT ERP Flagship
    /// </summary>
    public Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通ERP旗舰版不支持上传图片功能，条码: {Barcode}", barcode);
        
        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = "旺店通ERP旗舰版不支持上传图片功能",
            Data = "{\"info\":\"Feature not supported\"}",
            ParcelId = barcode,
            RequestTime = _clock.LocalNow,
            ResponseTime = _clock.LocalNow,
            DurationMs = 0
        });
    }

    /// <summary>
    /// 落格回调 - 旺店通ERP旗舰版不支持此功能
    /// Chute landing callback - Not supported by WDT ERP Flagship
    /// </summary>
    public Task<WcsApiResponse> NotifyChuteLandingAsync(
        string parcelId,
        string chuteId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("旺店通ERP旗舰版不支持落格回调功能，包裹ID: {ParcelId}", parcelId);
        
        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = HttpStatusCodes.Success,
            Message = "旺店通ERP旗舰版不支持落格回调功能",
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

        var signStr = appSecret + paramStr + appSecret;

        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("X2")); // X2 = 大写
        }
        return sb.ToString();
    }
}
