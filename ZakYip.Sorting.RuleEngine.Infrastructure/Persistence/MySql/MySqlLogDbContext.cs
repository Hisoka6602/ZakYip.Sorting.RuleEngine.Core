using Microsoft.EntityFrameworkCore;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL日志实体
/// </summary>
public class LogEntry
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// MySQL日志数据库上下文
/// </summary>
public class MySqlLogDbContext : DbContext
{
    public MySqlLogDbContext(DbContextOptions<MySqlLogDbContext> options)
        : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries { get; set; } = null!;
    public DbSet<CommunicationLog> CommunicationLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("log_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Details).HasColumnType("text");
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // 索引：Level字段用于日志级别筛选
            entity.HasIndex(e => e.Level).HasDatabaseName("IX_log_entries_Level");
            
            // 索引：CreatedAt字段按降序排序，用于时间范围查询和排序
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_log_entries_CreatedAt_Desc");
            
            // 复合索引：Level + CreatedAt，优化按日志级别和时间的查询
            entity.HasIndex(e => new { e.Level, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_log_entries_Level_CreatedAt");
        });

        modelBuilder.Entity<CommunicationLog>(entity =>
        {
            entity.ToTable("communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CommunicationType).IsRequired();
            entity.Property(e => e.Direction).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ParcelId).HasMaxLength(100);
            entity.Property(e => e.RemoteAddress).HasMaxLength(200);
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // 索引：ParcelId用于按包裹查询
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_communication_logs_ParcelId");
            
            // 索引：CreatedAt按降序，用于时间范围查询
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_communication_logs_CreatedAt_Desc");
            
            // 复合索引：CommunicationType + CreatedAt
            entity.HasIndex(e => new { e.CommunicationType, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_communication_logs_Type_CreatedAt");
        });
    }
}
