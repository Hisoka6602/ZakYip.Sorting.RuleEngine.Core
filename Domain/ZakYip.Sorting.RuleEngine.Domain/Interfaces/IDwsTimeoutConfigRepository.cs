using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// DWS超时配置仓储接口
/// DWS timeout configuration repository interface
/// </summary>
public interface IDwsTimeoutConfigRepository
{
    /// <summary>
    /// 获取DWS超时配置（单例）
    /// Get DWS timeout configuration (singleton)
    /// </summary>
    Task<DwsTimeoutConfig?> GetByIdAsync(long id);

    /// <summary>
    /// 添加或更新DWS超时配置（Upsert）
    /// Add or update DWS timeout configuration (Upsert)
    /// </summary>
    Task<bool> UpsertAsync(DwsTimeoutConfig config);
}
