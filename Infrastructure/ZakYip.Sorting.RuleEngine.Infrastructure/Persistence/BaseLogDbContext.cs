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
    public DbSet<ConfigurationAuditLog> ConfigurationAuditLogs { get; set; } = null!;
    public DbSet<ParcelInfo> ParcelInfos { get; set; } = null!;
    public DbSet<ParcelLifecycleNodeEntity> ParcelLifecycleNodes { get; set; } = null!;

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
        ConfigureConfigurationAuditLog(modelBuilder);
        ConfigureLogEntry(modelBuilder);
        ConfigureParcelInfo(modelBuilder);
        ConfigureParcelLifecycleNode(modelBuilder);
    }

    /// <summary>
    /// 配置日志实体，可被子类重写以提供数据库特定配置
    /// Configure log entry entity, can be overridden for database-specific configuration
    /// </summary>
    protected virtual void ConfigureLogEntry(ModelBuilder modelBuilder)
    {
        // Default implementation for common index configuration
        // 子类需要配置表名和字段类型
        // Subclasses need to configure table name and field types
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
    
    /// <summary>
    /// 配置配置审计日志实体
    /// Configure configuration audit log entity
    /// </summary>
    protected virtual void ConfigureConfigurationAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigurationAuditLog>(entity =>
        {
            entity.ToTable("configuration_audit_logs");
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.AuditId).ValueGeneratedOnAdd();
            entity.Property(e => e.ConfigurationType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ConfigurationId).IsRequired();
            entity.Property(e => e.OperationType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.OperatorUser).HasMaxLength(200);
            entity.Property(e => e.OperatorIpAddress).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            
            entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_config_audit_logs_CreatedAt_Desc");
            entity.HasIndex(e => new { e.ConfigurationType, e.ConfigurationId, e.CreatedAt })
                .IsDescending(false, false, true).HasDatabaseName("IX_config_audit_logs_Type_Id_CreatedAt");
            entity.HasIndex(e => new { e.ConfigurationType, e.CreatedAt })
                .IsDescending(false, true).HasDatabaseName("IX_config_audit_logs_Type_CreatedAt");
            entity.HasIndex(e => e.OperationType).HasDatabaseName("IX_config_audit_logs_OperationType");
            
            ConfigureConfigurationAuditLogDatabaseSpecific(entity);
        });
    }
    
    /// <summary>
    /// 子类重写以提供数据库特定的配置审计日志配置（如列类型）
    /// Override to provide database-specific configuration audit log configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureConfigurationAuditLogDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ConfigurationAuditLog> entity)
    {
    }
    
    /// <summary>
    /// 配置包裹信息实体
    /// Configure parcel info entity
    /// </summary>
    protected virtual void ConfigureParcelInfo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParcelInfo>(entity =>
        {
            entity.ToTable("parcel_infos");
            entity.HasKey(e => e.ParcelId);
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CartNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Barcode).HasMaxLength(100);
            
            // DWS 信息 - 精度 18,3
            entity.Property(e => e.Length).HasPrecision(18, 3);
            entity.Property(e => e.Width).HasPrecision(18, 3);
            entity.Property(e => e.Height).HasPrecision(18, 3);
            entity.Property(e => e.Volume).HasPrecision(18, 3);
            entity.Property(e => e.Weight).HasPrecision(18, 3);
            
            // 分拣信息
            entity.Property(e => e.TargetChute).HasMaxLength(50);
            entity.Property(e => e.ActualChute).HasMaxLength(50);
            entity.Property(e => e.DecisionReason).HasMaxLength(200);
            entity.Property(e => e.MatchedRuleId).HasMaxLength(100);
            entity.Property(e => e.PositionBias).IsRequired();
            
            // 袋信息
            entity.Property(e => e.BagId).HasMaxLength(100);
            
            // 时间信息
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CompletedAt);
            
            // 状态信息
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.LifecycleStage).IsRequired();
            
            // 索引配置（高并发查询优化）
            entity.HasIndex(e => e.ParcelId).HasDatabaseName("IX_parcel_infos_ParcelId");
            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_parcel_infos_Status_CreatedAt");
            entity.HasIndex(e => new { e.TargetChute, e.CreatedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_parcel_infos_TargetChute_CreatedAt");
            entity.HasIndex(e => e.CompletedAt)
                .IsDescending()
                .HasDatabaseName("IX_parcel_infos_CompletedAt_Desc");
            entity.HasIndex(e => e.BagId)
                .HasDatabaseName("IX_parcel_infos_BagId");
            entity.HasIndex(e => new { e.LifecycleStage, e.CreatedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_parcel_infos_LifecycleStage_CreatedAt");
            entity.HasIndex(e => e.CartNumber)
                .HasDatabaseName("IX_parcel_infos_CartNumber");
            
            ConfigureParcelInfoDatabaseSpecific(entity);
        });
    }
    
    /// <summary>
    /// 子类重写以提供数据库特定的包裹信息配置（如列类型）
    /// Override to provide database-specific parcel info configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureParcelInfoDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ParcelInfo> entity)
    {
    }
    
    /// <summary>
    /// 配置包裹生命周期节点实体
    /// Configure parcel lifecycle node entity
    /// </summary>
    protected virtual void ConfigureParcelLifecycleNode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParcelLifecycleNodeEntity>(entity =>
        {
            entity.ToTable("parcel_lifecycle_nodes");
            entity.HasKey(e => e.NodeId);
            entity.Property(e => e.NodeId).ValueGeneratedOnAdd();
            entity.Property(e => e.ParcelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Stage).IsRequired();
            entity.Property(e => e.EventTime).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // 索引配置（高并发查询优化）
            entity.HasIndex(e => new { e.ParcelId, e.EventTime })
                .IsDescending(false, true)
                .HasDatabaseName("IX_parcel_lifecycle_ParcelId_EventTime");
            entity.HasIndex(e => e.EventTime)
                .IsDescending()
                .HasDatabaseName("IX_parcel_lifecycle_EventTime_Desc");
            entity.HasIndex(e => new { e.Stage, e.EventTime })
                .IsDescending(false, true)
                .HasDatabaseName("IX_parcel_lifecycle_Stage_EventTime");
            
            ConfigureParcelLifecycleNodeDatabaseSpecific(entity);
        });
    }
    
    /// <summary>
    /// 子类重写以提供数据库特定的包裹生命周期节点配置（如列类型）
    /// Override to provide database-specific parcel lifecycle node configuration (e.g., column types)
    /// </summary>
    protected virtual void ConfigureParcelLifecycleNodeDatabaseSpecific(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ParcelLifecycleNodeEntity> entity)
    {
    }
}
