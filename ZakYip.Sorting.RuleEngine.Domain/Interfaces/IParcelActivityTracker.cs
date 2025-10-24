namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 包裹活动追踪器接口
/// Interface for tracking parcel creation activity
/// </summary>
public interface IParcelActivityTracker
{
    /// <summary>
    /// 记录包裹创建时间
    /// Record parcel creation time
    /// </summary>
    void RecordParcelCreation();

    /// <summary>
    /// 获取距离上次包裹创建的分钟数
    /// Get minutes since last parcel creation
    /// </summary>
    int GetMinutesSinceLastActivity();

    /// <summary>
    /// 获取上次包裹创建时间
    /// Get last parcel creation time
    /// </summary>
    DateTime? GetLastActivityTime();

    /// <summary>
    /// 检查是否处于空闲状态（超过指定分钟数未创建包裹）
    /// Check if system is idle (no parcel created for specified minutes)
    /// </summary>
    bool IsIdle(int idleMinutes);
}
