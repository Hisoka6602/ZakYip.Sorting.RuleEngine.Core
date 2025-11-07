using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Post;

/// <summary>
/// 邮政分揽投机构API适配器实现
/// Postal Collection/Delivery Institution API adapter implementation
/// </summary>
public class PostCollectionApiAdapter : IPostCollectionApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostCollectionApiAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostCollectionApiAdapter(
        HttpClient httpClient,
        ILogger<PostCollectionApiAdapter> logger)
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
    /// 上传称重数据到邮政分揽投机构
    /// Upload weighing data to postal collection institution
    /// </summary>
    public async Task<PostalApiResponse> UploadWeighingDataAsync(
        PostalParcelData parcelData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("上传称重数据到邮政分揽投机构，条码: {Barcode}", parcelData.Barcode);

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
                version = ApiConstants.PostCollectionApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            var endpoint = $"{ApiConstants.PostCollectionApi.RouterEndpoint}{ApiConstants.PostCollectionApi.Endpoints.WeighingUpload}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传称重数据到邮政分揽投机构成功，条码: {Barcode}, 状态码: {StatusCode}",
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
                    "上传称重数据到邮政分揽投机构失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
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
            _logger.LogError(ex, "上传称重数据到邮政分揽投机构异常，条码: {Barcode}", parcelData.Barcode);

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
    /// 查询包裹信息
    /// Query parcel information
    /// </summary>
    public async Task<PostalApiResponse> QueryParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("查询邮政分揽投机构包裹信息，条码: {Barcode}", barcode);

            // 构造请求数据
            var requestData = new
            {
                barcode,
                version = ApiConstants.PostCollectionApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            var endpoint = $"{ApiConstants.PostCollectionApi.RouterEndpoint}{ApiConstants.PostCollectionApi.Endpoints.ParcelQuery}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "查询邮政分揽投机构包裹信息成功，条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new PostalApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Parcel query successful",
                    Data = responseContent
                };
            }
            else
            {
                _logger.LogWarning(
                    "查询邮政分揽投机构包裹信息失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
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
            _logger.LogError(ex, "查询邮政分揽投机构包裹信息异常，条码: {Barcode}", barcode);

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
            _logger.LogDebug("上传包裹扫描数据到邮政分揽投机构，条码: {Barcode}", barcode);

            // 构造请求数据
            var requestData = new
            {
                barcode,
                scanTime,
                version = ApiConstants.PostCollectionApi.CommonParams.Version
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, ApiConstants.ContentTypes.ApplicationJson);

            // 发送POST请求
            var endpoint = $"{ApiConstants.PostCollectionApi.RouterEndpoint}{ApiConstants.PostCollectionApi.Endpoints.ScanUpload}";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "上传包裹扫描数据到邮政分揽投机构成功，条码: {Barcode}, 状态码: {StatusCode}",
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
                    "上传包裹扫描数据到邮政分揽投机构失败，条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
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
            _logger.LogError(ex, "上传包裹扫描数据到邮政分揽投机构异常，条码: {Barcode}", barcode);

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
