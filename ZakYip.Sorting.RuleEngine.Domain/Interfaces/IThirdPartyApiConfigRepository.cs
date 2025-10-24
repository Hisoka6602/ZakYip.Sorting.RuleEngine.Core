using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 第三方API配置仓储接口
/// </summary>
public interface IThirdPartyApiConfigRepository
{
    /// <summary>
    /// 获取所有启用的API配置（按优先级排序）
    /// </summary>
    /// <returns>启用的API配置列表</returns>
    Task<IEnumerable<ThirdPartyApiConfig>> GetEnabledConfigsAsync();

    /// <summary>
    /// 根据配置ID获取API配置
    /// </summary>
    /// <param name="configId">配置ID</param>
    /// <returns>API配置，如果不存在则返回null</returns>
    Task<ThirdPartyApiConfig?> GetByIdAsync(string configId);

    /// <summary>
    /// 获取所有API配置
    /// </summary>
    /// <returns>所有API配置</returns>
    Task<IEnumerable<ThirdPartyApiConfig>> GetAllAsync();

    /// <summary>
    /// 添加API配置
    /// </summary>
    /// <param name="config">API配置</param>
    /// <returns>是否添加成功</returns>
    Task<bool> AddAsync(ThirdPartyApiConfig config);

    /// <summary>
    /// 更新API配置
    /// </summary>
    /// <param name="config">API配置</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateAsync(ThirdPartyApiConfig config);

    /// <summary>
    /// 删除API配置
    /// </summary>
    /// <param name="configId">配置ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(string configId);
}
