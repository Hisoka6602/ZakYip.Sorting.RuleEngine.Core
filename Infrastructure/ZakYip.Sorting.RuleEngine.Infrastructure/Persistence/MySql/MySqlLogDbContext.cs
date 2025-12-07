using Microsoft.EntityFrameworkCore;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL日志实体
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
/// MySQL日志数据库上下文
/// MySQL log database context
/// </summary>
public class MySqlLogDbContext : BaseLogDbContext
{
    public MySqlLogDbContext(DbContextOptions<MySqlLogDbContext> options)
        : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MySQL特定：LogEntry配置
        // MySQL-specific: LogEntry configuration
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("log_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Details).HasColumnType("text");
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.Level).HasDatabaseName("IX_log_entries_Level");
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_log_entries_CreatedAt_Desc");
            entity.HasIndex(e => new { e.Level, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_log_entries_Level_CreatedAt");
        });

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureDwsCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.DwsCommunicationLog> entity)
    {
        entity.Property(e => e.Weight).HasPrecision(18, 2);
        entity.Property(e => e.Volume).HasPrecision(18, 2);
        entity.Property(e => e.ImagesJson).HasColumnType("text");
    }

    protected override void ConfigureApiCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.ApiCommunicationLog> entity)
    {
        entity.Property(e => e.RequestBody).HasColumnType("text");
        entity.Property(e => e.RequestHeaders).HasColumnType("text");
        entity.Property(e => e.ResponseBody).HasColumnType("text");
        entity.Property(e => e.ResponseHeaders).HasColumnType("text");
        entity.Property(e => e.FormattedCurl).HasColumnType("text");
    }

    protected override void ConfigureMatchingLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.MatchingLog> entity)
    {
        entity.Property(e => e.DwsContent).HasColumnType("text");
        entity.Property(e => e.ApiContent).HasColumnType("text");
    }

    protected override void ConfigureApiRequestLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.ApiRequestLog> entity)
    {
        entity.Property(e => e.RequestHeaders).HasColumnType("text");
        entity.Property(e => e.RequestBody).HasColumnType("text");
        entity.Property(e => e.ResponseHeaders).HasColumnType("text");
        entity.Property(e => e.ResponseBody).HasColumnType("text");
    }

    protected override void ConfigureMonitoringAlertDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Entities.MonitoringAlert> entity)
    {
        entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
        entity.Property(e => e.ThresholdValue).HasPrecision(18, 2);
        entity.Property(e => e.AdditionalData).HasColumnType("text");
    }
}
