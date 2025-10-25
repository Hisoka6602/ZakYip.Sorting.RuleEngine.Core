namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;

/// <summary>
/// SQLite数据库方言实现
/// SQLite database dialect implementation
/// </summary>
public class SqliteDialect : IDatabaseDialect
{
    public bool SupportsPerformanceMonitoring => false;

    public string GetTableExistsQuery(string tableName)
    {
        return $@"
            SELECT COUNT(*) 
            FROM sqlite_master 
            WHERE type = 'table' 
            AND name = '{tableName}'";
    }

    public string GetCreateShardingTableQuery(string tableName)
    {
        return $@"
            CREATE TABLE IF NOT EXISTS '{tableName}' (
                'Id' TEXT NOT NULL PRIMARY KEY,
                'ParcelId' TEXT NOT NULL,
                'CartNumber' TEXT,
                'ChuteNumber' TEXT,
                'Status' TEXT,
                'Weight' REAL,
                'Volume' REAL,
                'ProcessingTimeMs' INTEGER NOT NULL,
                'CreatedAt' TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS 'IX_{tableName}_CreatedAt' ON '{tableName}' ('CreatedAt' DESC);
            CREATE INDEX IF NOT EXISTS 'IX_{tableName}_ParcelId' ON '{tableName}' ('ParcelId');
            CREATE INDEX IF NOT EXISTS 'IX_{tableName}_ParcelId_CreatedAt' ON '{tableName}' ('ParcelId', 'CreatedAt');";
    }

    public string GetTableStatisticsQuery()
    {
        // SQLite不支持详细的表统计，返回基本信息
        // SQLite doesn't support detailed table statistics, return basic info
        return @"
            SELECT 
                name as TableName,
                0 as RowCount,
                0 as SizeMB
            FROM sqlite_master 
            WHERE type = 'table'";
    }

    public string GetIndexUsageQuery()
    {
        // SQLite不支持索引使用统计
        // SQLite doesn't support index usage statistics
        return @"
            SELECT 
                '' as DatabaseName,
                '' as TableName,
                '' as IndexName
            WHERE 1 = 0";
    }

    public string GetConnectionStatusQuery()
    {
        // SQLite不支持连接状态查询
        // SQLite doesn't support connection status queries
        return @"
            SELECT 
                'Threads_connected' as Variable_name,
                '1' as Value
            WHERE 1 = 0";
    }

    public string GetSlowQueryStatisticsQuery()
    {
        // SQLite不支持慢查询统计
        // SQLite doesn't support slow query statistics
        return @"
            SELECT 
                0 as Count
            WHERE 1 = 0";
    }

    public string GetOptimizeDatabaseCommand()
    {
        return "VACUUM;";
    }
}
