namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 表存在性检查器接口
/// Table existence checker interface
/// </summary>
public interface ITableExistenceChecker
{
    /// <summary>
    /// 检查指定的表是否存在
    /// Check if the specified table exists
    /// </summary>
    /// <param name="tableName">表名 / Table name</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>表是否存在 / Whether the table exists</returns>
    Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken = default);
}

/// <summary>
/// 表检查结果
/// Table check result
/// </summary>
public class TableCheckResult
{
    public int Value { get; set; }
}
