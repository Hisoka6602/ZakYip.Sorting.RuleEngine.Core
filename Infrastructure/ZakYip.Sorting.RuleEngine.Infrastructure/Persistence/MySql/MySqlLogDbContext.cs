using Microsoft.EntityFrameworkCore;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

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
/// </summary>
public class MySqlLogDbContext : DbContext
{
    public MySqlLogDbContext(DbContextOptions<MySqlLogDbContext> options)
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
    public DbSet<MonitoringAlert> MonitoringAlerts { get; set; } = null!;

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

        modelBuilder.Entity<Chute>(entity =>
        {
            entity.ToTable("chutes");
            entity.HasKey(e => e.ChuteId);
            entity.Property(e => e.ChuteName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ChuteCode).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // 索引：ChuteName用于按名称查询
            entity.HasIndex(e => e.ChuteName).HasDatabaseName("IX_chutes_ChuteName");
            
            // 索引：ChuteCode用于按编号查询
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
            
            // 索引：ExtractedParcelId用于按包裹查询
            entity.HasIndex(e => e.ExtractedParcelId).HasDatabaseName("IX_sorter_comm_logs_ParcelId");
            
            // 索引：CommunicationTime按降序
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
            entity.Property(e => e.Weight).HasPrecision(18, 2);
            entity.Property(e => e.Volume).HasPrecision(18, 2);
            entity.Property(e => e.CommunicationTime).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            // 索引：Barcode用于按条码查询
            entity.HasIndex(e => e.Barcode).HasDatabaseName("IX_dws_comm_logs_Barcode");
            
            // 索引：CommunicationTime按降序
            entity.HasIndex(e => e.CommunicationTime).IsDescending().HasDatabaseName("IX_dws_comm_logs_Time_Desc");
        });

        modelBuilder.Entity<ApiCommunicationLog>(entity =>
        {
            entity.ToTable("api_communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RequestUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RequestBody).HasColumnType("text");
            entity.Property(e => e.RequestHeaders).HasColumnType("text");
            entity.Property(e => e.RequestTime).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.ResponseBody).HasColumnType("text");
            entity.Property(e => e.ResponseStatusCode);
            entity.Property(e => e.ResponseHeaders).HasColumnType("text");
            entity.Property(e => e.FormattedCurl).HasColumnType("text");
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            // 索引：ParcelId用于按包裹查询
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_api_comm_logs_ParcelId");
            
            // 索引：RequestTime按降序
            entity.HasIndex(e => e.RequestTime).IsDescending().HasDatabaseName("IX_api_comm_logs_RequestTime_Desc");
        });

        modelBuilder.Entity<MatchingLog>(entity =>
        {
            entity.ToTable("matching_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DwsContent).HasColumnType("text");
            entity.Property(e => e.ApiContent).HasColumnType("text");
            entity.Property(e => e.MatchedRuleId).HasMaxLength(100);
            entity.Property(e => e.MatchingReason).HasMaxLength(500);
            entity.Property(e => e.ChuteId);
            entity.Property(e => e.CartOccupancy).IsRequired();
            entity.Property(e => e.MatchingTime).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            // 索引：ParcelId用于按包裹查询
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_matching_logs_ParcelId");
            
            // 索引：MatchingTime按降序
            entity.HasIndex(e => e.MatchingTime).IsDescending().HasDatabaseName("IX_matching_logs_Time_Desc");
            
            // 索引：ChuteId用于按格口查询
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
            entity.Property(e => e.RequestHeaders).HasColumnType("text");
            entity.Property(e => e.RequestBody).HasColumnType("text");
            entity.Property(e => e.ResponseTime);
            entity.Property(e => e.ResponseStatusCode);
            entity.Property(e => e.ResponseHeaders).HasColumnType("text");
            entity.Property(e => e.ResponseBody).HasColumnType("text");
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            // 索引：RequestTime按降序，用于时间范围查询
            entity.HasIndex(e => e.RequestTime).IsDescending().HasDatabaseName("IX_api_request_logs_RequestTime_Desc");

            // 索引：RequestPath用于按路径查询
            entity.HasIndex(e => e.RequestPath).HasDatabaseName("IX_api_request_logs_RequestPath");

            // 索引：RequestIp用于按IP查询
            entity.HasIndex(e => e.RequestIp).HasDatabaseName("IX_api_request_logs_RequestIp");

            // 复合索引：RequestMethod + RequestTime
            entity.HasIndex(e => new { e.RequestMethod, e.RequestTime }).IsDescending(false, true).HasDatabaseName("IX_api_request_logs_Method_Time");
        });

        modelBuilder.Entity<MonitoringAlert>(entity =>
        {
            entity.ToTable("monitoring_alerts");
            entity.HasKey(e => e.AlertId);
            entity.Property(e => e.AlertId).ValueGeneratedOnAdd();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Severity).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ResourceId).HasMaxLength(100);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            entity.Property(e => e.ThresholdValue).HasPrecision(18, 2);
            entity.Property(e => e.AlertTime).IsRequired();
            entity.Property(e => e.IsResolved).IsRequired();
            entity.Property(e => e.ResolvedTime);
            entity.Property(e => e.AdditionalData).HasColumnType("text");
            
            // 索引：AlertTime按降序，用于时间范围查询
            entity.HasIndex(e => e.AlertTime).IsDescending().HasDatabaseName("IX_monitoring_alerts_AlertTime_Desc");
            
            // 索引：IsResolved用于查询活跃告警
            entity.HasIndex(e => e.IsResolved).HasDatabaseName("IX_monitoring_alerts_IsResolved");
            
            // 复合索引：Type + AlertTime
            entity.HasIndex(e => new { e.Type, e.AlertTime }).IsDescending(false, true).HasDatabaseName("IX_monitoring_alerts_Type_AlertTime");
            
            // 复合索引：IsResolved + AlertTime，优化活跃告警查询
            entity.HasIndex(e => new { e.IsResolved, e.AlertTime }).IsDescending(false, true).HasDatabaseName("IX_monitoring_alerts_IsResolved_AlertTime");
        });
    }
}
