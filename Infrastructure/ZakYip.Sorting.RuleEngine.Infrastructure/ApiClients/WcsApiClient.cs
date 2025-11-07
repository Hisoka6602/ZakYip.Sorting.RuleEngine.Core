using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// WCS API客户端实现
/// WCS API client implementation
/// </summary>
public class WcsApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WcsApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WcsApiClient(
        HttpClient httpClient,
        ILogger<WcsApiClient> logger)
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
    /// 扫描包裹
    /// Scan parcel to register it in the wcs system
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        var requestUrl = ApiConstants.WcsEndpoints.ParcelScan;
        
        // 构造请求数据
        var requestData = new
        {
            barcode,
            scanTime = DateTime.Now
        };
        var json = JsonSerializer.Serialize(requestData, _jsonOptions);
        
        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            _logger.LogDebug("开始扫描包裹，条码: {Barcode}", barcode);

            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 生成请求信息
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = content
            };
            
            formattedCurl = await ApiRequestHelper.GenerateFormattedCurlFromRequestAsync(request);
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

            // 发送POST请求
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "扫描包裹成功，条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Parcel scanned successfully",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = barcode,
                    RequestUrl = requestUrl,
                    RequestBody = json,
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
                    "扫描包裹失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Scan Error: {response.StatusCode}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Scan Error: {response.StatusCode}",
                    ParcelId = barcode,
                    RequestUrl = requestUrl,
                    RequestBody = json,
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
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "扫描包裹异常，条码: {Barcode}, 耗时: {Duration}ms", barcode, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = barcode,
                RequestUrl = requestUrl,
                RequestBody = json,
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
    /// 请求格口
    /// Request a chute/gate number for the parcel
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        var requestUrl = ApiConstants.WcsEndpoints.ChuteRequest;
        
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
            ocrData = ocrData != null ? new
            {
                threeSegmentCode = ocrData.ThreeSegmentCode,
                firstSegmentCode = ocrData.FirstSegmentCode,
                secondSegmentCode = ocrData.SecondSegmentCode,
                thirdSegmentCode = ocrData.ThirdSegmentCode,
                recipientAddress = ocrData.RecipientAddress
            } : null,
            requestTime = DateTime.Now
        };
        
        var json = JsonSerializer.Serialize(requestData, _jsonOptions);
        
        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            _logger.LogDebug("开始请求格口，包裹ID: {ParcelId}, 条码: {Barcode}", parcelId, dwsData.Barcode);

            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 生成请求信息
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = content
            };
            
            formattedCurl = await ApiRequestHelper.GenerateFormattedCurlFromRequestAsync(request);
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

            // 发送POST请求
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);
            
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "请求格口成功，包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Chute requested successfully",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestUrl = requestUrl,
                    RequestBody = json,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FormattedCurl = formattedCurl,
                    OcrData = ocrData
                };
            }
            else
            {
                _logger.LogWarning(
                    "请求格口失败，包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
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
                    RequestUrl = requestUrl,
                    RequestBody = json,
                    RequestHeaders = requestHeaders,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
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
            _logger.LogError(ex, "请求格口异常，包裹ID: {ParcelId}, 耗时: {Duration}ms", parcelId, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestUrl = requestUrl,
                RequestBody = json,
                RequestHeaders = requestHeaders,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now,
                ResponseStatusCode = response != null ? (int)response.StatusCode : null,
                ResponseHeaders = responseHeaders,
                DurationMs = stopwatch.ElapsedMilliseconds,
                FormattedCurl = formattedCurl,
                OcrData = ocrData
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
        string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        var requestUrl = ApiConstants.WcsEndpoints.ImageUpload;
        
        HttpResponseMessage? response = null;
        string? responseContent = null;
        string? formattedCurl = null;
        string? requestHeaders = null;
        string? responseHeaders = null;
        
        try
        {
            _logger.LogDebug("开始上传图片，条码: {Barcode}, 图片大小: {Size} bytes, 类型: {ContentType}", 
                barcode, imageData.Length, contentType);

            // 构造multipart/form-data请求
            using var formContent = new MultipartFormDataContent();
            
            // 添加条码字段
            formContent.Add(new StringContent(barcode), "barcode");
            
            // 根据内容类型确定文件扩展名
            var extension = contentType switch
            {
                ApiConstants.ContentTypes.ImageJpeg => ".jpg",
                ApiConstants.ContentTypes.ImagePng => ".png",
                ApiConstants.ContentTypes.ImageGif => ".gif",
                ApiConstants.ContentTypes.ImageBmp => ".bmp",
                ApiConstants.ContentTypes.ImageWebp => ".webp",
                _ => ".bin"
            };
            
            // 添加图片文件
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(imageContent, "image", $"{barcode}{extension}");

            // 生成请求信息
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = formContent
            };
            
            // 注意：对于multipart/form-data，我们简化FormattedCurl（因为包含二进制数据）
            var boundaryParam = formContent.Headers.ContentType?.Parameters.FirstOrDefault(p => p.Name == "boundary");
            var boundary = boundaryParam?.Value ?? "----WebKitFormBoundary";
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = $"multipart/form-data; boundary={boundary}"
            };
            formattedCurl = ApiRequestHelper.GenerateFormattedCurl(
                "POST", 
                requestUrl, 
                headers, 
                $"[multipart form data: barcode={barcode}, image size={imageData.Length} bytes]");
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

            // 发送POST请求
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseHeaders = ApiRequestHelper.GetFormattedHeadersFromResponse(response);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传图片成功，条码: {Barcode}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Image uploaded successfully",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = barcode,
                    RequestUrl = requestUrl,
                    RequestBody = $"[multipart form data: barcode={barcode}, image size={imageData.Length} bytes]",
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
                    "上传图片失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Image Upload Error: {response.StatusCode}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Image Upload Error: {response.StatusCode}",
                    ParcelId = barcode,
                    RequestUrl = requestUrl,
                    RequestBody = $"[multipart form data: barcode={barcode}, image size={imageData.Length} bytes]",
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
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "上传图片异常，条码: {Barcode}, 耗时: {Duration}ms", barcode, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = barcode,
                RequestUrl = requestUrl,
                RequestBody = $"[multipart form data: barcode={barcode}, image size={imageData.Length} bytes]",
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
}
