namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;

/// <summary>
/// 数据库方言接口，定义不同数据库的特定操作
/// Database dialect interface defining database-specific operations
/// </summary>
public interface IDatabaseDialect
{
    /// <summary>
    /// 检查表是否存在的SQL查询
    /// SQL query to check if a table exists
    /// </summary>
    string GetTableExistsQuery(string tableName);

    /// <summary>
    /// 创建分片表的SQL语句
    /// SQL statement to create a sharding table
    /// </summary>
    string GetCreateShardingTableQuery(string tableName);

    /// <summary>
    /// 获取表统计信息的SQL查询
    /// SQL query to get table statistics
    /// </summary>
    string GetTableStatisticsQuery();

    /// <summary>
    /// 获取索引使用情况的SQL查询
    /// SQL query to get index usage information
    /// </summary>
    string GetIndexUsageQuery();

    /// <summary>
    /// 获取连接状态的SQL查询
    /// SQL query to get connection status
    /// </summary>
    string GetConnectionStatusQuery();

    /// <summary>
    /// 获取慢查询统计的SQL查询
    /// SQL query to get slow query statistics
    /// </summary>
    string GetSlowQueryStatisticsQuery();

    /// <summary>
    /// 优化数据库的SQL命令
    /// SQL command to optimize the database
    /// </summary>
    string GetOptimizeDatabaseCommand();

    /// <summary>
    /// 是否支持性能监控特性
    /// Whether performance monitoring features are supported
    /// </summary>
    bool SupportsPerformanceMonitoring { get; }
}
