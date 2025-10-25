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
        ValidateTableName(tableName);
        return $@"
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_schema = DATABASE() 
            AND table_name = '{tableName}'";
    }

    public string GetCreateShardingTableQuery(string tableName)
    {
        ValidateTableName(tableName);
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

    /// <summary>
    /// 验证表名，防止SQL注入
    /// Validate table name to prevent SQL injection
    /// </summary>
    private static void ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
        }

        // 只允许字母、数字、下划线，且必须以字母或下划线开头
        // Only allow letters, numbers, underscores, and must start with letter or underscore
        if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            throw new ArgumentException($"Invalid table name: {tableName}. Table names must start with a letter or underscore and contain only letters, numbers, and underscores.", nameof(tableName));
        }

        // 限制长度（MySQL表名最大64字符）
        // Limit length (MySQL table name max 64 characters)
        if (tableName.Length > 64)
        {
            throw new ArgumentException($"Table name is too long: {tableName}. Maximum length is 64 characters.", nameof(tableName));
        }
    }
}
