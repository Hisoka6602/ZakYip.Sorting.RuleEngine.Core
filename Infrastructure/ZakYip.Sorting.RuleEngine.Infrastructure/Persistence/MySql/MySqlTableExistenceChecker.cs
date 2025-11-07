using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL表存在性检查器
/// MySQL table existence checker
/// </summary>
public class MySqlTableExistenceChecker : ITableExistenceChecker
{
    private readonly MySqlLogDbContext _dbContext;
    private readonly ILogger<MySqlTableExistenceChecker> _logger;

    public MySqlTableExistenceChecker(
        MySqlLogDbContext dbContext,
        ILogger<MySqlTableExistenceChecker> logger)
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
            FormattableString sql = $@"
                SELECT 1 AS Value
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                AND table_name = {tableName}
                LIMIT 1";

            var result = await _dbContext.Database
                .SqlQuery<TableCheckResult>(sql)
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
