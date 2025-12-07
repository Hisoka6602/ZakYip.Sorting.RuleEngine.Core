using Microsoft.EntityFrameworkCore;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 日志数据库上下文基类，包含所有共享的实体配置
/// Base log database context with all shared entity configurations
/// </summary>
public abstract class BaseLogDbContext : DbContext
{
    protected BaseLogDbContext(DbContextOptions options) : base(options)
    {
    }

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
        ConfigureCommunicationLog(modelBuilder);
        ConfigureChute(modelBuilder);
        ConfigureSorterCommunicationLog(modelBuilder);
        ConfigureDwsCommunicationLog(modelBuilder);
        ConfigureApiCommunicationLog(modelBuilder);
        ConfigureMatchingLog(modelBuilder);
        ConfigureApiRequestLog(modelBuilder);
        ConfigureMonitoringAlert(modelBuilder);
    }

    /// <summary>
    /// 配置通信日志实体，可被子类重写以提供数据库特定配置
    /// Configure communication log entity, can be overridden for database-specific configuration
    /// </summary>
    protected virtual void ConfigureCommunicationLog(ModelBuilder modelBuilder)
    {
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
            entity.HasIndex(e => e.IsSuccess).HasDatabaseName("IX_communication_logs_IsSuccess");
            entity.HasIndex(e => new { e.IsSuccess, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_communication_logs_IsSuccess_CreatedAt");
        });
    }

    protected virtual void ConfigureChute(ModelBuilder modelBuilder)
    {
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
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_chutes_CreatedAt_Desc");
        });
    }

    protected virtual void ConfigureSorterCommunicationLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SorterCommunicationLog>(entity =>
        {
            entity.ToTable("sorter_communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CommunicationType).IsRequired();
            entity.Property(e => e.SorterAddress).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OriginalContent).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.FormattedContent).HasMaxLength(2000);
            entity.Property(e => e.ExtractedParcelId).HasMaxLength(100);
            entity.Property(e => e.ExtractedCartNumber).HasMaxLength(100);
            entity.Property(e => e.CommunicationTime).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.ExtractedParcelId).HasDatabaseName("IX_sorter_comm_logs_ParcelId");
            entity.HasIndex(e => e.CommunicationTime).IsDescending().HasDatabaseName("IX_sorter_comm_logs_Time_Desc");
            entity.HasIndex(e => e.CommunicationType).HasDatabaseName("IX_sorter_comm_logs_Type");
            entity.HasIndex(e => e.IsSuccess).HasDatabaseName("IX_sorter_comm_logs_IsSuccess");
            entity.HasIndex(e => new { e.CommunicationType, e.IsSuccess, e.CommunicationTime })
                .IsDescending(false, false, true).HasDatabaseName("IX_sorter_comm_logs_Type_Success_Time");
        });
    }

    protected virtual void ConfigureDwsCommunicationLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DwsCommunicationLog>(entity =>
        {
            entity.ToTable("dws_communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CommunicationType).IsRequired();
            entity.Property(e => e.DwsAddress).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OriginalContent).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.FormattedContent).HasMaxLength(2000);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.CommunicationTime).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.Barcode).HasDatabaseName("IX_dws_comm_logs_Barcode");
            entity.HasIndex(e => e.CommunicationTime).IsDescending().HasDatabaseName("IX_dws_comm_logs_Time_Desc");
            entity.HasIndex(e => e.CommunicationType).HasDatabaseName("IX_dws_comm_logs_Type");
            entity.HasIndex(e => e.IsSuccess).HasDatabaseName("IX_dws_comm_logs_IsSuccess");
            entity.HasIndex(e => new { e.Barcode, e.CommunicationTime })
                .IsDescending(false, true).HasDatabaseName("IX_dws_comm_logs_Barcode_Time");
            
            ConfigureDwsCommunicationLogDatabaseSpecific(entity);
        });
    }

    protected virtual void ConfigureApiCommunicationLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiCommunicationLog>(entity =>
        {
            entity.ToTable("api_communication_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CommunicationType).IsRequired();
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RequestUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RequestTime).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.ResponseStatusCode);
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_api_comm_logs_ParcelId");
            entity.HasIndex(e => e.RequestTime).IsDescending().HasDatabaseName("IX_api_comm_logs_RequestTime_Desc");
            entity.HasIndex(e => e.CommunicationType).HasDatabaseName("IX_api_comm_logs_Type");
            entity.HasIndex(e => e.IsSuccess).HasDatabaseName("IX_api_comm_logs_IsSuccess");
            entity.HasIndex(e => new { e.ParcelId, e.RequestTime })
                .IsDescending(false, true).HasDatabaseName("IX_api_comm_logs_ParcelId_RequestTime");
            entity.HasIndex(e => e.DurationMs).IsDescending().HasDatabaseName("IX_api_comm_logs_DurationMs_Desc");
            
            ConfigureApiCommunicationLogDatabaseSpecific(entity);
        });
    }

    protected virtual void ConfigureMatchingLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchingLog>(entity =>
        {
            entity.ToTable("matching_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
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
            entity.HasIndex(e => e.IsSuccess).HasDatabaseName("IX_matching_logs_IsSuccess");
            entity.HasIndex(e => e.MatchedRuleId).HasDatabaseName("IX_matching_logs_MatchedRuleId");
            entity.HasIndex(e => new { e.ChuteId, e.MatchingTime })
                .IsDescending(false, true).HasDatabaseName("IX_matching_logs_ChuteId_Time");
            
            ConfigureMatchingLogDatabaseSpecific(entity);
        });
    }

    protected virtual void ConfigureApiRequestLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiRequestLog>(entity =>
        {
            entity.ToTable("api_request_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestTime).IsRequired();
            entity.Property(e => e.RequestIp).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RequestMethod).HasMaxLength(10).IsRequired();
            entity.Property(e => e.RequestPath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.QueryString).HasMaxLength(2000);
            entity.Property(e => e.ResponseTime);
            entity.Property(e => e.ResponseStatusCode);
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            entity.HasIndex(e => e.RequestTime).IsDescending().HasDatabaseName("IX_api_request_logs_RequestTime_Desc");
            entity.HasIndex(e => e.RequestPath).HasDatabaseName("IX_api_request_logs_RequestPath");
            entity.HasIndex(e => e.RequestIp).HasDatabaseName("IX_api_request_logs_RequestIp");
            entity.HasIndex(e => new { e.RequestMethod, e.RequestTime }).IsDescending(false, true).HasDatabaseName("IX_api_request_logs_Method_Time");
            entity.HasIndex(e => e.IsSuccess).HasDatabaseName("IX_api_request_logs_IsSuccess");
            entity.HasIndex(e => e.ResponseStatusCode).HasDatabaseName("IX_api_request_logs_StatusCode");
            entity.HasIndex(e => e.DurationMs).IsDescending().HasDatabaseName("IX_api_request_logs_DurationMs_Desc");
            entity.HasIndex(e => new { e.RequestPath, e.RequestTime })
                .IsDescending(false, true).HasDatabaseName("IX_api_request_logs_Path_Time");
            
            ConfigureApiRequestLogDatabaseSpecific(entity);
        });
    }

    protected virtual void ConfigureMonitoringAlert(ModelBuilder modelBuilder)
    {
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
            entity.Property(e => e.AlertTime).IsRequired();
            entity.Property(e => e.IsResolved).IsRequired();
            entity.Property(e => e.ResolvedTime);
            
            entity.HasIndex(e => e.AlertTime).IsDescending().HasDatabaseName("IX_monitoring_alerts_AlertTime_Desc");
            entity.HasIndex(e => e.IsResolved).HasDatabaseName("IX_monitoring_alerts_IsResolved");
            entity.HasIndex(e => new { e.Type, e.AlertTime }).IsDescending(false, true).HasDatabaseName("IX_monitoring_alerts_Type_AlertTime");
            entity.HasIndex(e => new { e.IsResolved, e.AlertTime }).IsDescending(false, true).HasDatabaseName("IX_monitoring_alerts_IsResolved_AlertTime");
            
            ConfigureMonitoringAlertDatabaseSpecific(entity);
        });
    }

    /// <summary>
    /// 子类重写以提供数据库特定的DWS通信日志配置（如列类型）
    /// Override to provide database-specific DWS communication log configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureDwsCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<DwsCommunicationLog> entity)
    {
    }

    /// <summary>
    /// 子类重写以提供数据库特定的API通信日志配置（如列类型）
    /// Override to provide database-specific API communication log configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureApiCommunicationLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ApiCommunicationLog> entity)
    {
    }

    /// <summary>
    /// 子类重写以提供数据库特定的匹配日志配置（如列类型）
    /// Override to provide database-specific matching log configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureMatchingLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<MatchingLog> entity)
    {
    }

    /// <summary>
    /// 子类重写以提供数据库特定的API请求日志配置（如列类型）
    /// Override to provide database-specific API request log configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureApiRequestLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ApiRequestLog> entity)
    {
    }

    /// <summary>
    /// 子类重写以提供数据库特定的监控告警配置（如列类型）
    /// Override to provide database-specific monitoring alert configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureMonitoringAlertDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<MonitoringAlert> entity)
    {
    }
}
