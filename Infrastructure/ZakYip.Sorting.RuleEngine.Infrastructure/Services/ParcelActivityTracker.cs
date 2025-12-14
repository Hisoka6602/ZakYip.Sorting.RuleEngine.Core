using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 包裹活动追踪器实现
/// </summary>
public class ParcelActivityTracker : IParcelActivityTracker
{
    private DateTime? _lastActivityTime;
    private readonly object _lock = new();
    private readonly ISystemClock _clock;

    public ParcelActivityTracker(ISystemClock clock)
    {
        _clock = clock;
    }

    /// <summary>
    /// 记录包裹创建时间
    /// Record parcel creation time
    /// </summary>
    public void RecordParcelCreation()
    {
        lock (_lock)
        {
            _lastActivityTime = _clock.LocalNow;
        }
    }

    /// <summary>
    /// 获取距离上次包裹创建的分钟数
    /// Get minutes since last parcel creation
    /// </summary>
    public int GetMinutesSinceLastActivity()
    {
        lock (_lock)
        {
            if (_lastActivityTime == null)
            {
                return int.MaxValue; // 从未创建过包裹
            }

            return (int)(_clock.LocalNow - _lastActivityTime.Value).TotalMinutes;
        }
    }

    /// <summary>
    /// 获取上次包裹创建时间
    /// Get last parcel creation time
    /// </summary>
    public DateTime? GetLastActivityTime()
    {
        lock (_lock)
        {
            return _lastActivityTime;
        }
    }

    /// <summary>
    /// 检查是否处于空闲状态（超过指定分钟数未创建包裹）
    /// Check if system is idle (no parcel created for specified minutes)
    /// </summary>
    public bool IsIdle(int idleMinutes)
    {
        return GetMinutesSinceLastActivity() >= idleMinutes;
    }
}
