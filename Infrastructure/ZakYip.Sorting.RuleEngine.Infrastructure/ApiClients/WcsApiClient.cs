using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

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
    /// 上传数据到WCS API
    /// Upload data to wcs API
    /// </summary>
    public async Task<WcsApiResponse> UploadDataAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始调用WCS API，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            // 构造请求数据
            // Build request data
            var requestData = new
            {
                parcelId = parcelInfo.ParcelId,
                cartNumber = parcelInfo.CartNumber,
                barcode = dwsData.Barcode,
                weight = dwsData.Weight,
                length = dwsData.Length,
                width = dwsData.Width,
                height = dwsData.Height,
                volume = dwsData.Volume,
                timestamp = DateTime.Now
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            // Send POST request
            var response = await _httpClient.PostAsync(ApiConstants.WcsEndpoints.SortingUpload, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WCS API调用成功，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                    parcelInfo.ParcelId, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Success",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "WCS API返回错误，包裹ID: {ParcelId}, 状态码: {StatusCode}, 响应: {Response}",
                    parcelInfo.ParcelId, response.StatusCode, responseContent);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"API Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WCS API调用异常，包裹ID: {ParcelId}", parcelInfo.ParcelId);

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
    /// 扫描包裹
    /// Scan parcel to register it in the wcs system
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始扫描包裹，条码: {Barcode}", barcode);

            // 构造请求数据
            // Build request data
            var requestData = new
            {
                barcode,
                scanTime = DateTime.Now
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            // Send POST request
            var response = await _httpClient.PostAsync(ApiConstants.WcsEndpoints.ParcelScan, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "扫描包裹成功，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Parcel scanned successfully",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "扫描包裹失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
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
            _logger.LogError(ex, "扫描包裹异常，条码: {Barcode}", barcode);

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
    /// 请求格口
    /// Request a chute/gate number for the parcel
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始请求格口，条码: {Barcode}", barcode);

            // 构造请求数据
            // Build request data
            var requestData = new
            {
                barcode,
                requestTime = DateTime.Now
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            // Send POST request
            var response = await _httpClient.PostAsync(ApiConstants.WcsEndpoints.ChuteRequest, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "请求格口成功，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Chute requested successfully",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "请求格口失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Chute Request Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求格口异常，条码: {Barcode}", barcode);

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
        string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始上传图片，条码: {Barcode}, 图片大小: {Size} bytes, 类型: {ContentType}", 
                barcode, imageData.Length, contentType);

            // 构造multipart/form-data请求
            // Build multipart/form-data request
            using var formContent = new MultipartFormDataContent();
            
            // 添加条码字段
            formContent.Add(new StringContent(barcode), "barcode");
            
            // 根据内容类型确定文件扩展名
            // Determine file extension based on content type
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

            // 发送POST请求
            // Send POST request
            var response = await _httpClient.PostAsync(ApiConstants.WcsEndpoints.ImageUpload, formContent, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传图片成功，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Image uploaded successfully",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "上传图片失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Image Upload Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传图片异常，条码: {Barcode}", barcode);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }
}
