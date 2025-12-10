using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// DWS数据模板仓储接口
/// DWS data template repository interface
/// </summary>
public interface IDwsDataTemplateRepository
{
    /// <summary>
    /// 获取所有数据模板
    /// Get all data templates
    /// </summary>
    Task<IEnumerable<DwsDataTemplate>> GetAllAsync();

    /// <summary>
    /// 获取所有启用的数据模板
    /// Get all enabled data templates
    /// </summary>
    Task<IEnumerable<DwsDataTemplate>> GetEnabledTemplatesAsync();

    /// <summary>
    /// 根据ID获取数据模板
    /// Get data template by ID
    /// </summary>
    Task<DwsDataTemplate?> GetByIdAsync(string templateId);

    /// <summary>
    /// 添加数据模板
    /// Add data template
    /// </summary>
    Task<bool> AddAsync(DwsDataTemplate template);

    /// <summary>
    /// 更新数据模板
    /// Update data template
    /// </summary>
    Task<bool> UpdateAsync(DwsDataTemplate template);

    /// <summary>
    /// 删除数据模板
    /// Delete data template
    /// </summary>
    Task<bool> DeleteAsync(string templateId);
}
