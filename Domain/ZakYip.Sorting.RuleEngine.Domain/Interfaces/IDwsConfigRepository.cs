using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// DWS配置仓储接口
/// DWS configuration repository interface
/// </summary>
public interface IDwsConfigRepository
{
    /// <summary>
    /// 获取所有DWS配置
    /// Get all DWS configurations
    /// </summary>
    Task<IEnumerable<DwsConfig>> GetAllAsync();

    /// <summary>
    /// 获取所有启用的DWS配置
    /// Get all enabled DWS configurations
    /// </summary>
    Task<IEnumerable<DwsConfig>> GetEnabledConfigsAsync();

    /// <summary>
    /// 根据ID获取DWS配置
    /// Get DWS configuration by ID
    /// </summary>
    Task<DwsConfig?> GetByIdAsync(string configId);

    /// <summary>
    /// 添加DWS配置
    /// Add DWS configuration
    /// </summary>
    Task<bool> AddAsync(DwsConfig config);

    /// <summary>
    /// 更新DWS配置
    /// Update DWS configuration
    /// </summary>
    Task<bool> UpdateAsync(DwsConfig config);

    /// <summary>
    /// 删除DWS配置
    /// Delete DWS configuration
    /// </summary>
    Task<bool> DeleteAsync(string configId);
}
