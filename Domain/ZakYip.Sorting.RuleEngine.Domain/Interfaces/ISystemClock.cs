namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 系统时钟抽象接口，用于获取当前时间
/// System clock abstraction interface for getting current time
/// </summary>
/// <remarks>
/// 使用此接口而非直接使用 DateTime.Now/UtcNow 的好处：
/// Benefits of using this interface instead of DateTime.Now/UtcNow:
/// 1. 便于单元测试（可以 Mock 时间）/ Easy to unit test (can mock time)
/// 2. 统一时区管理 / Unified timezone management
/// 3. 避免时区转换错误 / Avoid timezone conversion errors
/// 4. 支持时间旅行测试场景 / Support time-travel testing scenarios
/// </remarks>
public interface ISystemClock
{
    /// <summary>
    /// 获取当前本地时间
    /// Get current local time
    /// </summary>
    /// <remarks>
    /// 推荐用于大多数业务场景：日志、记录、显示、业务逻辑
    /// Recommended for most business scenarios: logging, recording, display, business logic
    /// </remarks>
    DateTime LocalNow { get; }

    /// <summary>
    /// 获取当前 UTC 时间
    /// Get current UTC time
    /// </summary>
    /// <remarks>
    /// 仅在特定场景使用：
    /// Use only in specific scenarios:
    /// - 与外部系统通信时，协议明确要求 UTC 时间
    /// - 跨时区的分布式系统需要统一时间基准
    /// - 存储到数据库时需要 UTC（但显示时转换为本地时间）
    /// </remarks>
    DateTime UtcNow { get; }
}
