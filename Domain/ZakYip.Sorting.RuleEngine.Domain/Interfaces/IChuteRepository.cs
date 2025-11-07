namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 格口仓储接口
/// </summary>
public interface IChuteRepository
{
    /// <summary>
    /// 获取所有格口
    /// </summary>
    Task<IEnumerable<Entities.Chute>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取格口
    /// </summary>
    Task<Entities.Chute?> GetByIdAsync(long chuteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据格口编号获取格口
    /// </summary>
    Task<Entities.Chute?> GetByCodeAsync(string chuteCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加格口
    /// </summary>
    Task<Entities.Chute> AddAsync(Entities.Chute chute, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新格口
    /// </summary>
    Task UpdateAsync(Entities.Chute chute, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除格口
    /// </summary>
    Task DeleteAsync(long chuteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的格口
    /// </summary>
    Task<IEnumerable<Entities.Chute>> GetEnabledChutesAsync(CancellationToken cancellationToken = default);
}
