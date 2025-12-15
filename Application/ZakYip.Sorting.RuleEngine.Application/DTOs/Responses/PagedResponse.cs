using System.ComponentModel.DataAnnotations;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 分页响应包装器
/// </summary>
/// <typeparam name="T">数据项类型</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [Required]
    public bool Success { get; set; } = true;

    /// <summary>
    /// 数据列表
    /// </summary>
    [Required]
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    [Required]
    public int Total { get; set; }

    /// <summary>
    /// 当前页码（从1开始）
    /// </summary>
    [Required]
    public int Page { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    [Required]
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    [Required]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 0;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    [Required]
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    [Required]
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// 错误消息（失败时）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码（失败时）
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = SystemClockProvider.LocalNow;

    /// <summary>
    /// 创建成功的分页响应
    /// </summary>
    public static PagedResponse<T> SuccessResult(List<T> data, int total, int page, int pageSize)
    {
        return new PagedResponse<T>
        {
            Success = true,
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize,
            Timestamp = SystemClockProvider.LocalNow
        };
    }

    /// <summary>
    /// 创建失败的分页响应
    /// </summary>
    public static PagedResponse<T> FailureResult(string errorMessage, string? errorCode = null)
    {
        return new PagedResponse<T>
        {
            Success = false,
            Data = new List<T>(),
            Total = 0,
            Page = 0,
            PageSize = 0,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Timestamp = SystemClockProvider.LocalNow
        };
    }
}
