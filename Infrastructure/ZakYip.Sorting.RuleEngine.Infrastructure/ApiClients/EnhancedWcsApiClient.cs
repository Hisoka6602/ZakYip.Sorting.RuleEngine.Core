using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 增强版WCS API客户端实现
/// Enhanced WCS API client with strongly-typed responses and batch operations
/// </summary>
public class EnhancedWcsApiClient : IEnhancedWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EnhancedWcsApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EnhancedWcsApiClient(
        HttpClient httpClient,
        ILogger<EnhancedWcsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region IWcsApiAdapter Implementation (Existing Methods)

    /// <inheritdoc />
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        var requestUrl = ApiConstants.WcsEndpoints.ParcelScan;
        
        var requestData = new { barcode, scanTime = DateTime.Now };
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

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
            
            formattedCurl = await ApiRequestHelper.GenerateFormattedCurlFromRequestAsync(request);
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

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

                return CreateErrorResponse(barcode, requestUrl, json, requestHeaders, responseHeaders, 
                    response, responseContent, requestTime, stopwatch, formattedCurl);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "扫描包裹异常，条码: {Barcode}, 耗时: {Duration}ms", barcode, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(barcode, requestUrl, json, requestHeaders, responseHeaders,
                response, ex, requestTime, stopwatch, formattedCurl);
        }
    }

    /// <inheritdoc />
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        var requestUrl = ApiConstants.WcsEndpoints.ChuteRequest;
        
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

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
            
            formattedCurl = await ApiRequestHelper.GenerateFormattedCurlFromRequestAsync(request);
            requestHeaders = ApiRequestHelper.GetFormattedHeadersFromRequest(request);

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

                return CreateErrorResponse(parcelId, requestUrl, json, requestHeaders, responseHeaders,
                    response, responseContent, requestTime, stopwatch, formattedCurl, ocrData);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "请求格口异常，包裹ID: {ParcelId}, 耗时: {Duration}ms", parcelId, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(parcelId, requestUrl, json, requestHeaders, responseHeaders,
                response, ex, requestTime, stopwatch, formattedCurl, ocrData);
        }
    }

    /// <inheritdoc />
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

            using var formContent = new MultipartFormDataContent();
            
            formContent.Add(new StringContent(barcode), "barcode");
            
            var extension = contentType switch
            {
                ApiConstants.ContentTypes.ImageJpeg => ".jpg",
                ApiConstants.ContentTypes.ImagePng => ".png",
                ApiConstants.ContentTypes.ImageGif => ".gif",
                ApiConstants.ContentTypes.ImageBmp => ".bmp",
                ApiConstants.ContentTypes.ImageWebp => ".webp",
                _ => ".bin"
            };
            
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(imageContent, "image", $"{barcode}{extension}");

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = formContent };
            
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

                return CreateErrorResponse(barcode, requestUrl, 
                    $"[multipart form data: barcode={barcode}, image size={imageData.Length} bytes]",
                    requestHeaders, responseHeaders, response, responseContent, requestTime, stopwatch, formattedCurl);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "上传图片异常，条码: {Barcode}, 耗时: {Duration}ms", barcode, stopwatch.ElapsedMilliseconds);

            return CreateExceptionResponse(barcode, requestUrl,
                $"[multipart form data: barcode={barcode}, image size={imageData.Length} bytes]",
                requestHeaders, responseHeaders, response, ex, requestTime, stopwatch, formattedCurl);
        }
    }

    #endregion

    #region Strongly-Typed Methods

    /// <inheritdoc />
    public async Task<StronglyTypedApiResponseDto<ScanParcelResponseData>> ScanParcelStronglyTypedAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var response = await ScanParcelAsync(barcode, cancellationToken);
        return ConvertToStronglyTyped<ScanParcelResponseData>(response, ParseScanParcelResponse);
    }

    /// <inheritdoc />
    public async Task<StronglyTypedApiResponseDto<ChuteRequestResponseData>> RequestChuteStronglyTypedAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var response = await RequestChuteAsync(parcelId, dwsData, ocrData, cancellationToken);
        return ConvertToStronglyTyped<ChuteRequestResponseData>(response, ParseChuteRequestResponse);
    }

    /// <inheritdoc />
    public async Task<StronglyTypedApiResponseDto<ImageUploadResponseData>> UploadImageStronglyTypedAsync(
        string barcode,
        byte[] imageData,
        string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        var response = await UploadImageAsync(barcode, imageData, contentType, cancellationToken);
        return ConvertToStronglyTyped<ImageUploadResponseData>(response, ParseImageUploadResponse);
    }

    #endregion

    #region Batch Operations

    /// <inheritdoc />
    public async Task<BatchOperationResponse<WcsApiResponse>> BatchRequestChuteAsync(
        BatchOperationRequest<(string ParcelId, DwsData DwsData, OcrData? OcrData)> requests,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new BatchOperationResponse<WcsApiResponse>
        {
            TotalCount = requests.Requests.Count
        };

        try
        {
            _logger.LogInformation("开始批量请求格口，总数: {Count}, 并行: {Parallel}", 
                requests.Requests.Count, requests.ProcessInParallel);

            if (requests.ProcessInParallel)
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = requests.MaxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                };

                var results = new System.Collections.Concurrent.ConcurrentBag<WcsApiResponse>();

                await Parallel.ForEachAsync(requests.Requests, options, async (req, ct) =>
                {
                    var result = await RequestChuteAsync(req.ParcelId, req.DwsData, req.OcrData, ct);
                    results.Add(result);
                });

                response.SuccessfulResponses = results.Where(r => r.Success).ToList();
                response.FailedResponses = results.Where(r => !r.Success).ToList();
            }
            else
            {
                foreach (var req in requests.Requests)
                {
                    var result = await RequestChuteAsync(req.ParcelId, req.DwsData, req.OcrData, cancellationToken);
                    if (result.Success)
                        response.SuccessfulResponses.Add(result);
                    else
                        response.FailedResponses.Add(result);
                }
            }

            response.SuccessCount = response.SuccessfulResponses.Count;
            response.FailedCount = response.FailedResponses.Count;
            stopwatch.Stop();
            response.TotalDurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "批量请求格口完成，成功: {Success}, 失败: {Failed}, 总耗时: {Duration}ms",
                response.SuccessCount, response.FailedCount, response.TotalDurationMs);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "批量请求格口异常");
            response.TotalDurationMs = stopwatch.ElapsedMilliseconds;
            return response;
        }
    }

    /// <inheritdoc />
    public async Task<BatchOperationResponse<WcsApiResponse>> BatchUploadImageAsync(
        BatchOperationRequest<(string Barcode, byte[] ImageData, string ContentType)> requests,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new BatchOperationResponse<WcsApiResponse>
        {
            TotalCount = requests.Requests.Count
        };

        try
        {
            _logger.LogInformation("开始批量上传图片，总数: {Count}, 并行: {Parallel}", 
                requests.Requests.Count, requests.ProcessInParallel);

            if (requests.ProcessInParallel)
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = requests.MaxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                };

                var results = new System.Collections.Concurrent.ConcurrentBag<WcsApiResponse>();

                await Parallel.ForEachAsync(requests.Requests, options, async (req, ct) =>
                {
                    var result = await UploadImageAsync(req.Barcode, req.ImageData, req.ContentType, ct);
                    results.Add(result);
                });

                response.SuccessfulResponses = results.Where(r => r.Success).ToList();
                response.FailedResponses = results.Where(r => !r.Success).ToList();
            }
            else
            {
                foreach (var req in requests.Requests)
                {
                    var result = await UploadImageAsync(req.Barcode, req.ImageData, req.ContentType, cancellationToken);
                    if (result.Success)
                        response.SuccessfulResponses.Add(result);
                    else
                        response.FailedResponses.Add(result);
                }
            }

            response.SuccessCount = response.SuccessfulResponses.Count;
            response.FailedCount = response.FailedResponses.Count;
            stopwatch.Stop();
            response.TotalDurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "批量上传图片完成，成功: {Success}, 失败: {Failed}, 总耗时: {Duration}ms",
                response.SuccessCount, response.FailedCount, response.TotalDurationMs);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "批量上传图片异常");
            response.TotalDurationMs = stopwatch.ElapsedMilliseconds;
            return response;
        }
    }

    #endregion

    #region Helper Methods

    private static StronglyTypedApiResponseDto<TData> ConvertToStronglyTyped<TData>(
        WcsApiResponse response,
        Func<string?, TData?> parser)
    {
        return new StronglyTypedApiResponseDto<TData>
        {
            Success = response.Success,
            Code = response.Code,
            Message = response.Message,
            Data = response.Success && !string.IsNullOrEmpty(response.Data) 
                ? parser(response.Data) 
                : default,
            ErrorMessage = response.ErrorMessage,
            RequestTime = response.RequestTime,
            ResponseTime = response.ResponseTime,
            DurationMs = response.DurationMs
        };
    }

    private static ScanParcelResponseData? ParseScanParcelResponse(string? responseData)
    {
        if (string.IsNullOrEmpty(responseData)) return null;

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseData);
            return new ScanParcelResponseData
            {
                ParcelId = json.TryGetProperty("parcelId", out var pid) ? pid.GetString() : null,
                Barcode = json.TryGetProperty("barcode", out var bc) ? bc.GetString() : null,
                ScanTime = json.TryGetProperty("scanTime", out var st) ? st.GetDateTime() : null,
                IsRegistered = json.TryGetProperty("isRegistered", out var ir) && ir.GetBoolean()
            };
        }
        catch
        {
            return null;
        }
    }

    private static ChuteRequestResponseData? ParseChuteRequestResponse(string? responseData)
    {
        if (string.IsNullOrEmpty(responseData)) return null;

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseData);
            return new ChuteRequestResponseData
            {
                ChuteNumber = json.TryGetProperty("chuteNumber", out var cn) ? cn.GetString() : null,
                ChuteName = json.TryGetProperty("chuteName", out var cname) ? cname.GetString() : null,
                ParcelId = json.TryGetProperty("parcelId", out var pid) ? pid.GetString() : null,
                Barcode = json.TryGetProperty("barcode", out var bc) ? bc.GetString() : null,
                AdditionalInfo = json.TryGetProperty("additionalInfo", out var ai)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(ai.GetRawText())
                    : null
            };
        }
        catch
        {
            return null;
        }
    }

    private static ImageUploadResponseData? ParseImageUploadResponse(string? responseData)
    {
        if (string.IsNullOrEmpty(responseData)) return null;

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseData);
            return new ImageUploadResponseData
            {
                ImageId = json.TryGetProperty("imageId", out var iid) ? iid.GetString() : null,
                ImageUrl = json.TryGetProperty("imageUrl", out var url) ? url.GetString() : null,
                UploadTime = json.TryGetProperty("uploadTime", out var ut) ? ut.GetDateTime() : null,
                FileSize = json.TryGetProperty("fileSize", out var fs) ? fs.GetInt64() : 0
            };
        }
        catch
        {
            return null;
        }
    }

    private static WcsApiResponse CreateErrorResponse(
        string parcelId, string requestUrl, string requestBody,
        string? requestHeaders, string? responseHeaders,
        HttpResponseMessage response, string? responseContent,
        DateTime requestTime, Stopwatch stopwatch, string? formattedCurl,
        OcrData? ocrData = null)
    {
        return new WcsApiResponse
        {
            Success = false,
            Code = ((int)response.StatusCode).ToString(),
            Message = $"Request Error: {response.StatusCode}",
            Data = responseContent,
            ResponseBody = responseContent,
            ErrorMessage = $"Request Error: {response.StatusCode}",
            ParcelId = parcelId,
            RequestUrl = requestUrl,
            RequestBody = requestBody,
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

    private static WcsApiResponse CreateExceptionResponse(
        string parcelId, string requestUrl, string requestBody,
        string? requestHeaders, string? responseHeaders,
        HttpResponseMessage? response, Exception ex,
        DateTime requestTime, Stopwatch stopwatch, string? formattedCurl,
        OcrData? ocrData = null)
    {
        return new WcsApiResponse
        {
            Success = false,
            Code = ApiConstants.HttpStatusCodes.Error,
            Message = ex.Message,
            Data = ex.ToString(),
            ErrorMessage = ex.Message,
            ParcelId = parcelId,
            RequestUrl = requestUrl,
            RequestBody = requestBody,
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

    #endregion
}
