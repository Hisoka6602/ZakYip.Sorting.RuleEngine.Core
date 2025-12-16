using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;

/// <summary>
/// DWS超时配置实现，从LiteDB加载
/// DWS timeout settings implementation, loaded from LiteDB
/// </summary>
public class DwsTimeoutSettingsFromDb : IDwsTimeoutSettings
{
    private readonly IDwsTimeoutConfigRepository _repository;
    private readonly ISystemClock _clock;
    private readonly object _lock = new();
    private DwsTimeoutConfig? _cachedConfig;
    private DateTime _lastLoadTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);

    public DwsTimeoutSettingsFromDb(IDwsTimeoutConfigRepository repository, ISystemClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    private DwsTimeoutConfig GetConfig()
    {
        // 使用双重检查锁定确保线程安全 / Use double-check locking for thread safety
        if (_cachedConfig == null || _clock.LocalNow - _lastLoadTime > _cacheExpiration)
        {
            lock (_lock)
            {
                // 再次检查，避免重复加载 / Double-check to avoid redundant loading
                if (_cachedConfig == null || _clock.LocalNow - _lastLoadTime > _cacheExpiration)
                {
                    // 使用 Task.Run 在线程池中执行异步操作，避免死锁
                    // Execute async operation in thread pool to avoid deadlock
                    _cachedConfig = Task.Run(async () => 
                        await _repository.GetByIdAsync(DwsTimeoutConfig.SingletonId).ConfigureAwait(false)
                    ).GetAwaiter().GetResult();
                    
                    _lastLoadTime = _clock.LocalNow;
                    
                    // 如果数据库中没有配置，返回默认值
                    if (_cachedConfig == null)
                    {
                        _cachedConfig = new DwsTimeoutConfig
                        {
                            ConfigId = DwsTimeoutConfig.SingletonId,
                            Enabled = true,
                            MinDwsWaitSeconds = 2,
                            MaxDwsWaitSeconds = 30,
                            ExceptionChuteId = 999, // 使用999作为默认值 / Use 999 as default value
                            CheckIntervalSeconds = 5,
                            Description = "Default DWS timeout configuration",
                            CreatedAt = _clock.LocalNow,
                            UpdatedAt = _clock.LocalNow
                        };
                    }
                }
            }
        }
        
        return _cachedConfig;
    }

    public bool Enabled => GetConfig().Enabled;

    public int MinDwsWaitSeconds => GetConfig().MinDwsWaitSeconds;

    public int MaxDwsWaitSeconds => GetConfig().MaxDwsWaitSeconds;

    public long ExceptionChuteId => GetConfig().ExceptionChuteId;

    public int CheckIntervalSeconds => GetConfig().CheckIntervalSeconds;
}
