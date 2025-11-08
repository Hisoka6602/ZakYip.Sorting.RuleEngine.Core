namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 表存在性检查器接口
/// </summary>
public interface ITableExistenceChecker
{
    /// <summary>
    /// 检查指定的表是否存在
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表是否存在</returns>
    Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken = default);
}

/// <summary>
/// 表检查结果
/// </summary>
public class TableCheckResult
{
    /// <summary>
    /// 结果值
    /// </summary>
    public int Value { get; set; }
}
