using EFCore.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

/// <summary>
/// 分片日志数据库上下文
/// </summary>
public class ShardedLogDbContext : DbContext
{
    private readonly ILogger<ShardedLogDbContext> _logger;

    public ShardedLogDbContext(
        DbContextOptions<ShardedLogDbContext> options,
        ILogger<ShardedLogDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    public DbSet<LogEntry> LogEntries { get; set; } = null!;
    public DbSet<ParcelLogEntry> ParcelLogEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置LogEntry实体
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Level).HasMaxLength(50);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
            entity.HasIndex(e => e.Level);
        });

        // 配置ParcelLogEntry实体（用于包裹处理日志）
        modelBuilder.Entity<ParcelLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CartNumber).HasMaxLength(100);
            entity.Property(e => e.ChuteNumber).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            
            // 索引优化
            entity.HasIndex(e => e.CreatedAt).IsDescending();
            entity.HasIndex(e => e.ParcelId);
            entity.HasIndex(e => new { e.ParcelId, e.CreatedAt });
        });
    }
}

/// <summary>
/// 包裹处理日志条目
/// </summary>
public class ParcelLogEntry
{
    public Guid Id { get; set; }
    public required string ParcelId { get; set; }
    public string? CartNumber { get; set; }
    public string? ChuteNumber { get; set; }
    public string? Status { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Volume { get; set; }
    public int ProcessingTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
