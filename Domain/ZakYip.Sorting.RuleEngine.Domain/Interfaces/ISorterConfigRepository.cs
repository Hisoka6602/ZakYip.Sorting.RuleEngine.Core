using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 分拣机配置仓储接口
/// Sorter configuration repository interface
/// </summary>
public interface ISorterConfigRepository
{
    /// <summary>
    /// 获取分拣机配置（单例）
    /// Get sorter configuration (singleton)
    /// </summary>
    Task<SorterConfig?> GetByIdAsync(long id);

    /// <summary>
    /// 添加或更新分拣机配置（Upsert）
    /// Add or update sorter configuration (Upsert)
    /// </summary>
    Task<bool> UpsertAsync(SorterConfig config);
}
