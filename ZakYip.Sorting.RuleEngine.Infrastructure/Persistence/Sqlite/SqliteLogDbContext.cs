using Microsoft.EntityFrameworkCore;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite日志数据库上下文（降级方案）
/// SQLite database context for logging (fallback solution)
/// </summary>
public class SqliteLogDbContext : DbContext
{
    public SqliteLogDbContext(DbContextOptions<SqliteLogDbContext> options)
        : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("log_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Details);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // 索引：Level字段用于日志级别筛选
            // Index: Level field for log level filtering
            entity.HasIndex(e => e.Level).HasDatabaseName("IX_log_entries_Level");
            
            // 索引：CreatedAt字段按降序排序，用于时间范围查询和排序
            // Index: CreatedAt field in descending order for time range queries and sorting
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_log_entries_CreatedAt_Desc");
            
            // 复合索引：Level + CreatedAt，优化按日志级别和时间的查询
            // Composite index: Level + CreatedAt for optimized queries by log level and time
            entity.HasIndex(e => new { e.Level, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_log_entries_Level_CreatedAt");
        });
    }
}

/// <summary>
/// SQLite日志实体
/// SQLite log entity
/// </summary>
public class LogEntry
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
