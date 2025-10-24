namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 日志仓储接口
/// Log repository interface for persisting operation logs
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
}
