using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 第三方API客户端实现
/// Third-party API client implementation
/// </summary>
public class ThirdPartyApiClient : IThirdPartyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThirdPartyApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ThirdPartyApiClient(
        HttpClient httpClient,
        ILogger<ThirdPartyApiClient> logger)
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
    /// 上传数据到第三方API
    /// Upload data to third-party API
    /// </summary>
    public async Task<ThirdPartyResponse> UploadDataAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始调用第三方API，包裹ID: {ParcelId}", parcelInfo.ParcelId);

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
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 发送POST请求
            // Send POST request
            var response = await _httpClient.PostAsync("/api/sorting/upload", content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "第三方API调用成功，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                    parcelInfo.ParcelId, response.StatusCode);

                return new ThirdPartyResponse
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
                    "第三方API返回错误，包裹ID: {ParcelId}, 状态码: {StatusCode}, 响应: {Response}",
                    parcelInfo.ParcelId, response.StatusCode, responseContent);

                return new ThirdPartyResponse
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
            _logger.LogError(ex, "第三方API调用异常，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            return new ThirdPartyResponse
            {
                Success = false,
                Code = "ERROR",
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }
}
