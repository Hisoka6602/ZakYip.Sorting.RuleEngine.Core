namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 格口仓储接口
/// </summary>
public interface IChuteRepository
{
    /// <summary>
    /// 获取所有格口
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有格口的集合</returns>
    Task<IEnumerable<Entities.Chute>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取格口
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定ID的格口，如果不存在则返回null</returns>
    Task<Entities.Chute?> GetByIdAsync(long chuteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据格口编号获取格口
    /// </summary>
    /// <param name="chuteCode">格口编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定编号的格口，如果不存在则返回null</returns>
    Task<Entities.Chute?> GetByCodeAsync(string chuteCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加格口
    /// </summary>
    /// <param name="chute">要添加的格口实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加成功的格口实体</returns>
    Task<Entities.Chute> AddAsync(Entities.Chute chute, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新格口
    /// </summary>
    /// <param name="chute">要更新的格口实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(Entities.Chute chute, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除格口
    /// </summary>
    /// <param name="chuteId">要删除的格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(long chuteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的格口
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有启用状态的格口集合</returns>
    Task<IEnumerable<Entities.Chute>> GetEnabledChutesAsync(CancellationToken cancellationToken = default);
}
