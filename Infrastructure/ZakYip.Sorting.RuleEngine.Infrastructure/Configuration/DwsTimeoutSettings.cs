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
        // 简单的缓存机制，避免频繁查询数据库
        if (_cachedConfig == null || _clock.LocalNow - _lastLoadTime > _cacheExpiration)
        {
            _cachedConfig = _repository.GetByIdAsync(DwsTimeoutConfig.SingletonId).GetAwaiter().GetResult();
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
                    ExceptionChuteId = 0,
                    CheckIntervalSeconds = 5,
                    Description = "Default DWS timeout configuration",
                    CreatedAt = _clock.LocalNow,
                    UpdatedAt = _clock.LocalNow
                };
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
