using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Post;

/// <summary>
/// 邮政处理中心API适配器实现
/// Postal Processing Center API adapter implementation
/// </summary>
public class PostProcessingCenterApiAdapter : IPostProcessingCenterApiAdapter
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
    /// 上传称重数据到邮政处理中心
    /// Upload weighing data to postal processing center
    /// </summary>
    public async Task<PostalApiResponse> UploadWeighingDataAsync(
        PostalParcelData parcelData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("上传称重数据到邮政处理中心，条码: {Barcode}", parcelData.Barcode);

            // 构造请求数据
            var requestData = new
            {
                barcode = parcelData.Barcode,
                weight = parcelData.Weight,
                length = parcelData.Length,
                width = parcelData.Width,
                height = parcelData.Height,
                volume = parcelData.Volume,
                senderAddress = parcelData.SenderAddress,
                recipientAddress = parcelData.RecipientAddress,
                destinationCode = parcelData.DestinationCode,
                scannedAt = parcelData.ScannedAt,
                version = ApiConstants.PostProcessingCenterApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.WeighingUpload}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传称重数据到邮政处理中心成功，条码: {Barcode}, 状态码: {StatusCode}",
                    parcelData.Barcode, response.StatusCode);

                return new PostalApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Weighing data uploaded successfully",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "上传称重数据到邮政处理中心失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    parcelData.Barcode, response.StatusCode, responseContent);

                return new PostalApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Upload Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传称重数据到邮政处理中心异常，条码: {Barcode}", parcelData.Barcode);

            return new PostalApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = null
            };
        }
    }

    /// <summary>
    /// 查询包裹路由信息
    /// Query parcel routing information
    /// </summary>
    public async Task<PostalApiResponse> QueryParcelRoutingAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("查询邮政处理中心包裹路由信息，条码: {Barcode}", barcode);

            // 构造请求数据
            var requestData = new
            {
                barcode,
                version = ApiConstants.PostProcessingCenterApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.RoutingQuery}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "查询邮政处理中心包裹路由信息成功，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new PostalApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Parcel routing query successful",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "查询邮政处理中心包裹路由信息失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new PostalApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Query Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询邮政处理中心包裹路由信息异常，条码: {Barcode}", barcode);

            return new PostalApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = null
            };
        }
    }

    /// <summary>
    /// 上传分拣结果
    /// Upload sorting result
    /// </summary>
    public async Task<PostalApiResponse> UploadSortingResultAsync(
        string barcode,
        string destinationCode,
        string chuteNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "上传分拣结果到邮政处理中心，条码: {Barcode}, 目的地: {DestinationCode}, 格口: {ChuteNumber}",
                barcode, destinationCode, chuteNumber);

            // 构造请求数据
            var requestData = new
            {
                barcode,
                destinationCode,
                chuteNumber,
                sortTime = DateTime.Now,
                version = ApiConstants.PostProcessingCenterApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.SortingResultUpload}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传分拣结果到邮政处理中心成功，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new PostalApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Sorting result uploaded successfully",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "上传分拣结果到邮政处理中心失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new PostalApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Upload Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分拣结果到邮政处理中心异常，条码: {Barcode}", barcode);

            return new PostalApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = null
            };
        }
    }

    /// <summary>
    /// 上传包裹扫描数据
    /// Upload parcel scan data
    /// </summary>
    public async Task<PostalApiResponse> UploadScanDataAsync(
        string barcode,
        DateTime scanTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("上传包裹扫描数据到邮政处理中心，条码: {Barcode}", barcode);

            // 构造请求数据
            var requestData = new
            {
                barcode,
                scanTime,
                version = ApiConstants.PostProcessingCenterApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            var endpoint = $"{ApiConstants.PostProcessingCenterApi.RouterEndpoint}{ApiConstants.PostProcessingCenterApi.Endpoints.ScanUpload}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传包裹扫描数据到邮政处理中心成功，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new PostalApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Scan data uploaded successfully",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "上传包裹扫描数据到邮政处理中心失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new PostalApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Upload Error: {response.StatusCode}",
                    Data = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传包裹扫描数据到邮政处理中心异常，条码: {Barcode}", barcode);

            return new PostalApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = null
            };
        }
    }
}
