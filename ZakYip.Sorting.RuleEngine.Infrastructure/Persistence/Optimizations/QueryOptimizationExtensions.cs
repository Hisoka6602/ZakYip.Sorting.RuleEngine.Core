using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Optimizations;

/// <summary>
/// 数据库查询优化助手类
/// Database query optimization helper class
/// </summary>
public static class QueryOptimizationExtensions
{
    /// <summary>
    /// 慢查询阈值（毫秒）- 超过此时间的查询将被记录
    /// Slow query threshold (milliseconds) - Queries exceeding this time will be logged
    /// </summary>
    private const int SlowQueryThresholdMs = 1000;

    /// <summary>
    /// 优化分页查询 - 使用AsNoTracking提高只读查询性能
    /// Optimize paged queries - Use AsNoTracking for better read-only performance
    /// </summary>
    public static IQueryable<T> OptimizedPaging<T>(this IQueryable<T> query, int page, int pageSize)
        where T : class
    {
        return query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// 优化时间范围查询 - 确保索引被使用
    /// Optimize time range queries - Ensure indexes are used
    /// </summary>
    public static IQueryable<T> OptimizedTimeRange<T>(
        this IQueryable<T> query,
        Func<T, DateTime> timeSelector,
        DateTime? startTime = null,
        DateTime? endTime = null)
        where T : class
    {
        var result = query.AsNoTracking();

        if (startTime.HasValue)
        {
            result = result.Where(x => timeSelector(x) >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            result = result.Where(x => timeSelector(x) <= endTime.Value);
        }

        return result;
    }

    /// <summary>
    /// 批量插入优化 - 禁用自动检测变化以提高性能
    /// Optimize bulk insert - Disable auto detect changes for better performance
    /// </summary>
    public static async Task BulkInsertAsync<T>(this DbContext context, IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : class
    {
        var originalAutoDetectChanges = context.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            // 禁用自动检测变化以提高批量插入性能
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            await context.AddRangeAsync(entities, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            // 恢复原始设置
            context.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
        }
    }

    /// <summary>
    /// 批量删除优化 - 使用原始SQL提高性能
    /// Optimize bulk delete - Use raw SQL for better performance
    /// </summary>
    public static async Task<int> BulkDeleteAsync<T>(
        this DbContext context,
        string tableName,
        DateTime createdBefore,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // 使用参数化查询防止SQL注入
        var sql = $"DELETE FROM {tableName} WHERE CreatedAt < {{0}}";
        return await context.Database.ExecuteSqlRawAsync(sql, createdBefore, cancellationToken);
    }

    /// <summary>
    /// 获取查询执行计划（用于性能分析）
    /// Get query execution plan (for performance analysis)
    /// </summary>
    public static async Task<string> GetExecutionPlanAsync<T>(this IQueryable<T> query)
        where T : class
    {
        // 注意：这个方法主要用于开发和调试
        // Note: This method is mainly for development and debugging
        return await Task.FromResult(query.ToQueryString());
    }

    /// <summary>
    /// 编译查询以提高重复执行的性能
    /// Compile query for better performance on repeated execution
    /// </summary>
    public static Func<DbContext, DateTime, DateTime, IAsyncEnumerable<T>> CompileTimeRangeQuery<T>(
        Func<DbContext, DateTime, DateTime, IQueryable<T>> queryBuilder)
        where T : class
    {
        return EF.CompileAsyncQuery(
            (DbContext ctx, DateTime start, DateTime end) =>
                queryBuilder(ctx, start, end).AsNoTracking()
        );
    }

    /// <summary>
    /// 执行查询并检测慢查询 - 自动记录慢查询到日志
    /// Execute query with slow query detection - Auto log slow queries
    /// </summary>
    public static async Task<List<T>> ExecuteWithSlowQueryDetectionAsync<T>(
        this IQueryable<T> query,
        ILogger logger,
        string queryName,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var results = await query.ToListAsync(cancellationToken);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
        {
            // 慢查询仅记录到日志文件，不输出到控制台
            logger.LogWarning(
                "慢查询检测: {QueryName} 执行时间 {ElapsedMs}ms (阈值: {ThresholdMs}ms), 返回记录数: {Count}, SQL: {QueryString}",
                queryName,
                stopwatch.ElapsedMilliseconds,
                SlowQueryThresholdMs,
                results.Count,
                query.ToQueryString());
        }

        return results;
    }

    /// <summary>
    /// 监控索引使用情况 - 返回查询计划以分析索引使用
    /// Monitor index usage - Return query plan for index analysis
    /// </summary>
    public static async Task<(List<T> Results, string QueryPlan)> ExecuteWithIndexMonitoringAsync<T>(
        this IQueryable<T> query,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var queryString = query.ToQueryString();
        var stopwatch = Stopwatch.StartNew();
        var results = await query.ToListAsync(cancellationToken);
        stopwatch.Stop();

        // 记录查询性能和SQL语句用于索引分析
        if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
        {
            logger.LogWarning(
                "索引监控 - 慢查询: 执行时间 {ElapsedMs}ms, 记录数: {Count}, 建议检查索引使用情况",
                stopwatch.ElapsedMilliseconds,
                results.Count);
        }

        return (results, queryString);
    }

    /// <summary>
    /// 获取慢查询优化建议
    /// Get slow query optimization suggestions
    /// </summary>
    public static string GetOptimizationSuggestions(long executionTimeMs, int recordCount, string queryString)
    {
        var suggestions = new List<string>();

        if (executionTimeMs > 5000)
        {
            suggestions.Add("查询执行时间超过5秒，强烈建议优化");
        }
        else if (executionTimeMs > SlowQueryThresholdMs)
        {
            suggestions.Add($"查询执行时间超过{SlowQueryThresholdMs}ms，建议优化");
        }

        if (recordCount > 10000)
        {
            suggestions.Add("返回记录数超过10000条，建议增加分页或添加过滤条件");
        }

        if (queryString.Contains("SELECT *"))
        {
            suggestions.Add("查询使用了SELECT *，建议只选择需要的列");
        }

        if (!queryString.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("查询缺少WHERE条件，可能导致全表扫描，建议添加索引列过滤");
        }

        if (queryString.Contains("OR", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("查询包含OR条件，可能无法有效使用索引，建议考虑使用UNION或IN");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("查询性能良好，无需优化");
        }

        return string.Join("; ", suggestions);
    }
}
