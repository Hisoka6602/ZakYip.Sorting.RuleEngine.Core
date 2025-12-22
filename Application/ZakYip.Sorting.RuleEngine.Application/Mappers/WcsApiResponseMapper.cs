using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Mappers;

/// <summary>
/// WCS API响应映射器
/// WCS API response mapper
/// 用于将 WcsApiResponse 映射为 ApiCommunicationLog
/// Used to map WcsApiResponse to ApiCommunicationLog
/// </summary>
public static class WcsApiResponseMapper
{
    /// <summary>
    /// 将WcsApiResponse映射为ApiCommunicationLog
    /// Map WcsApiResponse to ApiCommunicationLog
    /// </summary>
    /// <param name="response">WCS API响应 / WCS API response</param>
    /// <returns>API通信日志 / API communication log</returns>
    public static ApiCommunicationLog ToApiCommunicationLog(WcsApiResponse response)
    {
        return new ApiCommunicationLog
        {
            ParcelId = response.ParcelId,
            RequestUrl = response.RequestUrl,
            RequestBody = response.RequestBody,
            RequestHeaders = response.RequestHeaders,
            RequestTime = response.RequestTime,
            DurationMs = response.DurationMs,
            ResponseTime = response.ResponseTime,
            ResponseBody = response.ResponseBody,
            ResponseStatusCode = response.ResponseStatusCode,
            ResponseHeaders = response.ResponseHeaders,
            FormattedCurl = response.FormattedCurl,
            CommunicationType = CommunicationType.Http,
            IsSuccess = response.RequestStatus == ApiRequestStatus.Success,
            ErrorMessage = response.ErrorMessage
        };
    }
}
