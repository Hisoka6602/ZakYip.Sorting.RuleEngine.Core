using Microsoft.EntityFrameworkCore;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL日志实体
/// MySQL log entity for persistent logging
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
/// MySQL database context for logging
/// </summary>
public class MySqlLogDbContext : DbContext
{
    public MySqlLogDbContext(DbContextOptions<MySqlLogDbContext> options)
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
            entity.Property(e => e.Details).HasColumnType("text");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
