using Microsoft.EntityFrameworkCore;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

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
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureLogEntry(ModelBuilder modelBuilder)
    {
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
    }

    protected override void ConfigureDwsCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.DwsCommunicationLog> entity)
    {
        entity.Property(e => e.Weight).HasColumnType("DECIMAL(18,2)");
        entity.Property(e => e.Volume).HasColumnType("DECIMAL(18,2)");
        // SQLite doesn't require explicit text column type specification
        // SQLite 不需要显式指定文本列类型
        entity.Property(e => e.ImagesJson);
    }

    protected override void ConfigureApiCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.ApiCommunicationLog> entity)
    {
        // SQLite doesn't require explicit text column type specification (unlike MySQL's HasColumnType("text"))
        // SQLite 不需要显式指定文本列类型（不同于 MySQL 的 HasColumnType("text")）
        entity.Property(e => e.RequestBody);
        entity.Property(e => e.RequestHeaders);
        entity.Property(e => e.ResponseBody);
        entity.Property(e => e.ResponseHeaders);
        entity.Property(e => e.FormattedCurl);
    }

    protected override void ConfigureMatchingLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.MatchingLog> entity)
    {
        // SQLite doesn't require explicit text column type specification
        // SQLite 不需要显式指定文本列类型
        entity.Property(e => e.DwsContent);
        entity.Property(e => e.ApiContent);
    }

    protected override void ConfigureApiRequestLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.ApiRequestLog> entity)
    {
        // SQLite doesn't require explicit text column type specification
        // SQLite 不需要显式指定文本列类型
        entity.Property(e => e.RequestHeaders);
        entity.Property(e => e.RequestBody);
        entity.Property(e => e.ResponseHeaders);
        entity.Property(e => e.ResponseBody);
    }

    protected override void ConfigureMonitoringAlertDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.MonitoringAlert> entity)
    {
        entity.Property(e => e.CurrentValue).HasColumnType("DECIMAL(18,2)");
        entity.Property(e => e.ThresholdValue).HasColumnType("DECIMAL(18,2)");
        // SQLite doesn't require explicit text column type specification
        // SQLite 不需要显式指定文本列类型
        entity.Property(e => e.AdditionalData);
    }
    
    protected override void ConfigureConfigurationAuditLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.ConfigurationAuditLog> entity)
    {
        // SQLite doesn't require explicit text column type specification
        // SQLite 不需要显式指定文本列类型
        entity.Property(e => e.ContentBefore);
        entity.Property(e => e.ContentAfter);
    }
}
