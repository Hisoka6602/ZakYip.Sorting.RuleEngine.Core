using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

/// <summary>
/// 分片数据库表存在性检查器
/// Sharded database table existence checker
/// </summary>
public class ShardedTableExistenceChecker : ITableExistenceChecker
{
    private readonly ShardedLogDbContext _dbContext;
    private readonly ILogger<ShardedTableExistenceChecker> _logger;

    public ShardedTableExistenceChecker(
        ShardedLogDbContext dbContext,
        ILogger<ShardedTableExistenceChecker> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 检查指定的表是否存在（使用EF Core SqlQuery查询information_schema）
    /// Check if the specified table exists (using EF Core SqlQuery to query information_schema)
    /// </summary>
    public async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证表名，防止SQL注入
            TableNameValidator.Validate(tableName);

            // 使用EF Core的SqlQuery查询information_schema.tables
            // Use EF Core's SqlQuery to query information_schema.tables
            var sql = $@"
                SELECT 1 AS Value
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                AND table_name = '{tableName}'
                LIMIT 1";

            var result = await _dbContext.Database
                .SqlQuery<TableCheckResult>(FormattableStringFactory.Create(sql))
                .FirstOrDefaultAsync(cancellationToken);

            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "检查表 {TableName} 是否存在时发生错误", tableName);
            return false;
        }
    }
}

/// <summary>
/// 表检查结果
/// Table check result
/// </summary>
public class TableCheckResult
{
    public int Value { get; set; }
}
