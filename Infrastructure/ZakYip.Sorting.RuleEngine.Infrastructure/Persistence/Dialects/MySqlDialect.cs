namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;

/// <summary>
/// MySQL数据库方言实现
/// MySQL database dialect implementation
/// </summary>
public class MySqlDialect : IDatabaseDialect
{
    public bool SupportsPerformanceMonitoring => true;

    public string GetTableExistsQuery(string tableName)
    {
        TableNameValidator.Validate(tableName);
        // 使用 EXISTS 来检查表是否存在
        // Note: EF Core's SqlQueryRaw requires FormattableString or manual parameter handling
        // Since we validate the table name, we can safely embed it
        return $@"
            SELECT EXISTS(
                SELECT 1 
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                AND table_name = '{tableName}'
            ) AS Value";
    }

    public string GetCreateShardingTableQuery(string tableName)
    {
        TableNameValidator.Validate(tableName);
        return $@"
            CREATE TABLE IF NOT EXISTS `{tableName}` (
                `Id` char(36) NOT NULL,
                `ParcelId` varchar(100) NOT NULL,
                `CartNumber` varchar(100) DEFAULT NULL,
                `ChuteNumber` varchar(100) DEFAULT NULL,
                `Status` varchar(50) DEFAULT NULL,
                `Weight` decimal(18,2) DEFAULT NULL,
                `Volume` decimal(18,2) DEFAULT NULL,
                `ProcessingTimeMs` int NOT NULL,
                `CreatedAt` datetime(6) NOT NULL,
                PRIMARY KEY (`Id`),
                KEY `IX_{tableName}_CreatedAt` (`CreatedAt` DESC),
                KEY `IX_{tableName}_ParcelId` (`ParcelId`),
                KEY `IX_{tableName}_ParcelId_CreatedAt` (`ParcelId`, `CreatedAt`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
    }

    public string GetTableStatisticsQuery()
    {
        return @"
            SELECT 
                table_name as TableName,
                table_rows as RowCount,
                ROUND((data_length + index_length) / 1024 / 1024, 2) as SizeMB
            FROM information_schema.TABLES
            WHERE table_schema = DATABASE()
            ORDER BY (data_length + index_length) DESC";
    }

    public string GetIndexUsageQuery()
    {
        return @"
            SELECT 
                OBJECT_SCHEMA as DatabaseName,
                OBJECT_NAME as TableName,
                INDEX_NAME as IndexName
            FROM performance_schema.table_io_waits_summary_by_index_usage
            WHERE INDEX_NAME IS NOT NULL
            AND INDEX_NAME != 'PRIMARY'
            AND COUNT_STAR = 0
            AND OBJECT_SCHEMA = DATABASE()";
    }

    public string GetConnectionStatusQuery()
    {
        return @"
            SHOW STATUS WHERE Variable_name IN (
                'Threads_connected', 
                'Threads_running', 
                'Max_used_connections',
                'Aborted_connects'
            )";
    }

    public string GetSlowQueryStatisticsQuery()
    {
        return @"
            SELECT 
                COUNT(*) as Count
            FROM information_schema.processlist
            WHERE time > 5
            AND command != 'Sleep'";
    }

    public string GetOptimizeDatabaseCommand()
    {
        // MySQL不需要显式的VACUUM命令
        // MySQL doesn't need explicit VACUUM command
        return string.Empty;
    }
}
