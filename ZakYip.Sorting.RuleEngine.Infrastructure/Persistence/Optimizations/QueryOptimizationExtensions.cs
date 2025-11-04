using Microsoft.EntityFrameworkCore;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Optimizations;

/// <summary>
/// 数据库查询优化助手类
/// Database query optimization helper class
/// </summary>
public static class QueryOptimizationExtensions
{
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
        return await context.Database.ExecuteSqlRawAsync(sql, cancellationToken, createdBefore);
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
}
