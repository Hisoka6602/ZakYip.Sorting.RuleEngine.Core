using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostProcessingCenter;

/// <summary>
/// 邮政处理中心API客户端实现
/// Postal Processing Center API client implementation
/// 参考: https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e (PostApi)
/// </summary>
public class PostProcessingCenterApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostProcessingCenterApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostProcessingCenterApiClient(
        HttpClient httpClient,
        ILogger<PostProcessingCenterApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// 扫描包裹到邮政处理中心系统
    /// Scan parcel to register it in the postal processing center system
    /// 对应参考代码中的 SubmitScanInfo 方法
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始扫描包裹到邮政处理中心，条码: {Barcode}", barcode);

            // 构造请求数据
            var requestData = new
            {
                barcode,
                scanTime = DateTime.Now,
                version = ApiConstants.PostProcessingCenterApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求到邮政处理中心扫描端点
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.ScanUpload}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "扫描包裹成功（邮政处理中心），条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Parcel scanned successfully at processing center",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "扫描包裹失败（邮政处理中心），条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Scan Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描包裹异常（邮政处理中心），条码: {Barcode}", barcode);

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
    /// 请求格口号（查询包裹路由信息并返回格口）
    /// Request a chute/gate number for the parcel (query routing and return chute)
    /// 对应参考代码中的 UploadData 方法
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        
        try
        {
            _logger.LogDebug("开始请求格口（邮政处理中心），包裹ID: {ParcelId}, 条码: {Barcode}", parcelId, dwsData.Barcode);

            // 先提交扫描信息（对应参考代码中UploadData内部调用SubmitScanInfo）
            await ScanParcelAsync(dwsData.Barcode, cancellationToken);

            // 构造请求数据 - 包含DWS数据
            var requestData = new
            {
                parcelId,
                barcode = dwsData.Barcode,
                weight = dwsData.Weight,
                length = dwsData.Length,
                width = dwsData.Width,
                height = dwsData.Height,
                volume = dwsData.Volume,
                scanTime = dwsData.ScannedAt,
                // 如果有OCR数据，也包含进去
                ocrData = ocrData != null ? new
                {
                    threeSegmentCode = ocrData.ThreeSegmentCode,
                    firstSegmentCode = ocrData.FirstSegmentCode,
                    secondSegmentCode = ocrData.SecondSegmentCode,
                    thirdSegmentCode = ocrData.ThirdSegmentCode,
                    recipientAddress = ocrData.RecipientAddress
                } : null,
                requestTime = DateTime.Now,
                version = ApiConstants.PostProcessingCenterApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求查询路由
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.RoutingQuery}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "请求格口成功（邮政处理中心），包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Chute requested successfully from processing center",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestUrl = endpoint,
                    RequestBody = json,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    OcrData = ocrData
                };
            }
            else
            {
                _logger.LogWarning(
                    "请求格口失败（邮政处理中心），包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Chute Request Error: {response.StatusCode}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Chute Request Error: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestUrl = endpoint,
                    RequestBody = json,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    OcrData = ocrData
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "请求格口异常（邮政处理中心），包裹ID: {ParcelId}, 耗时: {Duration}ms", parcelId, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now,
                DurationMs = stopwatch.ElapsedMilliseconds,
                OcrData = ocrData
            };
        }
    }

    /// <summary>
    /// 上传图片到邮政处理中心
    /// Upload image to postal processing center
    /// 注意：根据要求，如果不存在或未实现可以留空
    /// </summary>
    public async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("上传图片功能（邮政处理中心）当前未实现，条码: {Barcode}", barcode);
        
        await Task.CompletedTask;

        return new WcsApiResponse
        {
            Success = true,
            Code = ApiConstants.HttpStatusCodes.Success,
            Message = "邮政处理中心图片上传功能未实现",
            Data = "{\"info\":\"Feature not implemented\"}",
            ParcelId = barcode,
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 0
        };
    }
}
