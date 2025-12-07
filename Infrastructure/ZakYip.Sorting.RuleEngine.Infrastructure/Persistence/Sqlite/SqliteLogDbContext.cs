using Microsoft.EntityFrameworkCore;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite日志实体
/// SQLite log entry
/// </summary>
public class LogEntry
{
    public long Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// SQLite日志数据库上下文（降级方案）
/// SQLite log database context (fallback solution)
/// </summary>
public class SqliteLogDbContext : BaseLogDbContext
{
    public SqliteLogDbContext(DbContextOptions<SqliteLogDbContext> options)
        : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite特定：LogEntry配置
        // SQLite-specific: LogEntry configuration
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("log_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Details);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.Level).HasDatabaseName("IX_log_entries_Level");
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_log_entries_CreatedAt_Desc");
            entity.HasIndex(e => new { e.Level, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_log_entries_Level_CreatedAt");
        });

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureDwsCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.DwsCommunicationLog> entity)
    {
        entity.Property(e => e.Weight).HasColumnType("DECIMAL(18,2)");
        entity.Property(e => e.Volume).HasColumnType("DECIMAL(18,2)");
        entity.Property(e => e.ImagesJson);
    }

    protected override void ConfigureApiCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.ApiCommunicationLog> entity)
    {
        entity.Property(e => e.RequestBody);
        entity.Property(e => e.RequestHeaders);
        entity.Property(e => e.ResponseBody);
        entity.Property(e => e.ResponseHeaders);
        entity.Property(e => e.FormattedCurl);
    }

    protected override void ConfigureMatchingLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.MatchingLog> entity)
    {
        entity.Property(e => e.DwsContent);
        entity.Property(e => e.ApiContent);
    }

    protected override void ConfigureApiRequestLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.ApiRequestLog> entity)
    {
        entity.Property(e => e.RequestHeaders);
        entity.Property(e => e.RequestBody);
        entity.Property(e => e.ResponseHeaders);
        entity.Property(e => e.ResponseBody);
    }

    protected override void ConfigureMonitoringAlertDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.MonitoringAlert> entity)
    {
        entity.Property(e => e.CurrentValue).HasColumnType("DECIMAL(18,2)");
        entity.Property(e => e.ThresholdValue).HasColumnType("DECIMAL(18,2)");
        entity.Property(e => e.AdditionalData);
    }
}
