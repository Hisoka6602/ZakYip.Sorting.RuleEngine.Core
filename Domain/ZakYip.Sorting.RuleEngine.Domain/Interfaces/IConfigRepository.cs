namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 配置仓储基接口 - 提供配置管理的通用CRUD操作
/// Base configuration repository interface - Provides common CRUD operations for configuration management
/// </summary>
/// <typeparam name="TConfig">配置实体类型 / Configuration entity type</typeparam>
/// <remarks>
/// 此接口消除了 IDwsConfigRepository 和 IWcsApiConfigRepository 之间的重复定义，
/// 遵循DRY原则。
/// This interface eliminates duplicate definitions between IDwsConfigRepository 
/// and IWcsApiConfigRepository, following the DRY principle.
/// </remarks>
public interface IConfigRepository<TConfig> where TConfig : class
{
    /// <summary>
    /// 获取所有配置
    /// Get all configurations
    /// </summary>
    /// <returns>所有配置列表 / List of all configurations</returns>
    Task<IEnumerable<TConfig>> GetAllAsync();

    /// <summary>
    /// 获取所有启用的配置
    /// Get all enabled configurations
    /// </summary>
    /// <returns>启用的配置列表 / List of enabled configurations</returns>
    Task<IEnumerable<TConfig>> GetEnabledConfigsAsync();

    /// <summary>
    /// 根据ID获取配置
    /// Get configuration by ID
    /// </summary>
    /// <param name="configId">配置ID / Configuration ID</param>
    /// <returns>配置对象，如果不存在则返回null / Configuration object, or null if not found</returns>
    Task<TConfig?> GetByIdAsync(long configId);

    /// <summary>
    /// 添加配置
    /// Add configuration
    /// </summary>
    /// <param name="config">配置对象 / Configuration object</param>
    /// <returns>是否添加成功 / Whether addition was successful</returns>
    Task<bool> AddAsync(TConfig config);

    /// <summary>
    /// 更新配置
    /// Update configuration
    /// </summary>
    /// <param name="config">配置对象 / Configuration object</param>
    /// <returns>是否更新成功 / Whether update was successful</returns>
    Task<bool> UpdateAsync(TConfig config);

    /// <summary>
    /// 删除配置
    /// Delete configuration
    /// </summary>
    /// <param name="configId">配置ID / Configuration ID</param>
    /// <returns>是否删除成功 / Whether deletion was successful</returns>
    Task<bool> DeleteAsync(long configId);
}
