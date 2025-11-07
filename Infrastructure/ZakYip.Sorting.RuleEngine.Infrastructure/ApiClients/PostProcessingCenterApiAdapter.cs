using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 邮政处理中心API适配器实现
/// Postal Processing Center API adapter implementation
/// 参考: https://github.com/Hisoka6602/JayTom.Dws/blob/聚水潭(正式)/JayTom.Dws.Interface/Post/PostApi.cs
/// </summary>
public class PostProcessingCenterApiAdapter : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostProcessingCenterApiAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostProcessingCenterApiAdapter(
        HttpClient httpClient,
        ILogger<PostProcessingCenterApiAdapter> logger)
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
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始请求格口（邮政处理中心），条码: {Barcode}", barcode);

            // 构造请求数据
            var requestData = new
            {
                barcode,
                requestTime = DateTime.Now,
                version = ApiConstants.PostProcessingCenterApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求查询路由
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.RoutingQuery}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "请求格口成功（邮政处理中心），条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Chute requested successfully from processing center",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "请求格口失败（邮政处理中心），条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
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
            _logger.LogError(ex, "请求格口异常（邮政处理中心），条码: {Barcode}", barcode);

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
    /// 上传图片到邮政处理中心
    /// Upload image to postal processing center
    /// </summary>
    public async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始上传图片到邮政处理中心，条码: {Barcode}, 图片大小: {Size} bytes",  
                barcode, imageData.Length);

            // 构造multipart/form-data请求
            using var formContent = new MultipartFormDataContent();
            
            // 添加条码字段
            formContent.Add(new StringContent(barcode), "barcode");
            formContent.Add(new StringContent(ApiConstants.PostProcessingCenterApi.CommonParams.Version), "version");
            
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

            // 发送POST请求
            // 注意：这里使用称重上传端点，因为邮政处理中心通常将图片与称重数据一起上传
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.WeighingUpload}";
            var response = await _httpClient.PostAsync(endpoint, formContent, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传图片成功（邮政处理中心），条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Image uploaded successfully to processing center",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "上传图片失败（邮政处理中心），条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
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
            _logger.LogError(ex, "上传图片异常（邮政处理中心），条码: {Barcode}", barcode);

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
