using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;
using ZakYip.Sorting.RuleEngine.Service.Configuration;

namespace ZakYip.Sorting.RuleEngine.Service;

/// <summary>
/// 主程序入口 - Windows服务与MiniAPI
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // 配置NLog
        var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        
        try
        {
            logger.Info("应用程序启动中...");
            
            var builder = WebApplication.CreateBuilder(args);

#if !DEBUG
            // 仅在Release模式下配置Windows服务
            builder.Host.UseWindowsService();
            logger.Info("Windows服务模式已启用");
#else
            logger.Info("控制台模式运行（DEBUG模式）");
#endif

            // 配置NLog作为日志提供程序
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // 配置Kestrel服务器
            builder.WebHost.UseKestrel(options =>
            {
                options.AddServerHeader = false; // 不发送Server头以提高安全性
                options.Limits.MaxConcurrentConnections = 1000;
                options.Limits.MaxConcurrentUpgradedConnections = 1000;
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            });

            // 配置应用设置
            var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>() 
                ?? new AppSettings();

        // 注册配置
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        
        // 注册分片配置
        builder.Services.Configure<ShardingSettings>(builder.Configuration.GetSection("AppSettings:Sharding"));
        
        // 注册日志文件清理配置
        builder.Services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.LogFileCleanupSettings>(
            builder.Configuration.GetSection("AppSettings:LogFileCleanup"));
        
        // 注册数据库熔断器配置
        builder.Services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.DatabaseCircuitBreakerSettings>(
            builder.Configuration.GetSection("AppSettings:MySql:CircuitBreaker"));

        // 配置LiteDB（用于配置存储）
        builder.Services.AddSingleton<ILiteDatabase>(sp =>
        {
            var dbPath = Path.GetDirectoryName(appSettings.LiteDb.ConnectionString.Replace("Filename=", "").Split(';')[0]);
            if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }
            return new LiteDatabase(appSettings.LiteDb.ConnectionString);
        });

        // 配置日志数据库（带熔断器的弹性日志仓储）
        ConfigureSqliteLogging(builder.Services, appSettings);
        
        if (appSettings.MySql.Enabled && !string.IsNullOrEmpty(appSettings.MySql.ConnectionString))
        {
            try
            {
                logger.Info("尝试配置MySQL数据库连接...");
                builder.Services.AddDbContext<MySqlLogDbContext>(options =>
                    options.UseMySql(
                        appSettings.MySql.ConnectionString,
                        ServerVersion.AutoDetect(appSettings.MySql.ConnectionString)),
                    ServiceLifetime.Scoped);
                
                logger.Info("MySQL数据库连接配置成功，使用弹性日志仓储");
                // 使用带熔断器的弹性日志仓储
                builder.Services.AddScoped<ILogRepository, ResilientLogRepository>();
            }
            catch (Exception ex)
            {
                // MySQL配置失败，使用SQLite仓储
                logger.Warn(ex, "MySQL数据库连接配置失败，降级使用SQLite仓储: {Message}", ex.Message);
                builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
            }
        }
        else
        {
            logger.Info("MySQL未启用或连接字符串为空，使用SQLite仓储");
            // MySQL未启用，直接使用SQLite仓储
            builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
        }

        // 配置HttpClient用于第三方API
        builder.Services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>(client =>
        {
            client.BaseAddress = new Uri(appSettings.ThirdPartyApi.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(appSettings.ThirdPartyApi.TimeoutSeconds);
            
            if (!string.IsNullOrEmpty(appSettings.ThirdPartyApi.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", appSettings.ThirdPartyApi.ApiKey);
            }
        });

        // 注册仓储
        builder.Services.AddScoped<IRuleRepository, LiteDbRuleRepository>();
        builder.Services.AddScoped<IChuteRepository, LiteDbChuteRepository>();
        builder.Services.AddScoped<IThirdPartyApiConfigRepository, LiteDbThirdPartyApiConfigRepository>();
        builder.Services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();

        // 添加内存缓存（带可配置的绝对过期和滑动过期）
        // 从配置读取缓存大小限制（以条目数为单位），如果未配置则使用默认值
        var cacheSizeLimit = builder.Configuration.GetValue<long?>("Cache:SizeLimit") ?? 1024;
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheSizeLimit; // 设置缓存大小限制
            options.CompactionPercentage = 0.25; // 压缩百分比
        });

        // 注册应用服务
        builder.Services.AddScoped<PerformanceMetricService>();
        builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
        builder.Services.AddScoped<IParcelProcessingService, ParcelProcessingService>();
        builder.Services.AddScoped<RuleValidationService>();
        builder.Services.AddScoped<IGanttChartService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.GanttChartService>();
        builder.Services.AddScoped<IChuteStatisticsService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.ChuteStatisticsService>();
        
        // 注册包裹活动追踪器（用于空闲检测）
        builder.Services.AddSingleton<IParcelActivityTracker, ZakYip.Sorting.RuleEngine.Infrastructure.Services.ParcelActivityTracker>();
        
        // 注册事件驱动服务
        builder.Services.AddSingleton<ParcelOrchestrationService>();
        builder.Services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(ZakYip.Sorting.RuleEngine.Application.Services.RuleEngineService).Assembly);
        });
        
        // 注册后台服务
        builder.Services.AddHostedService<ParcelQueueProcessorService>();
        builder.Services.AddHostedService<DataCleanupService>();
        builder.Services.AddHostedService<DataArchiveService>();
        builder.Services.AddHostedService<MySqlAutoTuningService>();
        builder.Services.AddHostedService<ShardingTableManagementService>();
        builder.Services.AddHostedService<LogFileCleanupService>();

        // 添加控制器和API服务
        builder.Services.AddControllers(options =>
        {
            // 添加全局模型验证过滤器
            options.Filters.Add<ZakYip.Sorting.RuleEngine.Infrastructure.Filters.ModelValidationFilter>();
        })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            });
        builder.Services.AddEndpointsApiExplorer();
        
        // 添加SignalR服务
        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });
        
        if (appSettings.MiniApi.EnableSwagger)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "分拣规则引擎 API",
                    Version = "v1",
                    Description = "ZakYip 分拣规则引擎核心系统 API"
                });
            });
        }

        // 配置CORS（用于API和SignalR等端点）
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // 初始化数据库
        InitializeDatabases(app.Services, appSettings);

        // 配置HTTP管道
        
        // 添加API请求日志中间件
        app.UseMiddleware<ZakYip.Sorting.RuleEngine.Infrastructure.Middleware.ApiRequestLoggingMiddleware>();

        if (app.Environment.IsDevelopment() || appSettings.MiniApi.EnableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "分拣规则引擎 API v1");
            });
        }

        app.UseCors();
        app.UseRouting();
        app.MapControllers();
        
        // 映射SignalR Hub端点
        app.MapHub<ZakYip.Sorting.RuleEngine.Service.Hubs.SortingHub>("/hubs/sorting");
        app.MapHub<ZakYip.Sorting.RuleEngine.Service.Hubs.DwsHub>("/hubs/dws");

        // 添加最小API端点
        ConfigureMinimalApi(app);

        app.Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "应用程序启动时发生严重错误");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    /// <summary>
    /// 配置SQLite日志
    /// </summary>
    private static void ConfigureSqliteLogging(IServiceCollection services, AppSettings appSettings)
    {
        var dbPath = Path.GetDirectoryName(appSettings.Sqlite.ConnectionString.Replace("Data Source=", "").Split(';')[0]);
        if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        services.AddDbContext<SqliteLogDbContext>(options =>
            options.UseSqlite(appSettings.Sqlite.ConnectionString));
        
        services.AddScoped<ILogRepository, SqliteLogRepository>();
    }

    /// <summary>
    /// 初始化数据库并自动应用迁移
    /// </summary>
    private static void InitializeDatabases(IServiceProvider services, AppSettings appSettings)
    {
        var logger = LogManager.GetCurrentClassLogger();
        using var scope = services.CreateScope();

        // 确保MySQL或SQLite数据库创建并应用迁移
        if (appSettings.MySql.Enabled)
        {
            try
            {
                var mysqlContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();
                if (mysqlContext != null)
                {
                    logger.Info("尝试应用MySQL数据库迁移...");
                    // 自动应用数据库迁移
                    mysqlContext.Database.Migrate();
                    logger.Info("MySQL数据库迁移成功");
                }
            }
            catch (Exception ex)
            {
                // 如果MySQL失败，使用SQLite
                logger.Warn(ex, "MySQL数据库迁移失败，降级使用SQLite: {Message}", ex.Message);
                try
                {
                    var sqliteContext = scope.ServiceProvider.GetService<SqliteLogDbContext>();
                    if (sqliteContext != null)
                    {
                        logger.Info("应用SQLite数据库迁移...");
                        sqliteContext.Database.Migrate();
                        logger.Info("SQLite数据库迁移成功");
                    }
                }
                catch (Exception sqliteEx)
                {
                    logger.Error(sqliteEx, "SQLite数据库迁移也失败: {Message}", sqliteEx.Message);
                }
            }
        }
        else
        {
            try
            {
                logger.Info("MySQL未启用，使用SQLite数据库");
                var sqliteContext = scope.ServiceProvider.GetService<SqliteLogDbContext>();
                if (sqliteContext != null)
                {
                    logger.Info("应用SQLite数据库迁移...");
                    sqliteContext.Database.Migrate();
                    logger.Info("SQLite数据库迁移成功");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "SQLite数据库迁移失败: {Message}", ex.Message);
            }
        }
    }

    /// <summary>
    /// 配置最小API端点
    /// </summary>
    private static void ConfigureMinimalApi(WebApplication app)
    {
        // 健康检查端点
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }))
        .WithName("HealthCheck")
        .WithOpenApi();

        // 版本信息端点
        app.MapGet("/version", () => Results.Ok(new
        {
            version = "1.0.0",
            name = "ZakYip.Sorting.RuleEngine.Core",
            description = "分拣规则引擎核心系统"
        }))
        .WithName("Version")
        .WithOpenApi();
    }
}

