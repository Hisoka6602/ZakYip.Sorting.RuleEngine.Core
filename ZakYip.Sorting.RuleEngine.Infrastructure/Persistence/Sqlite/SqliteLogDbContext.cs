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
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.CreatedAt);
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
