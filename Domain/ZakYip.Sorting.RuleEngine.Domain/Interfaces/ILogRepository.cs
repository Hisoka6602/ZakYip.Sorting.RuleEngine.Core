namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 日志仓储接口
/// </summary>
public interface ILogRepository
{
    /// <summary>
    /// 写入日志
    /// Write log entry
    /// </summary>
    Task LogAsync(string level, string message, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入信息日志
    /// Write info log
    /// </summary>
    Task LogInfoAsync(string message, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入警告日志
    /// Write warning log
    /// </summary>
    Task LogWarningAsync(string message, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入错误日志
    /// Write error log
    /// </summary>
    Task LogErrorAsync(string message, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新DWS通信日志中的图片路径
    /// Bulk update image paths in DWS communication logs
    /// </summary>
    /// <param name="oldPrefix">旧路径前缀 / Old path prefix</param>
    /// <param name="newPrefix">新路径前缀 / New path prefix</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>更新的记录数 / Number of updated records</returns>
    Task<int> BulkUpdateImagePathsAsync(string oldPrefix, string newPrefix, CancellationToken cancellationToken = default);
}
