using Microsoft.EntityFrameworkCore;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite日志数据库上下文（降级方案）
/// </summary>
public class SqliteLogDbContext : DbContext
{
    public SqliteLogDbContext(DbContextOptions<SqliteLogDbContext> options)
        : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries { get; set; } = null!;
    public DbSet<CommunicationLog> CommunicationLogs { get; set; } = null!;
    public DbSet<Chute> Chutes { get; set; } = null!;
    public DbSet<SorterCommunicationLog> SorterCommunicationLogs { get; set; } = null!;
    public DbSet<DwsCommunicationLog> DwsCommunicationLogs { get; set; } = null!;
    public DbSet<ApiCommunicationLog> ApiCommunicationLogs { get; set; } = null!;
    public DbSet<MatchingLog> MatchingLogs { get; set; } = null!;
    public DbSet<ApiRequestLog> ApiRequestLogs { get; set; } = null!;

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
            
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_communication_logs_ParcelId");
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_communication_logs_CreatedAt_Desc");
            entity.HasIndex(e => new { e.CommunicationType, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_communication_logs_Type_CreatedAt");
        });

        modelBuilder.Entity<Chute>(entity =>
        {
            entity.ToTable("chutes");
            entity.HasKey(e => e.ChuteId);
            entity.Property(e => e.ChuteName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ChuteCode).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.ChuteName).HasDatabaseName("IX_chutes_ChuteName");
            entity.HasIndex(e => e.ChuteCode).HasDatabaseName("IX_chutes_ChuteCode");
            // 索引：CreatedAt字段按降序排序，用于时间范围查询和排序
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_chutes_CreatedAt_Desc");
        });

        modelBuilder.Entity<SorterCommunicationLog>(entity =>
        {
            entity.ToTable("sorter_communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SorterAddress).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CommunicationType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OriginalContent).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.FormattedContent).HasMaxLength(2000);
            entity.Property(e => e.ExtractedParcelId).HasMaxLength(100);
            entity.Property(e => e.ExtractedCartNumber).HasMaxLength(100);
            entity.Property(e => e.CommunicationTime).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.ExtractedParcelId).HasDatabaseName("IX_sorter_comm_logs_ParcelId");
            entity.HasIndex(e => e.CommunicationTime).IsDescending().HasDatabaseName("IX_sorter_comm_logs_Time_Desc");
        });

        modelBuilder.Entity<DwsCommunicationLog>(entity =>
        {
            entity.ToTable("dws_communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DwsAddress).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OriginalContent).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.FormattedContent).HasMaxLength(2000);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Weight).HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.Volume).HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.CommunicationTime).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.Barcode).HasDatabaseName("IX_dws_comm_logs_Barcode");
            entity.HasIndex(e => e.CommunicationTime).IsDescending().HasDatabaseName("IX_dws_comm_logs_Time_Desc");
        });

        modelBuilder.Entity<ApiCommunicationLog>(entity =>
        {
            entity.ToTable("api_communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RequestUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RequestBody);
            entity.Property(e => e.RequestHeaders);
            entity.Property(e => e.RequestTime).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.ResponseBody);
            entity.Property(e => e.ResponseStatusCode);
            entity.Property(e => e.ResponseHeaders);
            entity.Property(e => e.FormattedCurl);
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_api_comm_logs_ParcelId");
            entity.HasIndex(e => e.RequestTime).IsDescending().HasDatabaseName("IX_api_comm_logs_RequestTime_Desc");
        });

        modelBuilder.Entity<MatchingLog>(entity =>
        {
            entity.ToTable("matching_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DwsContent);
            entity.Property(e => e.ApiContent);
            entity.Property(e => e.MatchedRuleId).HasMaxLength(100);
            entity.Property(e => e.MatchingReason).HasMaxLength(500);
            entity.Property(e => e.ChuteId);
            entity.Property(e => e.CartOccupancy).IsRequired();
            entity.Property(e => e.MatchingTime).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_matching_logs_ParcelId");
            entity.HasIndex(e => e.MatchingTime).IsDescending().HasDatabaseName("IX_matching_logs_Time_Desc");
            entity.HasIndex(e => e.ChuteId).HasDatabaseName("IX_matching_logs_ChuteId");
        });

        modelBuilder.Entity<ApiRequestLog>(entity =>
        {
            entity.ToTable("api_request_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestTime).IsRequired();
            entity.Property(e => e.RequestIp).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RequestMethod).HasMaxLength(10).IsRequired();
            entity.Property(e => e.RequestPath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.QueryString).HasMaxLength(2000);
            entity.Property(e => e.RequestHeaders);
            entity.Property(e => e.RequestBody);
            entity.Property(e => e.ResponseTime);
            entity.Property(e => e.ResponseStatusCode);
            entity.Property(e => e.ResponseHeaders);
            entity.Property(e => e.ResponseBody);
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            entity.HasIndex(e => e.RequestTime).IsDescending().HasDatabaseName("IX_api_request_logs_RequestTime_Desc");
            entity.HasIndex(e => e.RequestPath).HasDatabaseName("IX_api_request_logs_RequestPath");
            entity.HasIndex(e => e.RequestIp).HasDatabaseName("IX_api_request_logs_RequestIp");
            entity.HasIndex(e => new { e.RequestMethod, e.RequestTime }).IsDescending(false, true).HasDatabaseName("IX_api_request_logs_Method_Time");
        });
    }
}

/// <summary>
/// SQLite日志实体
/// </summary>
public class LogEntry
{
    public long Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
